using System.Windows;
using SmartPOS.WPF.ViewModels;

namespace SmartPOS.WPF.Views
{
    public partial class CreatePromotionView : Window
    {
        public CreatePromotionView(CreatePromotionViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Handle the close request from ViewModel
            viewModel.OnRequestClose = () =>
            {
                this.DialogResult = true;
                this.Close();
            };
        }
    }
}
