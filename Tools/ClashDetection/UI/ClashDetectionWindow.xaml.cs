using System.Windows;
using JNNTool.Tools.ClashDetection.ViewModels;

namespace JNNTool.Tools.ClashDetection.UI
{
    public partial class ClashDetectionWindow : Window
    {
        public ClashDetectionWindow(ClashDetectionViewModel viewModel)
        {
            // Ensure WPF Application exists (required for XAML resource loading in Revit)

            if (System.Windows.Application.Current == null)

            {

                new System.Windows.Application();

            }


            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

