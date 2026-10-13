using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Pile.Views;

namespace JNNTool.RebarSuite.Pile
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PileRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (!JNNTool.Tools.Login.Services.LicenseGuard.EnsureLicensed(commandData.Application))
                    return Result.Cancelled;

                var window = new PileRebarWindow();
                window.Show();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khởi chạy lệnh bố trí thép cọc", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
