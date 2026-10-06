using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.IFCEtabs.UI.Views;

namespace JNNTool.Tools.IFCEtabs.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class EtabsConverterCommand : IExternalCommand
    {
        private static EtabsConverterWindow? _currentWindow;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                JNNTool.Core.ExternalEvents.ActionEventHandler.Instance.Initialize();

                if (_currentWindow != null && _currentWindow.IsLoaded)
                {
                    _currentWindow.Activate();
                    return Result.Succeeded;
                }

                _currentWindow = new EtabsConverterWindow();
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
                TaskDialog.Show("Lỗi JNN ETABS Converter", $"Không thể mở cửa sổ JNN ETABS Converter:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
