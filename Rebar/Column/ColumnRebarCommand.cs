using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Column.Views;

namespace JNNTool.RebarSuite.Column
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ColumnRebarCommand : IExternalCommand
    {
        private static ColumnRebarWindow? _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new ColumnRebarWindow();
                    _window.Closed += (s, e) => _window = null;
                    _window.Show();
                }
                else
                {
                    _window.Activate();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi mở cửa sổ Thép Cột", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
