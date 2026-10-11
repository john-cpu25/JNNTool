#nullable enable
using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Tools.AutoDimColumn.ViewModels;
using JNNTool.Tools.AutoDimColumn.Views;

namespace JNNTool.Tools.AutoDimColumn.Commands
{
    /// <summary>
    /// Lệnh mở cửa sổ JNN Auto Column Dimension (Modeless).
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class AutoDimColumnCommand : IExternalCommand
    {
        private static AutoDimColumnWindow? _window;

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
                UIDocument? uidoc = uiApp.ActiveUIDocument;
                if (uidoc == null)
                {
                    TaskDialog.Show("JNN - Auto Column Dimension", "Please open a Revit project first.");
                    return Result.Cancelled;
                }

                Document doc = uidoc.Document;
                View activeView = doc.ActiveView;

                if (activeView is not ViewPlan || activeView.ViewType == ViewType.CeilingPlan)
                {
                    TaskDialog.Show("JNN - Auto Column Dimension",
                        "Please open a Structural / Floor Plan View before launching Auto Column Dimension.");
                    return Result.Cancelled;
                }

                var handler = new AutoDimColumnHandler();
                var exEvent = ExternalEvent.Create(handler);

                var vm = new AutoDimColumnViewModel();
                vm.Initialize(uiApp, handler, exEvent);

                var window = new AutoDimColumnWindow { DataContext = vm };

                // Attach owner to Revit to keep window above view
                try
                {
                    new WindowInteropHelper(window).Owner = uiApp.MainWindowHandle;
                }
                catch (Exception ex)
                {
                    Logger.Error("Auto Column Dimension: unable to set window owner", ex);
                }

                // Catch uncaught UI exceptions to avoid Revit crashing
                window.Dispatcher.UnhandledException += (s, e) =>
                {
                    Logger.Error("Auto Column Dimension: unhandled UI exception", e.Exception);
                    e.Handled = true;
                };

                window.Closed += (s, e) =>
                {
                    _window = null;
                };

                _window = window;
                window.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Auto Column Dimension: Execute failed", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
