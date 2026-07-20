using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Promotion;
using System.Collections.ObjectModel;

namespace SmartPOS.WPF.ViewModels;

public partial class PromotionViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _successMessage = string.Empty;

    // Data lists
    private List<PromotionDto> _allPromotions = new();
    [ObservableProperty] private ObservableCollection<PromotionDto> _filteredPromotions = new();
    [ObservableProperty] private PromotionDto? _selectedPromotion;

    // Filter properties
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _statusFilter = "Tất cả";
    [ObservableProperty] private string _typeFilter = "Tất cả";

    public List<string> StatusOptions { get; } = new() { "Tất cả", "Active", "Inactive" };
    public List<string> TypeOptions { get; } = new() { "Tất cả", "Percentage", "FixedAmount" };
    public int FilteredCount => FilteredPromotions.Count;

    // Form fields
    [ObservableProperty] private bool _isAddPromotionVisible;
    [ObservableProperty] private string _newCode = string.Empty;
    [ObservableProperty] private string _newName = string.Empty;
    [ObservableProperty] private string _newType = "Percentage";
    [ObservableProperty] private decimal _newDiscountValue;
    [ObservableProperty] private decimal? _newMinOrderAmount;
    [ObservableProperty] private DateTime _newStartDate = DateTime.Today;
    [ObservableProperty] private DateTime _newEndDate = DateTime.Today.AddDays(7);

    public PromotionViewModel(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var data = await _promotionService.GetAllPromotionsAsync();
            _allPromotions = data.ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi tải khuyến mãi: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchQueryChanged(string value) => ApplyFilter();
    partial void OnStatusFilterChanged(string value) => ApplyFilter();
    partial void OnTypeFilterChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        var query = _allPromotions.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            var lowerQuery = SearchQuery.ToLower();
            query = query.Where(p => 
                (p.Code != null && p.Code.ToLower().Contains(lowerQuery)) ||
                (p.Name != null && p.Name.ToLower().Contains(lowerQuery))
            );
        }

        if (StatusFilter == "Active")
        {
            query = query.Where(p => p.IsActive);
        }
        else if (StatusFilter == "Inactive")
        {
            query = query.Where(p => !p.IsActive);
        }

        if (TypeFilter != "Tất cả")
        {
            query = query.Where(p => p.Type == TypeFilter);
        }

        FilteredPromotions = new ObservableCollection<PromotionDto>(query);
        OnPropertyChanged(nameof(FilteredCount));
    }

    [RelayCommand]
    private void ToggleAddPromotion() => IsAddPromotionVisible = !IsAddPromotionVisible;

    [RelayCommand]
    private async Task AddPromotionAsync()
    {
        if (string.IsNullOrWhiteSpace(NewCode) || string.IsNullOrWhiteSpace(NewName))
        {
            ErrorMessage = "Vui lòng nhập Mã (Code) và Tên (Name) khuyến mãi.";
            return;
        }

        if (NewDiscountValue <= 0)
        {
            ErrorMessage = "Mức giảm phải lớn hơn 0.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
        try
        {
            var dto = new CreatePromotionDto
            {
                Code = NewCode.Trim(),
                Name = NewName.Trim(),
                Type = NewType,
                DiscountValue = NewDiscountValue,
                MinOrderAmount = NewMinOrderAmount,
                StartDate = DateOnly.FromDateTime(NewStartDate),
                EndDate = DateOnly.FromDateTime(NewEndDate),
                IsActive = true
            };

            await _promotionService.CreatePromotionAsync(dto);
            SuccessMessage = "Thêm khuyến mãi thành công!";
            
            // Reset form
            NewCode = string.Empty;
            NewName = string.Empty;
            NewDiscountValue = 0;
            NewMinOrderAmount = null;
            IsAddPromotionVisible = false;

            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi thêm khuyến mãi: " + ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task DeactivatePromotionAsync(Guid id)
    {
        try
        {
            await _promotionService.UpdateIsActiveAsync(id, false);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi vô hiệu hóa: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task ReactivatePromotionAsync(Guid id)
    {
        try
        {
            await _promotionService.UpdateIsActiveAsync(id, true);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = "Lỗi khi kích hoạt lại: " + ex.Message;
        }
    }
}
