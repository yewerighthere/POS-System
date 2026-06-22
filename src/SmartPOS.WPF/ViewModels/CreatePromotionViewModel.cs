using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.Shared.Enums;
using SmartPOS.Shared.Exceptions;
using System;
using System.Threading.Tasks;

namespace SmartPOS.WPF.ViewModels;

public partial class CreatePromotionViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string? _description;

    [ObservableProperty]
    private string _type = "Percentage"; // e.g., "Percentage", "FixedAmount"

    [ObservableProperty]
    private decimal _discountValue;

    [ObservableProperty]
    private decimal? _minOrderAmount;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today.AddDays(7);

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public Action? OnRequestClose;

    public CreatePromotionViewModel(IPromotionService promotionService)
    {
        _promotionService = promotionService;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            IsLoading = true;
            ErrorMessage = string.Empty;

            var dto = new CreatePromotionDto
            {
                Code = this.Code,
                Name = this.Name,
                Description = this.Description,
                Type = this.Type,
                DiscountValue = this.DiscountValue,
                MinOrderAmount = this.MinOrderAmount,
                StartDate = DateOnly.FromDateTime(this.StartDate),
                EndDate = DateOnly.FromDateTime(this.EndDate),
                IsActive = this.IsActive
            };

            await _promotionService.CreatePromotionAsync(dto);
            
            // Close the window after successful creation
            OnRequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error creating promotion: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        OnRequestClose?.Invoke();
    }
}
