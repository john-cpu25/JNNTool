using System.Windows;
using JNNTool.Tools.PileLocation.ViewModels;

namespace JNNTool.Tools.PileLocation.UI
{
    public partial class PileLocationWindow : Window
    {
        public PileLocationWindow(PileLocationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
