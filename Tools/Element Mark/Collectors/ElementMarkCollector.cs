#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Tools.ElementMark.Models;

namespace JNNTool.Tools.ElementMark.Collectors
{
    /// <summary>Thông tin hiển thị trên bảng preview, không dùng cho engine.</summary>
    internal class ElementInfo
    {
        public string TypeName = "";
        public string OldMark = "";
    }

    internal class CollectedData
    {
        public List<BeamItem> Beams { get; } = new List<BeamItem>();
        public List<ColumnItem> Columns { get; } = new List<ColumnItem>();
        public List<FloorItem> Floors { get; } = new List<FloorItem>();
        public Dictionary<long, ElementInfo> Info { get; } = new Dictionary<long, ElementInfo>();
        public bool UsedViewFrame { get; set; }
    }

    /// <summary>Lọc phần tử theo phạm vi và đổi sang POCO (mm, toạ độ view).</summary>
    internal static class ElementMarkCollector
    {
        public static CollectedData Collect(Document doc, View activeView, ElementMarkSettings s)
        {
            var data = new CollectedData();

            // "Trái / phải / trên / dưới" phải theo đúng cái người dùng nhìn thấy, nên dùng hệ trục
            // của view mặt bằng (kể cả view bị xoay). View khác (3D, mặt cắt) thì dùng trục dự án.
            XYZ right = XYZ.BasisX, up = XYZ.BasisY;
            if (activeView is ViewPlan plan)
            {
                right = Flatten(plan.RightDirection) ?? XYZ.BasisX;
                up = Flatten(plan.UpDirection) ?? XYZ.BasisY;
                data.UsedViewFrame = true;
            }

            var levelNames = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToDictionary(l => l.Id.GetIdValue(), l => l.Name);

            bool activeOnly = s.Scope == MarkScope.ActiveView;

            if (s.BeamEnabled)
            {
                foreach (var fi in NewCollector(doc, activeView, activeOnly)
                             .OfCategory(BuiltInCategory.OST_StructuralFraming)
                             .OfClass(typeof(FamilyInstance))
                             .Cast<FamilyInstance>())
                {
                    if (fi.SuperComponent != null) continue;
                    if (fi.StructuralType == StructuralType.Brace) continue;
                    if (!(fi.Location is LocationCurve lc) || lc.Curve == null) continue;

                    XYZ p0 = lc.Curve.GetEndPoint(0), p1 = lc.Curve.GetEndPoint(1);
                    double x1 = ToMm(p0.DotProduct(right)), y1 = ToMm(p0.DotProduct(up));
                    double x2 = ToMm(p1.DotProduct(right)), y2 = ToMm(p1.DotProduct(up));
                    // Dầm gần như thẳng đứng không có phương trên mặt bằng.
                    if (Math.Abs(x2 - x1) < 1 && Math.Abs(y2 - y1) < 1) continue;

                    ElementId levelId = fi.get_Parameter(BuiltInParameter.INSTANCE_REFERENCE_LEVEL_PARAM)?.AsElementId()
                                        ?? ElementId.InvalidElementId;
                    if (levelId == ElementId.InvalidElementId) levelId = fi.LevelId;

                    long id = fi.Id.GetIdValue();
                    data.Beams.Add(new BeamItem(id, LevelName(levelNames, levelId), x1, y1, x2, y2));
                    AddInfo(data, fi, s);
                }
            }

            if (s.ColumnEnabled)
            {
                foreach (var fi in NewCollector(doc, activeView, activeOnly)
                             .OfCategory(BuiltInCategory.OST_StructuralColumns)
                             .OfClass(typeof(FamilyInstance))
                             .Cast<FamilyInstance>())
                {
                    if (fi.SuperComponent != null) continue;

                    XYZ? pt = null;
                    if (fi.Location is LocationPoint lp) pt = lp.Point;
                    else if (fi.Location is LocationCurve lcc && lcc.Curve != null)
                    {
                        // Cột xiên: lấy đầu thấp hơn làm chân cột.
                        XYZ a = lcc.Curve.GetEndPoint(0), b = lcc.Curve.GetEndPoint(1);
                        pt = a.Z <= b.Z ? a : b;
                    }
                    if (pt == null) continue;

                    ElementId levelId = fi.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId()
                                        ?? ElementId.InvalidElementId;
                    if (levelId == ElementId.InvalidElementId) levelId = fi.LevelId;

                    double baseZ = ToMm(pt.Z);
                    var bb = fi.get_BoundingBox(null);
                    if (bb != null) baseZ = ToMm(bb.Min.Z);

                    long id = fi.Id.GetIdValue();
                    data.Columns.Add(new ColumnItem(id, LevelName(levelNames, levelId),
                        ToMm(pt.DotProduct(right)), ToMm(pt.DotProduct(up)), baseZ));
                    AddInfo(data, fi, s);
                }
            }

            if (s.FloorEnabled)
            {
                foreach (var floor in NewCollector(doc, activeView, activeOnly)
                             .OfCategory(BuiltInCategory.OST_Floors)
                             .OfClass(typeof(Floor))
                             .Cast<Floor>())
                {
                    if (s.FloorStructuralOnly)
                    {
                        var p = floor.get_Parameter(BuiltInParameter.FLOOR_PARAM_IS_STRUCTURAL);
                        if (p == null || p.AsInteger() != 1) continue;
                    }

                    long id = floor.Id.GetIdValue();
                    data.Floors.Add(new FloorItem(id, LevelName(levelNames, floor.LevelId)));
                    AddInfo(data, floor, s);
                }
            }

            return data;
        }

        private static FilteredElementCollector NewCollector(Document doc, View view, bool activeOnly)
        {
            var c = activeOnly ? new FilteredElementCollector(doc, view.Id) : new FilteredElementCollector(doc);
            return c.WhereElementIsNotElementType();
        }

        private static void AddInfo(CollectedData data, Element e, ElementMarkSettings s)
        {
            var doc = e.Document;
            string typeName = doc.GetElement(e.GetTypeId()) is ElementType et
                ? $"{et.FamilyName}: {et.Name}"
                : "";
            data.Info[e.Id.GetIdValue()] = new ElementInfo
            {
                TypeName = typeName,
                OldMark = MarkParameterHelper.Get(e, s.TargetParameter)?.AsString() ?? ""
            };
        }

        private static string LevelName(Dictionary<long, string> names, ElementId id)
        {
            if (id == null || id == ElementId.InvalidElementId) return "";
            return names.TryGetValue(id.GetIdValue(), out var n) ? n : "";
        }

        private static XYZ? Flatten(XYZ v)
        {
            var flat = new XYZ(v.X, v.Y, 0);
            return flat.GetLength() < 1e-9 ? null : flat.Normalize();
        }

        private static double ToMm(double feet) => UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters);
    }

    internal static class MarkParameterHelper
    {
        /// <summary>"Mark" dùng BuiltInParameter để không phụ thuộc ngôn ngữ Revit; tên khác thì LookupParameter.</summary>
        public static Parameter? Get(Element e, string name)
        {
            if (e == null) return null;
            if (string.IsNullOrWhiteSpace(name) || string.Equals(name.Trim(), "Mark", StringComparison.OrdinalIgnoreCase))
                return e.get_Parameter(BuiltInParameter.ALL_MODEL_MARK);
            return e.LookupParameter(name.Trim());
        }
    }
}
