using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Foundation.Views;

namespace JNNTool.RebarSuite.Foundation
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FoundationRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                var window = new FoundationRebarWindow();
                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khởi chạy lệnh bố trí thép móng", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
