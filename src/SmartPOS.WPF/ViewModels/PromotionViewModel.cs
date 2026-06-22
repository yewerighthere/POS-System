using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class PromotionViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _session;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public ObservableCollection<PromotionDto> Promotions { get; } = new();

    public PromotionViewModel(
        IPromotionService promotionService, 
        NavigationService navigationService, 
        CurrentSessionContext session)
    {
        _promotionService = promotionService;
        _navigationService = navigationService;
        _session = session;

        _ = LoadPromotionsAsync();
    }

    [RelayCommand]
    private async Task LoadPromotionsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var promotions = await _promotionService.GetAllPromotionsAsync();
            Promotions.Clear();
            foreach (var p in promotions)
            {
                Promotions.Add(p);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tải danh sách: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void ViewDetail(Guid id)
    {
        _session.SelectedPromotionId = id;
        _navigationService.NavigateTo<PromotionDetailViewModel>();
    }

    [RelayCommand]
    private async Task DeleteAsync(Guid id)
    {
        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn xóa khuyến mãi này không?",
            "Xác nhận xóa",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            IsLoading = true;
            try
            {
                await _promotionService.DeleteAsync(id);
                await LoadPromotionsAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi xóa: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
