using System.Windows;
using JNNTool.Tools.RebarColumn.ViewModels;

namespace JNNTool.Tools.RebarColumn.UI
{
    public partial class ApplyToOtherFloorsWindow : Window
    {
        public ApplyToOtherFloorsWindow()
        {
            // Ensure WPF Application exists (required for XAML resource loading in Revit)

            if (System.Windows.Application.Current == null)

            {

                new System.Windows.Application();

            }


            InitializeComponent();
            DataContext = new ApplyToOtherFloorsViewModel();
        }

        private void OnXongClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}

