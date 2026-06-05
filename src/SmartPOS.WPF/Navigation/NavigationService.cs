using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace SmartPOS.WPF.Navigation;

public class NavigationService
{
    private readonly IServiceProvider _serviceProvider;
    public Frame? MainFrame { get; set; }

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void NavigateTo<TViewModel>()
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        var viewName = typeof(TViewModel).Name.Replace("ViewModel", "View");
        var viewType = Type.GetType($"SmartPOS.WPF.Views.{viewName}");
        if (viewType is null)
        {
            throw new NotImplementedException();
        }

        var view = (Control)_serviceProvider.GetRequiredService(viewType);
        view.DataContext = viewModel;
        MainFrame?.Navigate(view);
    }
}
