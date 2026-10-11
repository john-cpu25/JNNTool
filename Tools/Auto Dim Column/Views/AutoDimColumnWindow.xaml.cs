using System.Windows;
using JNNTool.Tools.AutoDimColumn.ViewModels;

namespace JNNTool.Tools.AutoDimColumn.Views
{
    public partial class AutoDimColumnWindow : Window
    {
        public AutoDimColumnWindow()
        {
            InitializeComponent();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
