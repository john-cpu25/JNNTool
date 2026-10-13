using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Core.UI;
using JNNTool.Tools.QuantityTakeoff.ViewModels;
using JNNTool.Tools.QuantityTakeoff.Views;

namespace JNNTool.Tools.QuantityTakeoff.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class QuantityTakeoffCommand : IExternalCommand
    {
        private static QuantityTakeoffWindow? _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                    return Result.Cancelled;

                if (_window != null && _window.IsLoaded)
                {
                    _window.Activate();
                    return Result.Succeeded;
                }

                UIApplication uiApp = commandData.Application;
                UIDocument? uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("JNN — Quantity Takeoff", "Vui lòng mở một dự án Revit trước khi chạy lệnh.");
                    return Result.Cancelled;
                }

                var handler = new QuantityTakeoffHandler();
                var exEvent = ExternalEvent.Create(handler);

                var vm = new QuantityTakeoffViewModel();
                vm.Initialize(uiApp, handler, exEvent);

                var window = new QuantityTakeoffWindow { DataContext = vm };

                window.Dispatcher.UnhandledException += (s, e) =>
                {
                    Logger.Error("Quantity Takeoff: unhandled UI exception", e.Exception);
                    e.Handled = true;
                };

                window.Closed += (s, e) =>
                {
                    _window = null;
                };

                _window = window;

                // Mở Modeless Window an toàn với WpfWindowHelper (ngăn chặn triệt để WPF Shutdown error)
                WpfWindowHelper.ShowModeless(window, uiApp);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Quantity Takeoff: Execute failed", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
