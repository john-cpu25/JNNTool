using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Utilities.Models;

namespace JNNTool.RebarSuite.Utilities.Engine
{
    public sealed class RawRebarInput
    {
        public string Diameter { get; set; } = "D10";
        public int Quantity { get; set; } = 1;
        public double LengthMm { get; set; } = 0.0;
        public string HostCategory { get; set; } = "Other";
        public string LevelName { get; set; } = "Default";
    }

    /// <summary>
    /// Thuật toán thuần C# thống kê tổng khối lượng và chiều dài cốt thép (không gọi Revit API).
    /// </summary>
    public static class RebarVolumeCalculator
    {
        public static List<RebarVolumeItem> CalculateByDiameter(IEnumerable<RawRebarInput> rebars)
        {
            var result = new List<RebarVolumeItem>();
            if (rebars == null) return result;

            var grouped = rebars.GroupBy(r => BarCatalog.Parse(r.Diameter)?.Name ?? r.Diameter);

            foreach (var group in grouped.OrderBy(g => BarCatalog.Parse(g.Key)?.NominalDiameter ?? 0))
            {
                var barInfo = BarCatalog.Parse(group.Key) ?? new BarInfo(10);
                double totalLenM = group.Sum(r => (r.LengthMm / 1000.0) * r.Quantity);
                int totalBars = group.Sum(r => r.Quantity);

                result.Add(new RebarVolumeItem
                {
                    Diameter = barInfo.Name,
                    NominalDiameter = barInfo.NominalDiameter,
                    UnitWeightKgPerM = barInfo.WeightKgPerM,
                    TotalBars = totalBars,
                    TotalLengthM = Math.Round(totalLenM, 2)
                });
            }

            return result;
        }

        public static double GetTotalWeightTon(IEnumerable<RebarVolumeItem> items)
        {
            return items?.Sum(i => i.TotalWeightTon) ?? 0.0;
        }
    }
}
