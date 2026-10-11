using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Tools.QuantityTakeoff.Models;

namespace JNNTool.Tools.QuantityTakeoff.Engine
{
    public static class QuantityExtractorEngine
    {
        private const double FeetToMeters = 0.3048;
        private const double SqFeetToSqMeters = 0.09290304;
        private const double CuFeetToCuMeters = 0.028316846592;
        private const double SteelDensityKgPerM3 = 7850.0;

        /// <summary>
        /// Danh sách các BuiltInCategory mặc định được hỗ trợ trích xuất khối lượng.
        /// </summary>
        public static readonly List<CategoryFilterItem> DefaultCategories = new List<CategoryFilterItem>
        {
            new CategoryFilterItem { Name = "Structural Framing", VietnameseName = "Dầm kết cấu", BuiltInCategory = BuiltInCategory.OST_StructuralFraming, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Structural Columns", VietnameseName = "Cột kết cấu", BuiltInCategory = BuiltInCategory.OST_StructuralColumns, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Floors", VietnameseName = "Sàn bê tông", BuiltInCategory = BuiltInCategory.OST_Floors, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Walls", VietnameseName = "Vách / Tường", BuiltInCategory = BuiltInCategory.OST_Walls, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Structural Foundation", VietnameseName = "Móng / Đài / Cọc", BuiltInCategory = BuiltInCategory.OST_StructuralFoundation, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Stairs", VietnameseName = "Cầu thang", BuiltInCategory = BuiltInCategory.OST_Stairs, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Structural Rebar", VietnameseName = "Cốt thép", BuiltInCategory = BuiltInCategory.OST_Rebar, IsStructural = true, IsSelected = true },
            new CategoryFilterItem { Name = "Columns", VietnameseName = "Cột kiến trúc", BuiltInCategory = BuiltInCategory.OST_Columns, IsStructural = false, IsSelected = false },
            new CategoryFilterItem { Name = "Roofs", VietnameseName = "Mái", BuiltInCategory = BuiltInCategory.OST_Roofs, IsStructural = false, IsSelected = false },
            new CategoryFilterItem { Name = "Ceilings", VietnameseName = "Trần", BuiltInCategory = BuiltInCategory.OST_Ceilings, IsStructural = false, IsSelected = false },
            new CategoryFilterItem { Name = "Generic Models", VietnameseName = "Cấu kiện khác", BuiltInCategory = BuiltInCategory.OST_GenericModel, IsStructural = false, IsSelected = false },
            new CategoryFilterItem { Name = "Doors", VietnameseName = "Cửa đi", BuiltInCategory = BuiltInCategory.OST_Doors, IsStructural = false, IsSelected = false },
            new CategoryFilterItem { Name = "Windows", VietnameseName = "Cửa sổ", BuiltInCategory = BuiltInCategory.OST_Windows, IsStructural = false, IsSelected = false }
        };

        /// <summary>
        /// Thu thập và trích xuất khối lượng chi tiết cho toàn bộ cấu kiện thỏa mãn bộ lọc.
        /// </summary>
        public static List<TakeoffElementItem> ExtractElements(
            Document doc,
            TakeoffScope scope,
            IEnumerable<BuiltInCategory> selectedCategories,
            View? activeView = null,
            ICollection<ElementId>? selectedElementIds = null)
        {
            var result = new List<TakeoffElementItem>();
            if (doc == null) return result;

            var targetCats = new HashSet<BuiltInCategory>(selectedCategories);
            if (targetCats.Count == 0) return result;

            // Lấy danh sách Level trong model để gán tầng cho cấu kiện không có LevelParam
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            var levelMap = levels.ToDictionary(l => l.Id.GetIdValue(), l => l);

            // Xây dựng danh sách Elements theo Scope cực nhanh
            IList<Element> elements;
            if (scope == TakeoffScope.Selection)
            {
                if (selectedElementIds == null || selectedElementIds.Count == 0)
                {
                    return result; // Không có đối tượng nào được chọn trong Selection
                }

                var targetIds = new HashSet<long>(targetCats.Select(c => (long)c));
                var selList = new List<Element>();
                foreach (var id in selectedElementIds)
                {
                    var e = doc.GetElement(id);
                    if (e != null && e.IsValidObject && !e.ViewSpecific && e.Category != null)
                    {
                        if (targetIds.Contains(e.Category.Id.GetIdValue()))
                        {
                            selList.Add(e);
                        }
                    }
                }
                elements = selList;
            }
            else if (scope == TakeoffScope.ActiveView && activeView != null)
            {
                elements = new FilteredElementCollector(doc, activeView.Id)
                    .WherePasses(new ElementMulticategoryFilter(targetCats.ToList()))
                    .WhereElementIsNotElementType()
                    .ToElements();
            }
            else
            {
                elements = new FilteredElementCollector(doc)
                    .WherePasses(new ElementMulticategoryFilter(targetCats.ToList()))
                    .WhereElementIsNotElementType()
                    .ToElements();
            }

            foreach (var elem in elements)
            {
                if (elem == null || !elem.IsValidObject) continue;

                var cat = elem.Category;
                if (cat == null) continue;

                BuiltInCategory bic = (BuiltInCategory)cat.Id.GetIdValue();
                if (!targetCats.Contains(bic)) continue;

                var item = ExtractSingleElement(doc, elem, bic, levelMap, levels);
                if (item != null)
                {
                    result.Add(item);
                }
            }

            return result;
        }

        private static TakeoffElementItem? ExtractSingleElement(
            Document doc,
            Element elem,
            BuiltInCategory bic,
            Dictionary<long, Level> levelMap,
            List<Level> allLevels)
        {
            try
            {
                var item = new TakeoffElementItem
                {
                    ElementId = elem.Id,
                    CategoryName = elem.Category?.Name ?? bic.ToString().Replace("OST_", ""),
                    BuiltInCategory = bic
                };

                // 1. Tên Family & Type
                ExtractFamilyAndType(doc, elem, item);

                // 2. Mark & Vật liệu
                ExtractMarkAndMaterial(doc, elem, item);

                // 3. Tầng (Level)
                ExtractLevel(doc, elem, item, levelMap, allLevels);

                // 4. Khối lượng (Volume, Area, Length, Weight)
                ExtractQuantities(doc, elem, bic, item);

                return item;
            }
            catch
            {
                return null;
            }
        }

        private static void ExtractFamilyAndType(Document doc, Element elem, TakeoffElementItem item)
        {
            if (elem is FamilyInstance fi)
            {
                item.FamilyName = fi.Symbol?.FamilyName ?? string.Empty;
                item.TypeName = fi.Symbol?.Name ?? elem.Name;
            }
            else
            {
                var typeId = elem.GetTypeId();
                if (typeId != null && typeId != ElementId.InvalidElementId)
                {
                    var elemType = doc.GetElement(typeId);
                    item.TypeName = elemType?.Name ?? elem.Name;
                    item.FamilyName = elemType?.get_Parameter(BuiltInParameter.ALL_MODEL_FAMILY_NAME)?.AsString()
                                      ?? elem.Category?.Name ?? string.Empty;
                }
                else
                {
                    item.TypeName = elem.Name;
                    item.FamilyName = elem.Category?.Name ?? string.Empty;
                }
            }
        }

        private static void ExtractMarkAndMaterial(Document doc, Element elem, TakeoffElementItem item)
        {
            // Mark
            var pMark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
                     ?? elem.get_Parameter(BuiltInParameter.DOOR_NUMBER)
                     ?? elem.LookupParameter("Mark")
                     ?? elem.LookupParameter("Số hiệu");
            if (pMark != null && pMark.HasValue)
            {
                item.Mark = pMark.AsString() ?? string.Empty;
            }

            // Material
            var pMat = elem.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)
                    ?? elem.LookupParameter("Material")
                    ?? elem.LookupParameter("Vật liệu");
            if (pMat != null && pMat.HasValue)
            {
                var matId = pMat.AsElementId();
                if (matId != null && matId != ElementId.InvalidElementId)
                {
                    var matElem = doc.GetElement(matId);
                    if (matElem != null) item.MaterialName = matElem.Name;
                }
                else
                {
                    item.MaterialName = pMat.AsString() ?? string.Empty;
                }
            }

            if (string.IsNullOrWhiteSpace(item.MaterialName))
            {
                var matIds = elem.GetMaterialIds(false);
                if (matIds != null && matIds.Count > 0)
                {
                    var matElem = doc.GetElement(matIds.First());
                    if (matElem != null) item.MaterialName = matElem.Name;
                }
            }
        }

        private static void ExtractLevel(
            Document doc,
            Element elem,
            TakeoffElementItem item,
            Dictionary<long, Level> levelMap,
            List<Level> allLevels)
        {
            Level? foundLevel = null;

            // A. Kiểm tra LevelId trực tiếp
            if (elem.LevelId != null && elem.LevelId != ElementId.InvalidElementId)
            {
                levelMap.TryGetValue(elem.LevelId.GetIdValue(), out foundLevel);
            }

            // B. Thử các BuiltInParameter phổ biến
            if (foundLevel == null)
            {
                var pLevel = elem.get_Parameter(BuiltInParameter.LEVEL_PARAM)
                          ?? elem.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)
                          ?? elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM)
                          ?? elem.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM)
                          ?? elem.get_Parameter(BuiltInParameter.STAIRS_BASE_LEVEL_PARAM)
                          ?? elem.get_Parameter(BuiltInParameter.WALL_BASE_CONSTRAINT)
                          ?? elem.get_Parameter(BuiltInParameter.ROOF_BASE_LEVEL_PARAM);

                if (pLevel != null && pLevel.HasValue)
                {
                    var lvlId = pLevel.AsElementId();
                    if (lvlId != null && lvlId != ElementId.InvalidElementId)
                    {
                        levelMap.TryGetValue(lvlId.GetIdValue(), out foundLevel);
                    }
                }
            }

            // C. Fallback: Dò theo cao độ Z của BoundingBox
            if (foundLevel == null && allLevels.Count > 0)
            {
                var bbox = elem.get_BoundingBox(null);
                if (bbox != null)
                {
                    double elemZ = (bbox.Min.Z + bbox.Max.Z) / 2.0;
                    // Tìm Level cao nhất mà Elevation <= elemZ
                    foundLevel = allLevels.LastOrDefault(l => l.Elevation <= elemZ + 0.1);
                    if (foundLevel == null)
                    {
                        foundLevel = allLevels.FirstOrDefault();
                    }
                }
            }

            if (foundLevel != null)
            {
                item.LevelName = foundLevel.Name;
                item.LevelElevation = foundLevel.Elevation * FeetToMeters;
            }
            else
            {
                item.LevelName = "Unassigned";
                item.LevelElevation = 0.0;
            }
        }

        private static void ExtractQuantities(
            Document doc,
            Element elem,
            BuiltInCategory bic,
            TakeoffElementItem item)
        {
            // --- 1. Xử lý riêng cho Cốt Thép (Rebar & RebarInSystem) ---
            if (elem is Rebar rebar)
            {
                ExtractRebarQuantities(rebar, item);
                return;
            }
            if (bic == BuiltInCategory.OST_Rebar || elem.Category?.Id.GetIdValue() == (long)BuiltInCategory.OST_Rebar)
            {
                ExtractGeneralRebarQuantities(elem, item);
                return;
            }

            // --- 2. Thể tích (Volume) qua Parameters ---
            double volCuFt = 0.0;
            var pVol = elem.get_Parameter(BuiltInParameter.HOST_VOLUME_COMPUTED)
                    ?? elem.LookupParameter("Volume")
                    ?? elem.LookupParameter("Thể tích");

            if (pVol != null && pVol.HasValue && pVol.StorageType == StorageType.Double)
            {
                volCuFt = pVol.AsDouble();
            }

            // --- 3. Diện tích (Area / Cốp pha) qua Parameters ---
            double areaSqFt = 0.0;
            var pArea = elem.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED)
                     ?? elem.get_Parameter(BuiltInParameter.SURFACE_AREA)
                     ?? elem.LookupParameter("Area")
                     ?? elem.LookupParameter("Diện tích");

            if (pArea != null && pArea.HasValue && pArea.StorageType == StorageType.Double)
            {
                areaSqFt = pArea.AsDouble();
            }

            // --- 4. Nếu chưa có Volume hoặc Area: tính từ Solid hình học 1 LẦN DUY NHẤT (DetailLevel.Coarse) ---
            if (volCuFt <= 1e-6 || areaSqFt <= 1e-6)
            {
                ComputeSolidsVolumeAndArea(elem, out double solidVol, out double solidArea);
                if (volCuFt <= 1e-6) volCuFt = solidVol;
                if (areaSqFt <= 1e-6)
                {
                    areaSqFt = solidArea;
                    // Với ván khuôn dầm/sàn, chia 2 để ước tính 1 mặt tiếp xúc
                    if (bic == BuiltInCategory.OST_Floors || bic == BuiltInCategory.OST_Walls)
                    {
                        areaSqFt /= 2.0;
                    }
                }
            }

            item.Volume = volCuFt * CuFeetToCuMeters;
            item.Area = areaSqFt * SqFeetToSqMeters;

            // --- 5. Chiều dài (Length) ---
            double lengthFt = 0.0;
            var pLen = elem.get_Parameter(BuiltInParameter.INSTANCE_LENGTH_PARAM)
                    ?? elem.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH)
                    ?? elem.LookupParameter("Length")
                    ?? elem.LookupParameter("Chiều dài");

            if (pLen != null && pLen.HasValue && pLen.StorageType == StorageType.Double)
            {
                lengthFt = pLen.AsDouble();
            }
            else if (elem.Location is LocationCurve locCurve && locCurve.Curve != null)
            {
                lengthFt = locCurve.Curve.Length;
            }

            item.Length = lengthFt * FeetToMeters;

            // --- 6. Khối lượng (Weight) nếu là Thép hình (Steel Framing / Column) ---
            if (item.MaterialName.IndexOf("Steel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.MaterialName.IndexOf("Thép", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                item.Weight = (item.Volume * SteelDensityKgPerM3) / 1000.0; // Tấn
            }
        }

        private static void ExtractRebarQuantities(Rebar rebar, TakeoffElementItem item)
        {
            double totalLenFt = 0.0;
            var pTotalLen = rebar.get_Parameter(BuiltInParameter.REBAR_ELEM_TOTAL_LENGTH)
                         ?? rebar.LookupParameter("Total Bar Length")
                         ?? rebar.LookupParameter("Tổng chiều dài");
            if (pTotalLen != null && pTotalLen.HasValue)
            {
                totalLenFt = pTotalLen.AsDouble();
            }
            else
            {
                try { totalLenFt = rebar.TotalLength; } catch { }
            }

            int count = rebar.Quantity;
            if (count < 1) count = 1;
            item.Count = count;

            double totalLenM = totalLenFt * FeetToMeters;
            item.Length = totalLenM;

            double barDiamMm = 10.0;
            var barType = rebar.Document.GetElement(rebar.GetTypeId()) as RebarBarType;
            if (barType != null)
            {
                barDiamMm = barType.BarModelDiameter * FeetToMeters * 1000.0;
            }

            double kgPerM = 0.006165 * barDiamMm * barDiamMm;
            double totalWeightKg = totalLenM * kgPerM;
            item.Weight = totalWeightKg / 1000.0; // Tấn
            item.Volume = totalWeightKg / SteelDensityKgPerM3;
            item.Area = 0.0;
        }

        private static void ExtractGeneralRebarQuantities(Element elem, TakeoffElementItem item)
        {
            double totalLenFt = 0.0;
            var pTotalLen = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_TOTAL_LENGTH)
                         ?? elem.LookupParameter("Total Bar Length")
                         ?? elem.LookupParameter("Length")
                         ?? elem.LookupParameter("Tổng chiều dài");
            if (pTotalLen != null && pTotalLen.HasValue)
            {
                totalLenFt = pTotalLen.AsDouble();
            }
            else if (elem is RebarInSystem ris)
            {
                try { totalLenFt = ris.TotalLength; } catch { }
            }

            int count = 1;
            var pQty = elem.get_Parameter(BuiltInParameter.REBAR_ELEM_QUANTITY_OF_BARS)
                    ?? elem.LookupParameter("Quantity");
            if (pQty != null && pQty.HasValue)
            {
                count = pQty.AsInteger();
            }
            if (count < 1) count = 1;
            item.Count = count;

            double totalLenM = totalLenFt * FeetToMeters;
            item.Length = totalLenM;

            double barDiamMm = 10.0;
            var typeId = elem.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
            {
                var barType = elem.Document.GetElement(typeId) as RebarBarType;
                if (barType != null)
                {
                    barDiamMm = barType.BarModelDiameter * FeetToMeters * 1000.0;
                }
            }

            double kgPerM = 0.006165 * barDiamMm * barDiamMm;
            double totalWeightKg = totalLenM * kgPerM;
            item.Weight = totalWeightKg / 1000.0; // Tấn
            item.Volume = totalWeightKg / SteelDensityKgPerM3;
            item.Area = 0.0;
        }

        private static void ComputeSolidsVolumeAndArea(Element elem, out double volume, out double area)
        {
            volume = 0.0;
            area = 0.0;
            try
            {
                var opt = new Options
                {
                    DetailLevel = ViewDetailLevel.Coarse, // Coarse giúp tính hình học siêu nhanh, tránh giật lag
                    ComputeReferences = false,
                    IncludeNonVisibleObjects = false
                };
                var geom = elem.get_Geometry(opt);
                if (geom == null) return;

                SumSolidsVolumeAndArea(geom, ref volume, ref area);
            }
            catch
            {
                // Bỏ qua lỗi hình học không hợp lệ
            }
        }

        private static void SumSolidsVolumeAndArea(GeometryElement geom, ref double volume, ref double area)
        {
            foreach (var obj in geom)
            {
                if (obj is Solid s && s.Faces.Size > 0)
                {
                    if (s.Volume > 1e-6) volume += s.Volume;
                    if (s.SurfaceArea > 1e-6) area += s.SurfaceArea;
                }
                else if (obj is GeometryInstance gi)
                {
                    var instGeom = gi.GetInstanceGeometry();
                    if (instGeom != null)
                    {
                        SumSolidsVolumeAndArea(instGeom, ref volume, ref area);
                    }
                }
            }
        }

        /// <summary>
        /// Tạo danh sách tổng hợp theo Tầng (LevelSummaryItem) kèm phân tách cấu kiện trong từng tầng.
        /// </summary>
        public static List<LevelSummaryItem> BuildLevelSummaries(IEnumerable<TakeoffElementItem> items)
        {
            var result = new List<LevelSummaryItem>();

            var grouped = items.GroupBy(i => i.LevelName)
                               .OrderBy(g => g.First().LevelElevation);

            foreach (var grp in grouped)
            {
                var lvlItem = new LevelSummaryItem
                {
                    LevelName = grp.Key,
                    Elevation = grp.First().LevelElevation,
                    ElementCount = grp.Sum(i => i.Count),
                    ElementIds = grp.Select(i => i.ElementId).Where(id => id != null).ToList()
                };

                // Tính tổng Volume, Formwork, Steel
                foreach (var i in grp)
                {
                    lvlItem.TotalVolume += i.Volume;

                    if (i.BuiltInCategory == BuiltInCategory.OST_Rebar ||
                        i.MaterialName.IndexOf("Steel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        i.MaterialName.IndexOf("Thép", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        lvlItem.SteelRebarWeight += i.Weight;
                    }
                    else
                    {
                        lvlItem.ConcreteVolume += i.Volume;
                        lvlItem.FormworkArea += i.Area;
                    }
                }

                // Chi tiết theo từng category trong tầng này
                lvlItem.CategoryBreakdowns = grp.GroupBy(i => i.CategoryName)
                    .Select(catGrp => new CategorySummaryItem
                    {
                        CategoryName = catGrp.Key,
                        BuiltInCategory = catGrp.First().BuiltInCategory,
                        Count = catGrp.Sum(x => x.Count),
                        TotalVolume = catGrp.Sum(x => x.Volume),
                        TotalArea = catGrp.Sum(x => x.Area),
                        TotalLength = catGrp.Sum(x => x.Length),
                        TotalWeight = catGrp.Sum(x => x.Weight),
                        ElementIds = catGrp.Select(x => x.ElementId).Where(id => id != null).ToList()
                    })
                    .OrderBy(c => c.CategoryName)
                    .ToList();

                result.Add(lvlItem);
            }

            return result;
        }

        /// <summary>
        /// Tạo danh sách tổng hợp theo Category (CategorySummaryItem).
        /// </summary>
        public static List<CategorySummaryItem> BuildCategorySummaries(IEnumerable<TakeoffElementItem> items)
        {
            return items.GroupBy(i => i.CategoryName)
                .Select(grp => new CategorySummaryItem
                {
                    CategoryName = grp.Key,
                    BuiltInCategory = grp.First().BuiltInCategory,
                    Count = grp.Sum(x => x.Count),
                    TotalVolume = grp.Sum(x => x.Volume),
                    TotalArea = grp.Sum(x => x.Area),
                    TotalLength = grp.Sum(x => x.Length),
                    TotalWeight = grp.Sum(x => x.Weight),
                    ElementIds = grp.Select(x => x.ElementId).Where(id => id != null).ToList()
                })
                .OrderBy(c => c.CategoryName)
                .ToList();
        }
    }
}
