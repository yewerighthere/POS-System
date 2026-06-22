using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Promotion;
using SmartPOS.WPF.Navigation;
using SmartPOS.WPF.Session;

namespace SmartPOS.WPF.ViewModels;

public partial class PromotionDetailViewModel : ObservableObject
{
    private readonly IPromotionService _promotionService;
    private readonly NavigationService _navigationService;
    private readonly CurrentSessionContext _session;

    [ObservableProperty]
    private PromotionDto _promotion = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public PromotionDetailViewModel(
        IPromotionService promotionService,
        NavigationService navigationService,
        CurrentSessionContext session)
    {
        _promotionService = promotionService;
        _navigationService = navigationService;
        _session = session;

        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        if (_session.SelectedPromotionId == null)
        {
            ErrorMessage = "Không có Promotion Id được chọn.";
            return;
        }

        IsLoading = true;
        try
        {
            var p = await _promotionService.GetByIdAsync(_session.SelectedPromotionId.Value);
            if (p != null)
            {
                Promotion = p;
            }
            else
            {
                ErrorMessage = "Không tìm thấy dữ liệu Promotion.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Lỗi tải dữ liệu: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UpdateAsync()
    {
        ErrorMessage = string.Empty;

        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn cập nhật khuyến mãi này?",
            "Xác nhận cập nhật",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            IsLoading = true;
            try
            {
                await _promotionService.UpdateAsync(Promotion);
                MessageBox.Show("Cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                _navigationService.NavigateTo<PromotionViewModel>();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Lỗi cập nhật: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        ErrorMessage = string.Empty;

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
                await _promotionService.DeleteAsync(Promotion.Id);
                _navigationService.NavigateTo<PromotionViewModel>();
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

    [RelayCommand]
    private void Return()
    {
        _navigationService.NavigateTo<PromotionViewModel>();
    }
}
