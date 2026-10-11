using System.Windows;
using JNNTool.Tools.ClashDetection.ViewModels;

namespace JNNTool.Tools.ClashDetection.UI
{
    public partial class ClashDetectionWindow : Window
    {
        public ClashDetectionWindow(ClashDetectionViewModel viewModel)
        {
            JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();


            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

