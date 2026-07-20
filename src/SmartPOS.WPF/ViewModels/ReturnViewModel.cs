using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Data.Entities;
using SmartPOS.Data.Repositories.Interfaces;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Return;
using SmartPOS.Shared.DTOs.Order;
using SmartPOS.Shared.Exceptions;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class ReturnItemInputViewModel : ObservableObject
{
    public Guid OrderItemId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int MaxQuantity { get; set; }

    [ObservableProperty]
    private int _returnQuantity;
}

public partial class ReturnViewModel : ObservableObject
{
    private readonly IReturnService _returnService;
    private readonly IOrderRepository _orderRepository;
    private readonly CurrentSessionContext _session;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusFilter = "Tất cả";

    [ObservableProperty]
    private ReturnDto? _selectedReturn;

    [ObservableProperty]
    private bool _isCreatePopupOpen;

    [ObservableProperty]
    private string _orderSearchInput = string.Empty;

    [ObservableProperty]
    private OrderDto? _selectedOrderToReturn;

    [ObservableProperty]
    private string _returnReasonInput = string.Empty;

    public ObservableCollection<ReturnDto> ReturnsList { get; } = new();
    public ObservableCollection<ReturnDto> FilteredReturns { get; } = new();
    public ObservableCollection<ReturnItemInputViewModel> OrderItemsForReturn { get; } = new();

    public ReturnViewModel(
        IReturnService returnService,
        IOrderRepository orderRepository,
        CurrentSessionContext session)
    {
        _returnService = returnService;
        _orderRepository = orderRepository;
        _session = session;

        _ = LoadReturnsAsync();
    }

    [RelayCommand]
    public async Task LoadReturnsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var list = await _returnService.GetAllReturnsAsync();
            ReturnsList.Clear();
            foreach (var item in list)
            {
                ReturnsList.Add(item);
            }
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi nạp danh sách trả hàng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        FilteredReturns.Clear();
        var query = ReturnsList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var kw = SearchQuery.Trim().ToLower();
            query = query.Where(r => r.OrderCode.ToLower().Contains(kw) ||
                                     r.Reason.ToLower().Contains(kw) ||
                                     r.RequestedByName.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(StatusFilter) && StatusFilter != "Tất cả")
        {
            query = query.Where(r => r.Status.Equals(StatusFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query)
        {
            FilteredReturns.Add(item);
        }
    }

    [RelayCommand]
    private void OpenCreatePopup()
    {
        ErrorMessage = string.Empty;
        OrderSearchInput = string.Empty;
        SelectedOrderToReturn = null;
        ReturnReasonInput = string.Empty;
        OrderItemsForReturn.Clear();
        IsCreatePopupOpen = true;
    }

    [RelayCommand]
    private void CloseCreatePopup()
    {
        IsCreatePopupOpen = false;
    }

    [RelayCommand]
    private async Task SearchOrderForReturnAsync()
    {
        if (string.IsNullOrWhiteSpace(OrderSearchInput))
            return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var searchKw = OrderSearchInput.Trim();
            Order? fullOrder = null;

            if (Guid.TryParse(searchKw, out var orderGuid))
            {
                fullOrder = await _orderRepository.GetByIdWithItemsAsync(orderGuid).ConfigureAwait(false);
            }
            else
            {
                var orders = await _orderRepository.GetOrdersByDateRangeAsync(DateTime.MinValue, DateTime.MaxValue, null, null, null).ConfigureAwait(false);
                var match = orders.FirstOrDefault(o => o.Id.ToString().StartsWith(searchKw, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    fullOrder = await _orderRepository.GetByIdWithItemsAsync(match.Id).ConfigureAwait(false);
                }
            }

            if (fullOrder == null)
            {
                ErrorMessage = "Không tìm thấy đơn hàng phù hợp.";
                SelectedOrderToReturn = null;
                OrderItemsForReturn.Clear();
                return;
            }

            SelectedOrderToReturn = new OrderDto
            {
                Id = fullOrder.Id,
                OrderCode = fullOrder.Id.ToString()[..8].ToUpper(),
                TotalAmount = fullOrder.TotalAmount,
                Status = fullOrder.Status.ToString(),
                CreatedAt = fullOrder.CreatedAt
            };

            var previousReturns = await _returnService.GetAllReturnsAsync();
            var prevForOrder = previousReturns.Where(r => r.OrderId == fullOrder.Id && r.Status != "Rejected").ToList();

            OrderItemsForReturn.Clear();
            foreach (var item in fullOrder.Items)
            {
                int returnedSoFar = prevForOrder
                    .SelectMany(r => r.Items)
                    .Where(ri => ri.OrderItemId == item.Id)
                    .Sum(ri => ri.Quantity);

                int available = item.Quantity - returnedSoFar;
                if (available > 0)
                {
                    OrderItemsForReturn.Add(new ReturnItemInputViewModel
                    {
                        OrderItemId = item.Id,
                        ProductName = item.ProductName,
                        UnitPrice = item.UnitPrice,
                        MaxQuantity = available,
                        ReturnQuantity = 0
                    });
                }
            }

            if (OrderItemsForReturn.Count == 0)
            {
                ErrorMessage = "Đơn hàng này đã trả hết toàn bộ số lượng sản phẩm.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tìm đơn hàng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmCreateReturnAsync()
    {
        if (SelectedOrderToReturn == null)
        {
            ErrorMessage = "Vui lòng chọn đơn hàng cần trả.";
            return;
        }

        var selectedItems = OrderItemsForReturn
            .Where(i => i.ReturnQuantity > 0)
            .Select(i => new ReturnItemInputDto(i.OrderItemId, i.ReturnQuantity))
            .ToList();

        if (selectedItems.Count == 0)
        {
            ErrorMessage = "Vui lòng nhập số lượng trả cho ít nhất một sản phẩm.";
            return;
        }

        if (string.IsNullOrWhiteSpace(ReturnReasonInput))
        {
            ErrorMessage = "Vui lòng nhập lý do trả hàng.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var currentUserId = _session.CurrentUser?.Id ?? Guid.Empty;
            var request = new ReturnRequestDto(SelectedOrderToReturn.Id, currentUserId, ReturnReasonInput, selectedItems);

            await _returnService.CreateReturnAsync(request);
            CloseCreatePopup();
            await LoadReturnsAsync();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tạo yêu cầu trả hàng: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ApproveReturnAsync(ReturnDto? item)
    {
        var target = item ?? SelectedReturn;
        if (target == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var managerId = _session.CurrentUser?.Id ?? Guid.Empty;
            await _returnService.ApproveAsync(target.Id, managerId);
            await LoadReturnsAsync();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi phê duyệt: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RejectReturnAsync(ReturnDto? item)
    {
        var target = item ?? SelectedReturn;
        if (target == null) return;

        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var managerId = _session.CurrentUser?.Id ?? Guid.Empty;
            await _returnService.RejectAsync(target.Id, managerId);
            await LoadReturnsAsync();
        }
        catch (BusinessException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi từ chối: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
