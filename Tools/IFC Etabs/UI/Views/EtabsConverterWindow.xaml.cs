using System.Windows;

namespace JNNTool.Tools.IFCEtabs.UI.Views
{
    public partial class EtabsConverterWindow : Window
    {
        public EtabsConverterWindow()
        {
            JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
