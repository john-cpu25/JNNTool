using System;

namespace JNNTool.RebarSuite.Common
{
    /// <summary>
    /// Tỷ lệ phần trăm cốt thép nối chồng trên cùng một mặt cắt (theo TCVN 5574:2018).
    /// </summary>
    public enum LapSpliceRatio
    {
        Ratio25OrLess,  // <= 25% diện tích nối (alpha_l = 1.2)
        Ratio50OrLess,  // 25% - 50% diện tích nối (alpha_l = 1.4)
        Ratio100        // > 50% hoặc 100% nối tại một mặt cắt (alpha_l = 2.0)
    }

    /// <summary>
    /// Bộ tính toán chiều dài nối chồng cốt thép không hàn theo TCVN 5574:2018.
    /// </summary>
    public static class LapCalculator
    {
        /// <summary>
        /// Lấy hệ số nối chồng alpha_l theo TCVN 5574:2018 mục 10.3.8.3.
        /// </summary>
        public static double GetAlphaL(LapSpliceRatio ratio, bool isCompression = false)
        {
            if (isCompression) return 1.0;

            switch (ratio)
            {
                case LapSpliceRatio.Ratio25OrLess: return 1.2;
                case LapSpliceRatio.Ratio50OrLess: return 1.4;
                case LapSpliceRatio.Ratio100: return 2.0;
                default: return 1.4;
            }
        }

        /// <summary>
        /// Tính chiều dài nối chồng cốt thép ll (mm) theo TCVN 5574:2018.
        /// </summary>
        public static double CalculateLapLengthMm(
            int diameterMm,
            ConcreteGrade concreteGrade,
            RebarSteelGrade steelGrade,
            LapSpliceRatio ratio = LapSpliceRatio.Ratio50OrLess,
            bool isCompression = false,
            int roundMultipleMm = 50)
        {
            double l0_an = AnchorCalculator.CalculateBaseAnchorLengthMm(diameterMm, concreteGrade, steelGrade);
            double alphaL = GetAlphaL(ratio, isCompression);
            double ll = alphaL * l0_an;

            // Quy định giới hạn tối thiểu: không nhỏ hơn 0.4 * alpha_l * l0_an, 20d và 250 mm
            double minLength = Math.Max(20.0 * diameterMm, 250.0);
            minLength = Math.Max(minLength, 0.4 * alphaL * l0_an);

            double finalLength = Math.Max(ll, minLength);
            return GeometryHelper.RoundUpToMultiple(finalLength, roundMultipleMm);
        }

        /// <summary>
        /// Tính nhanh chiều dài nối chồng theo bội số đường kính (ví dụ 30d, 40d, 45d).
        /// </summary>
        public static double CalculateByMultiplier(int diameterMm, double multiplier = 40.0, int roundMultipleMm = 50)
        {
            double len = Math.Max(diameterMm * multiplier, 250.0);
            return GeometryHelper.RoundUpToMultiple(len, roundMultipleMm);
        }
    }
}
