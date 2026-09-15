using System;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.PileLocation.ViewModels;

namespace JNNTool.Tools.PileLocation
{
    /// <summary>
    /// Xử lý 2 hành động trong Revit thread:
    ///   Tab1 – Ghi toạ độ khảo sát vào cọc mẫu
    ///   Tab2 – Cập nhật toạ độ tất cả cọc còn lại trong view
    /// </summary>
    public class PileLocationExternalEventHandler : IExternalEventHandler
    {
        public PileLocationViewModel ViewModel { get; set; }

        public void Execute(UIApplication app)
        {
            if (ViewModel == null) return;

            switch (ViewModel.PendingAction)
            {
                case PileAction.WriteReferencePile:
                    ExecuteWriteReference(app);
                    break;
                case PileAction.UpdateAllPiles:
                    ExecuteUpdateAll(app);
                    break;
            }
        }

        // ── Tab 1: Ghi toạ độ khảo sát vào cọc mẫu ─────────────────────────
        private void ExecuteWriteReference(UIApplication app)
        {
            var vm  = ViewModel;
            var doc = vm.Doc;

            try
            {
                Element refPile = doc.GetElement(vm.RefPileId);
                if (refPile?.Location is not LocationPoint lp)
                {
                    vm.SetStatus("❌ Không đọc được vị trí cọc mẫu.", isError: true);
                    return;
                }

                ProjectPosition pos = doc.ActiveProjectLocation.GetProjectPosition(lp.Point);
                double xM = pos.NorthSouth * 0.3048;
                double yM = pos.EastWest   * 0.3048;

                using var t = new Transaction(doc, "Pile Location – Ghi toạ độ cọc mẫu");
                t.Start();

                bool okX = TrySetParam(refPile, "X Coordinate", xM);
                bool okY = TrySetParam(refPile, "Y Coordinate", yM);

                if (okX && okY)
                {
                    t.Commit();
                    // Cập nhật lại hiển thị trong UI
                    vm.RefParamX = xM.ToString("F2");
                    vm.RefParamY = yM.ToString("F2");
                    vm.SetStatus($"✓ Đã ghi X={xM:F2} m, Y={yM:F2} m vào cọc mẫu.");
                }
                else
                {
                    t.RollBack();
                    vm.SetStatus("❌ Không ghi được parameter – kiểm tra tên X/Y Coordinate.", isError: true);
                }
            }
            catch (Exception ex)
            {
                vm.SetStatus($"❌ Lỗi: {ex.Message}", isError: true);
            }
        }

        // ── Tab 2: Cập nhật tất cả cọc trong active view ────────────────────
        private void ExecuteUpdateAll(UIApplication app)
        {
            var vm       = ViewModel;
            var doc      = vm.Doc;
            var uidoc    = app.ActiveUIDocument;
            var activeView = uidoc?.ActiveView;

            if (activeView == null)
            {
                vm.SetStatus("❌ Không có active view.", isError: true);
                return;
            }

            try
            {
                var allPiles = new FilteredElementCollector(doc, activeView.Id)
                    .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(IsPile)
                    .Cast<Element>()
                    .ToList();

                int updatedCount = 0;
                var skipped = new List<string>();

                using var t = new Transaction(doc, "Pile Location – Cập nhật X/Y Coordinate");
                t.Start();

                foreach (Element pile in allPiles)
                {
                    // Bỏ qua cọc mẫu
                    if (pile.Id == vm.RefPileId) continue;
                    if (pile.Location is not LocationPoint lp) continue;

                    ProjectPosition pos = doc.ActiveProjectLocation.GetProjectPosition(lp.Point);
                    double xM = pos.NorthSouth * 0.3048;
                    double yM = pos.EastWest   * 0.3048;

                    bool okX = TrySetParam(pile, "X Coordinate", xM);
                    bool okY = TrySetParam(pile, "Y Coordinate", yM);

                    if (okX && okY)
                        updatedCount++;
                    else
                        skipped.Add(GetLabel(pile));
                }

                t.Commit();

                vm.UpdatedCount = updatedCount;
                vm.TotalCount   = allPiles.Count;

                string msg = $"✓ Cập nhật {updatedCount} / {allPiles.Count} cọc thành công.";
                if (skipped.Any())
                    msg += $"\n⚠ Bỏ qua: {string.Join(", ", skipped.Take(5))}";

                vm.SetStatus(msg);
            }
            catch (Exception ex)
            {
                vm.SetStatus($"❌ Lỗi: {ex.Message}", isError: true);
            }
        }

        // ── Shared helpers ────────────────────────────────────────────────────

        private static bool IsPile(FamilyInstance fi)
        {
            string fam  = fi.Symbol?.Family?.Name ?? string.Empty;
            string type = fi.Symbol?.Name          ?? string.Empty;
            return fam.IndexOf("Pile",  StringComparison.OrdinalIgnoreCase) >= 0
                || type.IndexOf("Pile", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TrySetParam(Element elem, string paramName, double value)
        {
            Parameter param = elem.LookupParameter(paramName);
            if (param == null || param.IsReadOnly) return false;
            return param.StorageType switch
            {
                StorageType.Double => TrySet(param, value),
                StorageType.String => TrySetStr(param, value.ToString("F2")),
                _                  => false
            };
        }

        private static bool TrySet(Parameter p, double v)
        {
            try { p.Set(v); return true; } catch { return false; }
        }

        private static bool TrySetStr(Parameter p, string v)
        {
            try { p.Set(v); return true; } catch { return false; }
        }

        private static string GetLabel(Element e)
            => (e as FamilyInstance)?.Symbol?.Name ?? e.Id.ToString();

        public string GetName() => "Pile Location External Event";
    }

    public enum PileAction { None, WriteReferencePile, UpdateAllPiles }
}
