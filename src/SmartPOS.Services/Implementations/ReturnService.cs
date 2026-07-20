using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;

namespace SmartPOS.Services.Implementations;

public class ReturnService : IReturnService
{
    private readonly IReturnRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventorySyncService _inventorySyncService;
    private readonly ICustomerService _customerService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReturnService> _logger;

    public ReturnService(
        IReturnRepository returnRepository,
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInventorySyncService inventorySyncService,
        ICustomerService customerService,
        IAuditService auditService,
        ILogger<ReturnService> logger)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inventorySyncService = inventorySyncService;
        _customerService = customerService;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<ReturnDto> CreateReturnAsync(ReturnRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        if (dto.Items == null || dto.Items.Count == 0)
            throw new BusinessException("Danh sách sản phẩm trả hàng không được để trống.");

        var order = await _orderRepository.GetByIdWithItemsAsync(dto.OrderId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy đơn hàng");

        if (order.PaymentStatus != PaymentStatus.Success && order.Status != OrderStatus.Confirmed)
            throw new BusinessException("Chỉ cho phép trả hàng với đơn hàng đã thanh toán thành công.");

        var existingReturns = await _returnRepository.GetByOrderIdAsync(dto.OrderId).ConfigureAwait(false);
        var previousReturnedQuantities = existingReturns
            .Where(r => r.Status != ReturnStatus.Rejected)
            .SelectMany(r => r.Items)
            .GroupBy(i => i.OrderItemId)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        decimal totalRefundAmount = 0;
        var returnItems = new List<ReturnItem>();

        foreach (var itemInput in dto.Items)
        {
            if (itemInput.Quantity <= 0)
                throw new BusinessException("Số lượng trả hàng phải lớn hơn 0.");

            var orderItem = order.Items.FirstOrDefault(i => i.Id == itemInput.OrderItemId)
                ?? throw new BusinessException("Sản phẩm không thuộc đơn hàng này.");

            int alreadyReturned = previousReturnedQuantities.TryGetValue(orderItem.Id, out var qty) ? qty : 0;
            int maxAllowable = orderItem.Quantity - alreadyReturned;

            if (itemInput.Quantity > maxAllowable)
            {
                throw new BusinessException($"Số lượng trả ({itemInput.Quantity}) vượt quá số lượng tối đa có thể trả ({maxAllowable}) cho sản phẩm.");
            }

            totalRefundAmount += itemInput.Quantity * orderItem.UnitPrice;

            returnItems.Add(new ReturnItem
            {
                Id = Guid.NewGuid(),
                OrderItemId = orderItem.Id,
                Quantity = itemInput.Quantity
            });
        }

        var returnEntity = new Return
        {
            Id = Guid.NewGuid(),
            OrderId = dto.OrderId,
            RequestedBy = dto.RequestedBy,
            Reason = dto.Reason ?? string.Empty,
            Status = ReturnStatus.Requested,
            RefundAmount = totalRefundAmount,
            CreatedAt = DateTime.UtcNow,
            Items = returnItems
        };

        await _returnRepository.AddAsync(returnEntity).ConfigureAwait(false);

        await _auditService.LogAsync(
            "CREATE_RETURN",
            "Return",
            returnEntity.Id,
            null,
            new { returnEntity.OrderId, returnEntity.RefundAmount, returnEntity.Reason },
            dto.RequestedBy).ConfigureAwait(false);

        _logger.LogInformation("Đã tạo yêu cầu trả hàng {ReturnId} cho đơn hàng {OrderId}", returnEntity.Id, dto.OrderId);

        // Fetch re-populated entity for DTO mapping
        var reloaded = await _returnRepository.GetByIdAsync(returnEntity.Id).ConfigureAwait(false);
        return MapToDto(reloaded ?? returnEntity);
    }

    public async Task<ReturnDto> ApproveAsync(Guid returnId, Guid approvedBy)
    {
        var returnEntity = await _returnRepository.GetByIdAsync(returnId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy yêu cầu trả hàng");

        if (returnEntity.Status != ReturnStatus.Requested)
            throw new BusinessException("Yêu cầu trả hàng đã được xử lý trước đó.");

        returnEntity.Status = ReturnStatus.Approved;
        returnEntity.ApprovedBy = approvedBy;
        returnEntity.ResolvedAt = DateTime.UtcNow;

        // 1. Re-increment local stock
        foreach (var item in returnEntity.Items)
        {
            if (item.OrderItem?.Product != null)
            {
                item.OrderItem.Product.LocalStockQuantity += item.Quantity;
                await _productRepository.UpdateAsync(item.OrderItem.Product).ConfigureAwait(false);

                if (Guid.TryParse(item.OrderItem.Product.ExternalInventoryId, out var externalGuid))
                {
                    try
                    {
                        await _inventorySyncService.RestockAsync(new RestockEventDto(externalGuid, item.Quantity, $"Return #{returnEntity.Id}")).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Không thể đồng bộ nhập lại kho sang Inventory Manager API cho sản phẩm {ProductId}", item.OrderItem.ProductId);
                    }
                }
            }
        }

        // 3. Deduct loyalty points if customer exists
        var order = returnEntity.Order ?? await _orderRepository.GetByIdWithItemsAsync(returnEntity.OrderId).ConfigureAwait(false);
        if (order != null && order.CustomerId.HasValue && returnEntity.RefundAmount.HasValue && returnEntity.RefundAmount.Value > 0)
        {
            int pointsToDeduct = (int)Math.Floor(returnEntity.RefundAmount.Value / 10000m);
            if (pointsToDeduct > 0)
            {
                try
                {
                    await _customerService.DeductLoyaltyPointsAsync(order.CustomerId.Value, pointsToDeduct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi trừ điểm tích lũy của khách hàng {CustomerId} sau khi hoàn tiền", order.CustomerId.Value);
                }
            }
        }

        await _returnRepository.UpdateAsync(returnEntity).ConfigureAwait(false);

        await _auditService.LogAsync(
            "APPROVE_RETURN",
            "Return",
            returnId,
            null,
            new { returnId, approvedBy, returnEntity.RefundAmount },
            approvedBy).ConfigureAwait(false);

        _logger.LogInformation("Đã phê duyệt yêu cầu trả hàng {ReturnId}", returnId);

        return MapToDto(returnEntity);
    }

    public async Task<ReturnDto> RejectAsync(Guid returnId, Guid approvedBy)
    {
        var returnEntity = await _returnRepository.GetByIdAsync(returnId).ConfigureAwait(false)
            ?? throw new BusinessException("Không tìm thấy yêu cầu trả hàng");

        if (returnEntity.Status != ReturnStatus.Requested)
            throw new BusinessException("Yêu cầu trả hàng đã được xử lý trước đó.");

        returnEntity.Status = ReturnStatus.Rejected;
        returnEntity.ApprovedBy = approvedBy;
        returnEntity.ResolvedAt = DateTime.UtcNow;

        await _returnRepository.UpdateAsync(returnEntity).ConfigureAwait(false);

        await _auditService.LogAsync(
            "REJECT_RETURN",
            "Return",
            returnId,
            null,
            new { returnId, approvedBy },
            approvedBy).ConfigureAwait(false);

        _logger.LogInformation("Đã từ chối yêu cầu trả hàng {ReturnId}", returnId);

        return MapToDto(returnEntity);
    }

    public async Task<IReadOnlyList<ReturnDto>> GetAllReturnsAsync()
    {
        var list = await _returnRepository.GetAllAsync().ConfigureAwait(false);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ReturnDto?> GetReturnByIdAsync(Guid id)
    {
        var item = await _returnRepository.GetByIdAsync(id).ConfigureAwait(false);
        return item == null ? null : MapToDto(item);
    }

    private static ReturnDto MapToDto(Return r)
    {
        return new ReturnDto
        {
            Id = r.Id,
            OrderId = r.OrderId,
            OrderCode = r.OrderId.ToString()[..8].ToUpper(),
            RequestedByName = r.RequestedByUser?.FullName ?? r.RequestedByUser?.Username ?? "N/A",
            ApprovedByName = null, // Will be populated if resolved
            Status = r.Status.ToString(),
            Reason = r.Reason,
            RefundAmount = r.RefundAmount,
            CreatedAt = r.CreatedAt,
            ResolvedAt = r.ResolvedAt,
            Items = r.Items?.Select(i => new ReturnItemDto
            {
                OrderItemId = i.OrderItemId,
                ProductName = i.OrderItem?.ProductName ?? i.OrderItem?.Product?.Name ?? "Sản phẩm",
                Quantity = i.Quantity,
                UnitPrice = i.OrderItem?.UnitPrice ?? 0
            }).ToList() ?? new List<ReturnItemDto>()
        };
    }
}
