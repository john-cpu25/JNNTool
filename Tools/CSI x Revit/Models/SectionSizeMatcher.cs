using System;

namespace JNNTool.Tools.CSIxRevit.Models
{
    /// <summary>
    /// So khớp kích thước tiết diện ETABS với type Revit (mm). Thuần C# để unit test.
    /// </summary>
    public static class SectionSizeMatcher
    {
        /// <summary>Sai số chấp nhận (mm): đổi feet ↔ mm có thể lệch phần nhỏ.</summary>
        public const double ToleranceMm = 0.5;

        public static bool AlmostEqual(double a, double b) => Math.Abs(a - b) <= ToleranceMm;

        public static bool ThicknessMatches(double wantMm, double actualMm) =>
            wantMm > 0 && actualMm > 0 && AlmostEqual(wantMm, actualMm);

        /// <summary>
        /// Tiết diện chữ nhật. Cột cho phép đảo b/h vì hướng cột đã được ColumnRotationHelper xoay lại khi dựng.
        /// </summary>
        public static bool RectMatches(double wantB, double wantH, double b, double h, bool allowSwap)
        {
            if (wantB <= 0 || wantH <= 0 || b <= 0 || h <= 0) return false;
            if (AlmostEqual(wantB, b) && AlmostEqual(wantH, h)) return true;
            return allowSwap && AlmostEqual(wantB, h) && AlmostEqual(wantH, b);
        }

        public static bool CircleMatches(double wantD, double d) =>
            wantD > 0 && d > 0 && AlmostEqual(wantD, d);
    }
}
