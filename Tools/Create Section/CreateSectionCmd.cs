using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace JNNTool
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSectionCmd : IExternalCommand
    {
        // Danh sách tên tham số phổ biến cho chiều rộng (b) và chiều cao (h)
        private static readonly string[] WidthParamNames  = { "b", "B", "Width", "width", "W", "w" };
        private static readonly string[] HeightParamNames = { "h", "H", "Depth", "depth", "Height", "height", "D", "d" };

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            var doc = uidoc.Document;

            try
            {
                // ── 1. Thu thập tất cả FamilySymbol dầm và cột ──────────────
                var beamSymbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .Cast<FamilySymbol>()
                    .ToList();

                var columnSymbols = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .Cast<FamilySymbol>()
                    .ToList();

                var allSymbols = beamSymbols.Concat(columnSymbols).ToList();

                // ── 2. Nhóm theo Family.Name ────────────────────────────────
                var beamFamilies = GroupByFamily(beamSymbols);
                var columnFamilies = GroupByFamily(columnSymbols);

                if (beamFamilies.Count == 0 && columnFamilies.Count == 0)
                {
                    TaskDialog.Show("Thông báo", "Không tìm thấy Family dầm hoặc cột nào trong dự án.");
                    return Result.Failed;
                }

                // ── 3. Tạo HashSet tên type đã tồn tại ──────────────────────
                var existingTypeNames = new HashSet<string>(
                    allSymbols.Select(s => s.Name),
                    StringComparer.OrdinalIgnoreCase);

                // ── 4. Hiển thị dialog ───────────────────────────────────────
                var dialog = new CreateSectionWindow(beamFamilies, columnFamilies, existingTypeNames);
                bool? result = dialog.ShowDialog();

                if (result != true || !dialog.CreateRequested)
                    return Result.Cancelled;

                // ── 5. Tìm FamilySymbol gốc ─────────────────────────────────
                string selectedFamily = dialog.SelectedFamily;
                string selectedBaseType = dialog.SelectedBaseType;
                bool isBeam = dialog.SelectedCategory.Contains("Dầm");

                var sourceSymbols = isBeam ? beamSymbols : columnSymbols;
                FamilySymbol baseSymbol = sourceSymbols.FirstOrDefault(
                    s => s.FamilyName == selectedFamily && s.Name == selectedBaseType);

                if (baseSymbol == null)
                {
                    TaskDialog.Show("Lỗi", $"Không tìm thấy type gốc: {selectedFamily} : {selectedBaseType}");
                    return Result.Failed;
                }

                // ── 6. Duplicate hàng loạt trong Transaction ─────────────────
                var toCreate = dialog.ParsedSections.Where(s => s.Status == "⊕ Tạo mới").ToList();
                int createdCount = 0;
                int failedCount = 0;
                var failedNames = new List<string>();

                using (var tx = new Transaction(doc, "Tạo Section Hàng Loạt"))
                {
                    tx.Start();

                    foreach (var item in toCreate)
                    {
                        try
                        {
                            // Duplicate type
                            FamilySymbol newSymbol = baseSymbol.Duplicate(item.TypeName) as FamilySymbol;
                            if (newSymbol == null)
                            {
                                failedCount++;
                                failedNames.Add(item.TypeName);
                                continue;
                            }

                            // Gán kích thước b (mm → feet)
                            double bFt = item.B / 304.8;
                            double hFt = item.H / 304.8;

                            bool bSet = TrySetParameter(newSymbol, WidthParamNames, bFt);
                            bool hSet = TrySetParameter(newSymbol, HeightParamNames, hFt);

                            if (!bSet || !hSet)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"[CreateSection] {item.TypeName}: b={bSet}, h={hSet} — Một số tham số không tìm thấy.");
                            }

                            createdCount++;
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[CreateSection] Lỗi duplicate {item.TypeName}: {ex.Message}");
                            failedCount++;
                            failedNames.Add(item.TypeName);
                        }
                    }

                    tx.Commit();
                }

                // ── 7. Thông báo kết quả ────────────────────────────────────
                string summary = $"✅  Đã tạo: {createdCount} section mới.";
                if (failedCount > 0)
                    summary += $"\n⚠️  Lỗi: {failedCount} section ({string.Join(", ", failedNames)}).";

                TaskDialog.Show("Tạo Section Hoàn Tất", summary);
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Nhóm danh sách FamilySymbol theo Family.Name.
        /// </summary>
        private static Dictionary<string, List<string>> GroupByFamily(List<FamilySymbol> symbols)
        {
            return symbols
                .GroupBy(s => s.FamilyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(s => s.Name).Distinct().OrderBy(n => n).ToList());
        }

        /// <summary>
        /// Thử gán giá trị cho tham số theo danh sách tên ưu tiên.
        /// Tìm tham số đầu tiên khớp tên và gán giá trị (đơn vị feet).
        /// </summary>
        private static bool TrySetParameter(FamilySymbol symbol, string[] paramNames, double valueFt)
        {
            foreach (string name in paramNames)
            {
                // Tìm theo tên definition
                Parameter param = symbol.LookupParameter(name);
                if (param != null && !param.IsReadOnly && param.StorageType == StorageType.Double)
                {
                    param.Set(valueFt);
                    return true;
                }
            }
            return false;
        }
    }
}
