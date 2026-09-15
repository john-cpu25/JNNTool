using System.Windows;
using JNNTool.Tools.CreateFilter.ViewModels;

namespace JNNTool.Tools.CreateFilter.UI
{
    public partial class CreateFilterWindow : Window
    {
        public CreateFilterWindow(CreateFilterViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
