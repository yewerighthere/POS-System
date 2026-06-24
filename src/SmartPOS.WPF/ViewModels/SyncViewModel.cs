using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Services.Interfaces;
using SmartPOS.Shared.DTOs.Inventory;

namespace SmartPOS.WPF.ViewModels;

public partial class SyncViewModel : ObservableObject
{
    private readonly IInventorySyncService _inventorySyncService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _catalogStatus = string.Empty;

    [ObservableProperty]
    private string _stockStatus = string.Empty;

    [ObservableProperty]
    private bool _catalogSuccess;

    [ObservableProperty]
    private bool _stockSuccess;

    public SyncViewModel(IInventorySyncService inventorySyncService)
    {
        _inventorySyncService = inventorySyncService;
    }

    // Khi IsLoading thay đổi → thông báo lại CanExecute cho tất cả command
    partial void OnIsLoadingChanged(bool value)
    {
        SyncCatalogCommand.NotifyCanExecuteChanged();
        SyncStockCommand.NotifyCanExecuteChanged();
        SyncAllCommand.NotifyCanExecuteChanged();
    }

    private bool CanSync() => !IsLoading;

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncCatalog()
    {
        IsLoading = true;
        CatalogStatus = "Đang đồng bộ catalog...";
        CatalogSuccess = false;
        StatusMessage = string.Empty;

        try
        {
            SyncResultDto result = await _inventorySyncService.SyncCatalogAsync();
            CatalogSuccess = result.Status == "SUCCESS";
            CatalogStatus = result.Message ?? string.Empty;
            if (!CatalogSuccess)
                StatusMessage = result.Message ?? "Đồng bộ catalog thất bại.";
        }
        catch (Exception ex)
        {
            CatalogSuccess = false;
            CatalogStatus = "Lỗi: " + ex.Message;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncStock()
    {
        IsLoading = true;
        StockStatus = "Đang đồng bộ tồn kho...";
        StockSuccess = false;
        StatusMessage = string.Empty;

        try
        {
            SyncResultDto result = await _inventorySyncService.SyncStockAsync();
            StockSuccess = result.Status == "SUCCESS";
            StockStatus = result.Message ?? string.Empty;
            if (!StockSuccess)
                StatusMessage = result.Message ?? "Đồng bộ tồn kho thất bại.";
        }
        catch (Exception ex)
        {
            StockSuccess = false;
            StockStatus = "Lỗi: " + ex.Message;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSync))]
    private async Task SyncAll()
    {
        await SyncCatalog();
        if (CatalogSuccess)
            await SyncStock();
        else
            StatusMessage = "Bỏ qua đồng bộ tồn kho vì catalog thất bại.";
    }
}
