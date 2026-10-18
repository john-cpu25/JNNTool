using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Interop;
using JNNTool.Tools.CreateView.ViewModels;

namespace JNNTool.Tools.CreateView.UI
{
    public partial class CreateViewWindow : Window
    {
        public CreateViewWindow(CreateViewViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            try
            {
                var mainHandle = Process.GetCurrentProcess().MainWindowHandle;
                if (mainHandle != IntPtr.Zero)
                {
                    new WindowInteropHelper(this).Owner = mainHandle;
                }
            }
            catch { }
        }
    }
}
