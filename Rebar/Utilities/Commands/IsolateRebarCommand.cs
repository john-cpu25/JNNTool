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
    public class IsolateRebarCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;
            var view = doc.ActiveView;

            try
            {
                var rebarIds = new FilteredElementCollector(doc, view.Id)
                    .OfClass(typeof(Autodesk.Revit.DB.Structure.Rebar))
                    .ToElementIds();

                if (rebarIds.Count == 0)
                {
                    TaskDialog.Show("JNN - Isolate Rebar", "Không tìm thấy cốt thép nào trong View hiện tại để cô lập.");
                    return Result.Succeeded;
                }

                using (var tx = new Transaction(doc, "JNN - Cô lập cốt thép"))
                {
                    tx.Start();
                    if (view.IsTemporaryHideIsolateActive())
                    {
                        view.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                    }
                    else
                    {
                        view.IsolateElementsTemporary(rebarIds);
                    }
                    tx.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi cô lập cốt thép", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
