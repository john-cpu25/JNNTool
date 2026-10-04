using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Utilities.Engine
{
    public sealed class RebarNumberingItem
    {
        public long ElementId { get; set; }
        public string Diameter { get; set; } = "D10";
        public double LengthMm { get; set; } = 0.0;
        public string ShapeName { get; set; } = "Standard";
        public int AssignedMark { get; set; } = 0;
    }

    /// <summary>
    /// Thuật toán thuần C# đánh số hiệu cốt thép tự động (Mark) theo tiêu chuẩn TCVN.
    /// </summary>
    public static class RebarNumberingEngine
    {
        public static List<RebarNumberingItem> AssignNumbers(IEnumerable<RebarNumberingItem> items, int startNumber = 1, double lengthToleranceMm = 10.0)
        {
            var result = new List<RebarNumberingItem>();
            if (items == null) return result;

            var list = items.ToList();

            // Sắp xếp theo đường kính tăng dần, sau đó theo chiều dài tăng dần
            var sorted = list.OrderBy(x => BarCatalog.Parse(x.Diameter)?.NominalDiameter ?? 0)
                             .ThenBy(x => Math.Round(x.LengthMm / lengthToleranceMm) * lengthToleranceMm)
                             .ThenBy(x => x.ShapeName)
                             .ToList();

            int currentMark = startNumber;

            // Nhóm các thanh tương tự nhau
            var groups = new List<List<RebarNumberingItem>>();

            foreach (var item in sorted)
            {
                var existingGroup = groups.FirstOrDefault(g =>
                {
                    var rep = g[0];
                    bool sameDia = (BarCatalog.Parse(rep.Diameter)?.NominalDiameter ?? 0) == (BarCatalog.Parse(item.Diameter)?.NominalDiameter ?? 0);
                    bool sameLen = Math.Abs(rep.LengthMm - item.LengthMm) <= lengthToleranceMm;
                    bool sameShape = rep.ShapeName == item.ShapeName;
                    return sameDia && sameLen && sameShape;
                });

                if (existingGroup != null)
                {
                    existingGroup.Add(item);
                }
                else
                {
                    groups.Add(new List<RebarNumberingItem> { item });
                }
            }

            foreach (var g in groups)
            {
                foreach (var item in g)
                {
                    item.AssignedMark = currentMark;
                    result.Add(item);
                }
                currentMark++;
            }

            return result;
        }
    }
}
