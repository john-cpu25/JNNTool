using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Utilities.Engine;

namespace JNNTool.RebarSuite.Utilities.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RebarNumberCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var rebars = new FilteredElementCollector(doc)
                    .OfClass(typeof(Autodesk.Revit.DB.Structure.Rebar))
                    .WhereElementIsNotElementType()
                    .Cast<Autodesk.Revit.DB.Structure.Rebar>()
                    .ToList();

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("JNN - Đánh Số Hiệu Thép", "Không tìm thấy cốt thép nào trong dự án.");
                    return Result.Succeeded;
                }

                var items = new List<RebarNumberingItem>();

                foreach (var rb in rebars)
                {
                    var barType = doc.GetElement(rb.GetTypeId()) as RebarBarType;
                    string dia = barType?.Name ?? "D10";

                    int qty = rb.Quantity;
                    if (qty <= 0) qty = 1;
                    double lenMm = GeometryHelper.FeetToMm(rb.TotalLength / qty);

                    string shape = "Standard";
                    try
                    {
                        var shapeElem = doc.GetElement(rb.GetShapeId());
                        if (shapeElem != null) shape = shapeElem.Name;
                    }
                    catch { }

                    items.Add(new RebarNumberingItem
                    {
                        ElementId = rb.Id.GetIdValue(),
                        Diameter = dia,
                        LengthMm = lenMm,
                        ShapeName = shape
                    });
                }

                var numberedList = RebarNumberingEngine.AssignNumbers(items, startNumber: 1, lengthToleranceMm: 10.0);
                var markMap = numberedList.ToDictionary(x => x.ElementId, x => x.AssignedMark);

                int updatedCount = 0;
                using (var tx = new Transaction(doc, "JNN - Đánh số hiệu thép"))
                {
                    tx.Start();

                    foreach (var rb in rebars)
                    {
                        long id = rb.Id.GetIdValue();
                        if (markMap.TryGetValue(id, out int mark))
                        {
                            var markParam = rb.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
                            if (markParam != null && !markParam.IsReadOnly)
                            {
                                markParam.Set(mark.ToString());
                                updatedCount++;
                            }
                        }
                    }

                    tx.Commit();
                }

                int maxMark = markMap.Values.DefaultIfEmpty(0).Max();
                TaskDialog.Show("JNN - Đánh Số Hiệu Thép Thành Công", 
                    $"Đã đánh số hiệu tự động cho {updatedCount} cốt thép!\nTổng số ký hiệu phân loại: {maxMark} loại thanh.");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi đánh số hiệu thép", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
