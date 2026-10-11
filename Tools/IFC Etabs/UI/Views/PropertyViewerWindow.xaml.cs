using System.Windows;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.UI.ViewModels;

namespace JNNTool.Tools.IFCEtabs.UI.Views
{
    public partial class PropertyViewerWindow : Window
    {
        public PropertyViewerViewModel ViewModel => (PropertyViewerViewModel)DataContext;

        public PropertyViewerWindow()
        {
            JNNTool.Core.UI.WpfWindowHelper.EnsureApplicationContext();
            InitializeComponent();
        }

        public PropertyViewerWindow(Element initialElement) : this()
        {
            if (initialElement != null)
            {
                ViewModel.LoadFromElement(initialElement);
            }
        }
    }
}
