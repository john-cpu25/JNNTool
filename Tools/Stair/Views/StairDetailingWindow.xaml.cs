using System.Windows;
using JNN.Stair.ViewModels;

namespace JNN.Stair.Views
{
    public partial class StairDetailingWindow : Window
    {
        public StairDetailingViewModel ViewModel { get; }

        public StairDetailingWindow(StairDetailingViewModel? viewModel = null)
        {
            InitializeComponent();
            ViewModel = viewModel ?? new StairDetailingViewModel();
            DataContext = ViewModel;
        }
    }
}
