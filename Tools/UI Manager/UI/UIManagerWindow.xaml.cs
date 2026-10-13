using System.Windows;
using JNNTool.Tools.UIManager.ViewModels;

namespace JNNTool.Tools.UIManager.UI
{
    /// <summary>
    /// Interaction logic for UIManagerWindow.xaml
    /// </summary>
    public partial class UIManagerWindow : Window
    {
        public UIManagerWindow(UIManagerViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            if (viewModel != null)
            {
                viewModel.RequestClose += Close;
            }
        }
    }
}
