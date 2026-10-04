using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JNNTool.RebarSuite.Common
{
    /// <summary>
    /// Thông tin tiêu chuẩn của một loại đường kính thanh thép.
    /// </summary>
    public sealed class BarInfo
    {
        public string Name { get; }
        public int NominalDiameter { get; }
        public double DiameterMm => NominalDiameter;
        public double AreaMm2 { get; }
        public double WeightKgPerM { get; }

        public BarInfo(int diameter)
        {
            NominalDiameter = diameter;
            Name = $"D{diameter}";
            AreaMm2 = Math.PI * diameter * diameter / 4.0;
            // Khối lượng riêng danh định của thép: 7850 kg/m3 -> 0.00785 g/mm3
            WeightKgPerM = AreaMm2 * 0.00785;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Danh mục tiêu chuẩn các loại đường kính cốt thép thông dụng (TCVN / BS / ASTM).
    /// </summary>
    public static class BarCatalog
    {
        private static readonly Dictionary<int, BarInfo> _barsByDiameter = new Dictionary<int, BarInfo>();
        private static readonly List<BarInfo> _standardBars = new List<BarInfo>();

        static BarCatalog()
        {
            int[] diameters = { 6, 8, 10, 12, 14, 16, 18, 20, 22, 25, 28, 32 };
            foreach (var d in diameters)
            {
                var bar = new BarInfo(d);
                _barsByDiameter[d] = bar;
                _standardBars.Add(bar);
            }
        }

        /// <summary>
        /// Danh sách toàn bộ các loại thanh thép tiêu chuẩn từ D6 đến D32.
        /// </summary>
        public static IReadOnlyList<BarInfo> StandardBars => _standardBars;

        /// <summary>
        /// Tìm BarInfo theo đường kính số nguyên (ví dụ: 10 -> D10).
        /// </summary>
        public static BarInfo? GetByDiameter(int diameter)
        {
            _barsByDiameter.TryGetValue(diameter, out var bar);
            return bar;
        }

        /// <summary>
        /// Phân tích chuỗi đầu vào (ví dụ: "D10", "d10", "10", "phi 10", "T10", "10mm") thành BarInfo.
        /// </summary>
        public static BarInfo? Parse(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            var match = Regex.Match(input.Trim(), @"\d+");
            if (match.Success && int.TryParse(match.Value, out int dia))
            {
                return GetByDiameter(dia);
            }
            return null;
        }

        /// <summary>
        /// Tìm loại thanh thép gần nhất theo đường kính thực (mm).
        /// </summary>
        public static BarInfo GetClosest(double diameterMm)
        {
            return _standardBars.OrderBy(b => Math.Abs(b.DiameterMm - diameterMm)).First();
        }
    }
}
