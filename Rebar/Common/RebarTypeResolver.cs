using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace JNNTool.RebarSuite.Common
{
    /// <summary>
    /// Tiện ích tìm kiếm và khớp các kiểu thép (RebarBarType, RebarHookType, RebarCoverType) trong Revit Document.
    /// </summary>
    public static class RebarTypeResolver
    {
        /// <summary>
        /// Lấy toàn bộ danh sách RebarBarType trong dự án.
        /// </summary>
        public static IList<RebarBarType> GetAllBarTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RebarBarType))
                .Cast<RebarBarType>()
                .OrderBy(t => t.BarModelDiameter)
                .ToList();
        }

        /// <summary>
        /// Tìm RebarBarType phù hợp nhất theo đường kính danh định (mm) hoặc tên (ví dụ "D10", "10M").
        /// </summary>
        public static RebarBarType? ResolveBarType(Document doc, int diameterMm)
        {
            var types = GetAllBarTypes(doc);
            if (types.Count == 0) return null;

            // 1. Khớp theo tên chứa đường kính chính xác (ví dụ "D10", "10", "phi10")
            string targetName = $"D{diameterMm}";
            var byName = types.FirstOrDefault(t => 
                t.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase) ||
                t.Name.StartsWith(targetName + " ", StringComparison.OrdinalIgnoreCase) ||
                t.Name.StartsWith(targetName + "_", StringComparison.OrdinalIgnoreCase));
            if (byName != null) return byName;

            // 2. Khớp theo đường kính hình học thực tế (feet sang mm)
            double targetFeet = GeometryHelper.MmToFeet(diameterMm);
            var byDiameter = types
                .OrderBy(t => Math.Abs(t.BarModelDiameter - targetFeet))
                .FirstOrDefault();

            return byDiameter ?? types[0];
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RebarHookType trong dự án.
        /// </summary>
        public static IList<RebarHookType> GetAllHookTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RebarHookType))
                .Cast<RebarHookType>()
                .ToList();
        }

        /// <summary>
        /// Tìm RebarHookType theo góc uốn (90, 135, 180 độ).
        /// </summary>
        public static RebarHookType? ResolveHookType(Document doc, HookAngle angle)
        {
            if (angle == HookAngle.None) return null;

            var hooks = GetAllHookTypes(doc);
            double targetAngleRad = (double)angle * Math.PI / 180.0;
            const double tolerance = 0.05; // Sai số góc radian

            // Khớp theo thuộc tính HookAngle của RebarHookType
            return hooks.FirstOrDefault(h => Math.Abs(h.HookAngle - targetAngleRad) <= tolerance)
                   ?? hooks.FirstOrDefault(h => h.Name.Contains(((int)angle).ToString()));
        }

        /// <summary>
        /// Lấy toàn bộ danh sách RebarCoverType trong dự án.
        /// </summary>
        public static IList<RebarCoverType> GetAllCoverTypes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(RebarCoverType))
                .Cast<RebarCoverType>()
                .ToList();
        }

        /// <summary>
        /// Tìm RebarCoverType có chiều dày gần nhất với giá trị mm mong muốn.
        /// </summary>
        public static RebarCoverType? ResolveCoverType(Document doc, double targetCoverMm)
        {
            var covers = GetAllCoverTypes(doc);
            if (covers.Count == 0) return null;

            double targetFeet = GeometryHelper.MmToFeet(targetCoverMm);
            return covers.OrderBy(c => Math.Abs(c.CoverDistance - targetFeet)).FirstOrDefault();
        }
    }
}
