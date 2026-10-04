using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Wall.Views;

namespace JNNTool.RebarSuite.Wall
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class WallRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var window = new WallRebarWindow();
                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khởi chạy lệnh bố trí thép vách", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
