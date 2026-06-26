using System.Windows.Controls;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class CatalogView : UserControl
{
    public CatalogView()
    {
        InitializeComponent();

        // Tự động load dữ liệu khi view được hiển thị lần đầu
        Loaded += async (_, _) =>
        {
            if (DataContext is CatalogViewModel vm)
                await vm.LoadAsync();
        };
    }
}
