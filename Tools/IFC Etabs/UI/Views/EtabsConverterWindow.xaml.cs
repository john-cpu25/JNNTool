using System.Windows;

namespace JNNTool.Tools.IFCEtabs.UI.Views
{
    public partial class EtabsConverterWindow : Window
    {
        public EtabsConverterWindow()
        {
            if (System.Windows.Application.Current == null)
            {
                new System.Windows.Application();
            }
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
