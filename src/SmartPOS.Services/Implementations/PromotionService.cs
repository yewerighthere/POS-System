using Microsoft.Extensions.Logging;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Auth;
using SmartPOS.Shared.DTOs.Shift;
using SmartPOS.Shared.DTOs.Product;
using SmartPOS.Shared.DTOs.Cart;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.DTOs.Payment;
using SmartPOS.Shared.DTOs.Invoice;
using SmartPOS.Shared.DTOs.Customer;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Catalog;
using SmartPOS.Shared.DTOs.Inventory;
using SmartPOS.Shared.DTOs.Report;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;

namespace SmartPOS.Services.Implementations;

public class PromotionService : IPromotionService
{
    private readonly ILogger<PromotionService> _logger;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IAuditService? _auditService;

    public PromotionService(ILogger<PromotionService> logger, IPromotionRepository promotionRepository, IAuditService? auditService = null)
    {
        _logger = logger;
        _promotionRepository = promotionRepository;
        _auditService = auditService;
    }

    public async Task<PromotionValidationResultDto> ValidateCodeAsync(string code, CartSummaryDto cart)
    {
        var promotion = await _promotionRepository.GetByCodeAsync(code);
        if (promotion == null)
            return new PromotionValidationResultDto { IsValid = false, Message = "Mã khuyến mãi không tồn tại." };

        if (!promotion.IsActive)
            return new PromotionValidationResultDto { IsValid = false, Message = "Mã khuyến mãi đang tạm ngưng." };

        var today = DateOnly.FromDateTime(DateTime.Today);
        if (today < promotion.StartDate)
            return new PromotionValidationResultDto { IsValid = false, Message = "Mã khuyến mãi chưa tới thời gian áp dụng." };
        if (today > promotion.EndDate)
            return new PromotionValidationResultDto { IsValid = false, Message = "Mã khuyến mãi đã hết hạn." };

        if (promotion.MinOrderAmount.HasValue && cart.Subtotal < promotion.MinOrderAmount.Value)
            return new PromotionValidationResultDto { IsValid = false, Message = $"Đơn hàng phải đạt tối thiểu {promotion.MinOrderAmount.Value:N0} đ." };

        decimal discountAmount = 0;
        if (promotion.Type.Contains("Percentage", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = cart.Subtotal * (promotion.DiscountValue / 100m);
        }
        else if (promotion.Type.Contains("FixedAmount", StringComparison.OrdinalIgnoreCase) || promotion.Type.Contains("Amount", StringComparison.OrdinalIgnoreCase))
        {
            discountAmount = promotion.DiscountValue;
        }

        if (discountAmount > cart.Subtotal)
            discountAmount = cart.Subtotal;

        return new PromotionValidationResultDto 
        { 
            IsValid = true, 
            DiscountAmount = discountAmount 
        };
    }

    public Task<bool> RequestApprovalAsync(Guid promotionId, Guid managerId)
    {
        return Task.FromResult(true); // Mock approval logic for now
    }

    public async Task<CartSummaryDto> ApplyPromotionAsync(string code, CartSummaryDto cart)
    {
        var validation = await ValidateCodeAsync(code, cart);
        if (validation.IsValid)
        {
            var promotion = await _promotionRepository.GetByCodeAsync(code);
            if (promotion != null)
            {
                cart.AppliedPromotion = new PromotionDto 
                {
                    Id = promotion.Id,
                    Code = promotion.Code,
                    Name = promotion.Name,
                    Type = promotion.Type,
                    DiscountValue = promotion.DiscountValue,
                    MinOrderAmount = promotion.MinOrderAmount,
                    StartDate = promotion.StartDate,
                    EndDate = promotion.EndDate,
                    IsActive = promotion.IsActive
                };
            }
            cart.DiscountAmount = validation.DiscountAmount;
            cart.TotalAmount = Math.Max(0, cart.Subtotal - cart.DiscountAmount - cart.PointsDiscountAmount + cart.TaxAmount);
        }
        return cart;
    }

    public async Task<PromotionDto> CreatePromotionAsync(CreatePromotionDto dto)
    {
        if (dto.EndDate < dto.StartDate)
        {
            throw new ArgumentException("EndDate cannot be before StartDate");
        }

        var promotion = new SmartPOS.Data.Entities.Promotion
        {
            Id = Guid.NewGuid(),
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Type = dto.Type,
            DiscountValue = dto.DiscountValue,
            MinOrderAmount = dto.MinOrderAmount,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            IsActive = dto.IsActive
        };

        await _promotionRepository.AddAsync(promotion);

        if (_auditService != null)
        {
            await _auditService.LogAsync(
                "CREATE_PROMOTION",
                "Promotion",
                promotion.Id,
                null,
                new { promotion.Code, promotion.Name, promotion.DiscountValue, promotion.Type },
                Guid.Empty).ConfigureAwait(false);
        }

        return MapToDto(promotion);
    }

    private PromotionDto MapToDto(SmartPOS.Data.Entities.Promotion p)
    {
        return new PromotionDto
        {
            Id = p.Id,
            Code = p.Code,
            Name = p.Name,
            Description = p.Description,
            Type = p.Type,
            DiscountValue = p.DiscountValue,
            MinOrderAmount = p.MinOrderAmount,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            IsActive = p.IsActive
        };
    }

    private IReadOnlyList<PromotionDto> MapToDtoList(IEnumerable<SmartPOS.Data.Entities.Promotion> list)
    {
        return list.Select(MapToDto).ToList();
    }

    public async Task<PromotionDto?> GetByIdAsync(Guid id)
    {
        var p = await _promotionRepository.GetByIdAsync(id);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PromotionDto?> GetByCodeAsync(string code)
    {
        var p = await _promotionRepository.GetByCodeAsync(code);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PromotionDto?> GetByNameAsync(string name)
    {
        var p = await _promotionRepository.GetByNameAsync(name);
        return p == null ? null : MapToDto(p);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetAllPromotionsAsync()
    {
        return MapToDtoList(await _promotionRepository.GetAllPromotionsAsync());
    }

    public async Task<IReadOnlyList<PromotionDto>> GetExpiredPromotionsAsync(DateOnly currentDate)
    {
        return MapToDtoList(await _promotionRepository.GetExpiredPromotionsAsync(currentDate));
    }

    public async Task<IReadOnlyList<PromotionDto>> GetUnexpiredPromotionsAsync(DateOnly currentDate)
    {
        return MapToDtoList(await _promotionRepository.GetUnexpiredPromotionsAsync(currentDate));
    }

    public async Task<IReadOnlyList<PromotionDto>> SearchByNameAsync(string nameKeyword)
    {
        return MapToDtoList(await _promotionRepository.SearchByNameAsync(nameKeyword));
    }

    public async Task<IReadOnlyList<PromotionDto>> SearchByCodeAsync(string codeKeyword)
    {
        return MapToDtoList(await _promotionRepository.SearchByCodeAsync(codeKeyword));
    }

    public async Task<IReadOnlyList<PromotionDto>> GetActiveAsync(DateOnly date)
    {
        return MapToDtoList(await _promotionRepository.GetActiveAsync(date));
    }

    public async Task DeleteAsync(Guid id)
    {
        await _promotionRepository.DeleteAsync(id);
    }

    public async Task UpdateCodeAsync(Guid id, string code)
    {
        await _promotionRepository.UpdateCodeAsync(id, code);
    }

    public async Task UpdateNameAsync(Guid id, string name)
    {
        await _promotionRepository.UpdateNameAsync(id, name);
    }

    public async Task UpdateDescriptionAsync(Guid id, string? description)
    {
        await _promotionRepository.UpdateDescriptionAsync(id, description);
    }

    public async Task UpdateTypeAsync(Guid id, string type)
    {
        await _promotionRepository.UpdateTypeAsync(id, type);
    }

    public async Task UpdateDiscountValueAsync(Guid id, decimal discountValue)
    {
        await _promotionRepository.UpdateDiscountValueAsync(id, discountValue);
    }

    public async Task UpdateMinOrderAmountAsync(Guid id, decimal? minOrderAmount)
    {
        await _promotionRepository.UpdateMinOrderAmountAsync(id, minOrderAmount);
    }

    public async Task UpdateProductIdAsync(Guid id, Guid? productId)
    {
        await _promotionRepository.UpdateProductIdAsync(id, productId);
    }

    public async Task UpdateStartDateAsync(Guid id, DateOnly startDate)
    {
        await _promotionRepository.UpdateStartDateAsync(id, startDate);
    }

    public async Task UpdateEndDateAsync(Guid id, DateOnly endDate)
    {
        await _promotionRepository.UpdateEndDateAsync(id, endDate);
    }

    public async Task UpdateRequiresApprovalThresholdAsync(Guid id, decimal? threshold)
    {
        await _promotionRepository.UpdateRequiresApprovalThresholdAsync(id, threshold);
    }

    public async Task UpdateIsActiveAsync(Guid id, bool isActive)
    {
        await _promotionRepository.UpdateIsActiveAsync(id, isActive);
    }
}
