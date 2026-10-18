using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.QuantityTakeoff.Helpers;
using JNNTool.Tools.QuantityTakeoff.ViewModels;
using JNNTool.Tools.QuantityTakeoff.Views;

namespace JNNTool.Tools.QuantityTakeoff.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class QuantityTakeoffCommand : IExternalCommand
    {
        private static QuantityTakeoffWindow _window;
        private static QuantityTakeoffHandler _handler;
        private static ExternalEvent _externalEvent;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_window != null && _window.IsLoaded)
                {
                    _window.Activate();
                    return Result.Succeeded;
                }

                UIApplication uiApp = commandData.Application;
                UIDocument uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("JNNTool — Quantity Takeoff", "Vui lòng mở một dự án Revit trước khi chạy lệnh.");
                    return Result.Cancelled;
                }

                if (_handler == null)
                {
                    _handler = new QuantityTakeoffHandler();
                    _externalEvent = ExternalEvent.Create(_handler);
                }

                var vm = new QuantityTakeoffViewModel();
                vm.Initialize(uiApp, _handler, _externalEvent);

                // Trích xuất khối lượng trực tiếp và đồng bộ ngay trên Revit API thread
                vm.ExecuteExtraction(uiApp);

                var window = new QuantityTakeoffWindow { DataContext = vm };

                window.Dispatcher.UnhandledException += (s, e) =>
                {
                    QtoLogger.Error("Quantity Takeoff: unhandled UI exception", e.Exception);
                    e.Handled = true;
                };

                window.Closed += (s, e) =>
                {
                    _window = null;
                };

                _window = window;

                // Modeless Window an toàn bám theo Revit MainWindow
                try
                {
                    var interop = new WindowInteropHelper(window);
                    interop.Owner = uiApp.MainWindowHandle;
                }
                catch { }

                window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                QtoLogger.Error("Quantity Takeoff: Execute failed", ex);
                message = ex.Message;
                string innerDetail = ex.InnerException != null ? $"\n\nChi tiết nguyên nhân:\n{ex.InnerException.Message}" : "";
                TaskDialog.Show("JNNTool — Quantity Takeoff", $"Đã xảy ra lỗi khi khởi động công cụ:\n\n{ex.Message}{innerDetail}\n\nChi tiết:\n{ex.StackTrace}");
                return Result.Failed;
            }
        }
    }
}
