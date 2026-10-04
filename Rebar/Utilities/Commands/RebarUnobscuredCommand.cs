using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;

namespace JNNTool.RebarSuite.Utilities.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RebarUnobscuredCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            try
            {
                var rebars = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Autodesk.Revit.DB.Structure.Rebar))
                    .Cast<Autodesk.Revit.DB.Structure.Rebar>()
                    .ToList();

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("JNN - Rebar Unobscured", "Không tìm thấy cốt thép nào trong View hiện tại.");
                    return Result.Succeeded;
                }

                using (var tx = new Transaction(doc, "JNN - Bật Rebar Unobscured"))
                {
                    tx.Start();
                    int count = 0;
                    foreach (var rb in rebars)
                    {
                        try
                        {
                            rb.SetUnobscuredInView(view, true);
                            count++;
                        }
                        catch { }
                    }
                    tx.Commit();

                    TaskDialog.Show("JNN - Rebar Unobscured", $"Đã bật hiển thị nhìn xuyên (Unobscured) cho {count} cốt thép trong view!");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi bật Rebar Unobscured", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
