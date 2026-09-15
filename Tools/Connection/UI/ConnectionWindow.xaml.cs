using System.Windows;
using Autodesk.Revit.UI;
using JNNTool.Tools.Connection.ViewModels;

namespace JNNTool.Tools.Connection.UI
{
    public partial class ConnectionWindow : Window
    {
        public ConnectionWindow(UIDocument uidoc, ExternalEvent externalEvent, ConnectionEventHandler handler)
        {
            InitializeComponent();
            DataContext = new ConnectionViewModel(uidoc, externalEvent, handler);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

