using System.Windows;
using JNNTool.Tools.SpeedOverriderElement.ViewModels;

namespace JNNTool.Tools.SpeedOverriderElement.UI
{
    public partial class SpeedOverriderWindow : Window
    {
        public SpeedOverriderWindow(SpeedOverriderViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
