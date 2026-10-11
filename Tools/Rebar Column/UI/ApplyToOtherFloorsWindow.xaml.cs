using System.Windows;
using JNNTool.Tools.RebarColumn.ViewModels;

namespace JNNTool.Tools.RebarColumn.UI
{
    public partial class ApplyToOtherFloorsWindow : Window
    {
        public ApplyToOtherFloorsWindow()
        {
            JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();


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

