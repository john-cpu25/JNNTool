using System.Windows;

namespace JNNTool
{
    public partial class RebarBeamWindow : Window
    {
        public RebarBeamWindow(RebarBeamViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
