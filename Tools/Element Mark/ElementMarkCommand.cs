#nullable enable
using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Tools.ElementMark.ViewModels;
using JNNTool.Tools.ElementMark.Views;

namespace JNNTool.Tools.ElementMark
{
    /// <summary>Mở cửa sổ Element Mark (modeless, một instance).</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ElementMarkCommand : IExternalCommand
    {
        private static ElementMarkWindow? _window;

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
                    TaskDialog.Show("JNN - Element Mark", "Please open a Revit project first.");
                    return Result.Cancelled;
                }

                var vm = new ElementMarkViewModel(ElementMarkService.ReadLevels(uidoc.Document));
                var window = new ElementMarkWindow { DataContext = vm };

                // Attach owner to Revit to keep window above view
                try { new WindowInteropHelper(window).Owner = uiApp.MainWindowHandle; }
                catch (Exception ex) { Logger.Error("Element Mark: unable to set window owner", ex); }

                // Catch uncaught UI exceptions to avoid Revit crashing
                window.Dispatcher.UnhandledException += (s, e) =>
                {
                    Logger.Error("Element Mark: unhandled UI exception", e.Exception);
                    e.Handled = true;
                };

                window.Closed += (s, e) =>
                {
                    vm.Cleanup();
                    _window = null;
                };

                _window = window;
                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Error opening Element Mark", ex);
                TaskDialog.Show("JNN - Element Mark", $"Unable to open Element Mark:\n\n{ex.GetType().Name}: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
