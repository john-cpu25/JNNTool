using System;
using System.Linq;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JNNTool.Tools.IFCEtabs.UI.Views;

namespace JNNTool.Tools.IFCEtabs.Commands
{
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class PropertyViewerCommand : IExternalCommand
    {
        private static PropertyViewerWindow? _currentWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                JNNTool.Core.ExternalEvents.ActionEventHandler.Instance.Initialize();

                var uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null) return Result.Cancelled;

                Element? selectedElement = null;
                var selIds = uidoc.Selection.GetElementIds();
                if (selIds.Count > 0)
                {
                    selectedElement = uidoc.Document.GetElement(selIds.First());
                }

                if (_currentWindow != null && _currentWindow.IsLoaded)
                {
                    if (selectedElement != null)
                    {
                        _currentWindow.ViewModel.LoadFromElement(selectedElement);
                    }
                    _currentWindow.Activate();
                    return Result.Succeeded;
                }

                _currentWindow = new PropertyViewerWindow(selectedElement!);
                IntPtr revitHandle = commandData.Application.MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    new WindowInteropHelper(_currentWindow).Owner = revitHandle;
                }

                _currentWindow.Closed += (s, e) => _currentWindow = null;
                _currentWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Lỗi Property Viewer", $"Không thể mở Property Viewer:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
