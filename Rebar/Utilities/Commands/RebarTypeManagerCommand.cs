using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Utilities.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RebarTypeManagerCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var existingTypes = new FilteredElementCollector(doc)
                    .OfClass(typeof(RebarBarType))
                    .Cast<RebarBarType>()
                    .ToList();

                if (existingTypes.Count == 0)
                {
                    TaskDialog.Show("JNN - Rebar Type Manager", "Không tìm thấy RebarBarType mẫu trong dự án để nhân bản.");
                    return Result.Failed;
                }

                var templateType = existingTypes[0];
                int createdCount = 0;

                int[] standardDiameters = { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32 };

                using (var tx = new Transaction(doc, "JNN - Chuẩn hóa RebarBarType D6-D32"))
                {
                    tx.Start();

                    foreach (var dia in standardDiameters)
                    {
                        string typeName = $"D{dia}";
                        bool exists = existingTypes.Any(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));

                        if (!exists)
                        {
                            try
                            {
                                var newType = templateType.Duplicate(typeName) as RebarBarType;
                                if (newType != null)
                                {
                                    double diaFeet = GeometryHelper.MmToFeet(dia);
                                    newType.BarModelDiameter = diaFeet;
                                    newType.BarNominalDiameter = diaFeet;

                                    createdCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Warning($"Không thể tạo loại thép {typeName}: {ex.Message}");
                            }
                        }
                    }

                    tx.Commit();
                }

                TaskDialog.Show("JNN - Rebar Type Manager", 
                    $"Đã kiểm tra và tạo mới {createdCount} RebarBarType chuẩn TCVN (D6–D32)!");

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi quản lý loại cốt thép", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
