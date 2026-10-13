using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.UI;
using JNNTool.Tools.UIManager.UI;
using JNNTool.Tools.UIManager.ViewModels;

namespace JNNTool.Tools.UIManager.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class UIManagerCmd : IExternalCommand
    {
        private static UIManagerWindow? _activeWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                    return Result.Cancelled;

                // Đảm bảo môi trường WPF an toàn, ngăn lỗi "The Application object is being shut down"
                WpfWindowHelper.EnsureApplicationContext();

                // Nếu cửa sổ đang mở thì focus mang lên trước
                if (_activeWindow != null && _activeWindow.IsLoaded)
                {
                    _activeWindow.Activate();
                    if (_activeWindow.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _activeWindow.WindowState = System.Windows.WindowState.Normal;
                    }
                    return Result.Succeeded;
                }

                var handler = new UIManagerExternalEventHandler();
                var viewModel = new UIManagerViewModel(handler);

                _activeWindow = new UIManagerWindow(viewModel);

                // Gán MainWindowHandle của Revit làm Owner
                try
                {
                    IntPtr hwnd = commandData.Application.MainWindowHandle;
                    if (hwnd != IntPtr.Zero)
                    {
                        var interop = new WindowInteropHelper(_activeWindow);
                        interop.Owner = hwnd;
                    }
                }
                catch { }

                _activeWindow.Closed += (s, e) => _activeWindow = null;
                _activeWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("JNNTool — Ribbon Theme", $"Lỗi khi mở giao diện Ribbon Theme: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
