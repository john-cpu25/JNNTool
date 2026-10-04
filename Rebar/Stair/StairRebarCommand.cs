using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Stair.Views;

namespace JNNTool.RebarSuite.Stair
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class StairRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var window = new StairRebarWindow();
                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khởi chạy lệnh bố trí thép thang", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
