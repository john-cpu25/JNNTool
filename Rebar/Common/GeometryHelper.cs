using System;

namespace JNNTool.RebarSuite.Common
{
    /// <summary>
    /// Các thuật toán toán học và hình học thuần C# phục vụ tính toán cốt thép.
    /// Toàn bộ đơn vị đầu vào và ra là milimet (mm), độc lập hoàn toàn với Revit API.
    /// </summary>
    public static class GeometryHelper
    {
        public const double MM_PER_FOOT = 304.8;

        public static double FeetToMm(double feet) => feet * MM_PER_FOOT;
        public static double MmToFeet(double mm) => mm / MM_PER_FOOT;

        /// <summary>
        /// Làm tròn lên bội số xác định (ví dụ bội số 50 mm hoặc 10 mm).
        /// </summary>
        public static double RoundUpToMultiple(double value, int multiple = 50)
        {
            if (multiple <= 0) return value;
            return Math.Ceiling(value / multiple) * multiple;
        }

        /// <summary>
        /// Làm tròn xuống bội số xác định.
        /// </summary>
        public static double RoundDownToMultiple(double value, int multiple = 50)
        {
            if (multiple <= 0) return value;
            return Math.Floor(value / multiple) * multiple;
        }

        /// <summary>
        /// Làm tròn tới bội số gần nhất.
        /// </summary>
        public static double RoundToNearestMultiple(double value, int multiple = 50)
        {
            if (multiple <= 0) return value;
            return Math.Round(value / multiple, MidpointRounding.AwayFromZero) * multiple;
        }

        /// <summary>
        /// Tính toán số lượng thanh thép và khoảng rải thực tế trong một phạm vi rải.
        /// </summary>
        /// <param name="rangeLengthMm">Chiều dài phạm vi rải (mm)</param>
        /// <param name="coverStartMm">Khoảng lùi thanh đầu tiên (mm)</param>
        /// <param name="coverEndMm">Khoảng lùi thanh cuối cùng (mm)</param>
        /// <param name="targetSpacingMm">Khoảng cách rải mong muốn (ví dụ 150mm, 200mm)</param>
        /// <param name="barCount">Số thanh rải ra</param>
        /// <param name="actualSpacingMm">Khoảng rải đều thực tế (mm)</param>
        public static void CalculateSpacing(
            double rangeLengthMm,
            double coverStartMm,
            double coverEndMm,
            double targetSpacingMm,
            out int barCount,
            out double actualSpacingMm)
        {
            double netLength = rangeLengthMm - coverStartMm - coverEndMm;
            if (netLength <= 0 || targetSpacingMm <= 0)
            {
                barCount = 1;
                actualSpacingMm = 0;
                return;
            }

            // Số khoảng chia (số bước rải) = làm tròn lên của netLength / targetSpacing
            int intervals = (int)Math.Ceiling(netLength / targetSpacingMm);
            if (intervals < 1) intervals = 1;

            barCount = intervals + 1;
            actualSpacingMm = netLength / intervals;
        }
    }
}
