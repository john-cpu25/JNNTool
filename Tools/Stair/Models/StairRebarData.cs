using System.Collections.Generic;

namespace JNN.Stair.Models
{
    /// <summary>
    /// Tọa độ điểm 3D độc lập (không phụ thuộc Autodesk.Revit.DB.XYZ để chạy unit test độc lập)
    /// </summary>
    public struct StairPoint3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public StairPoint3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static StairPoint3D operator +(StairPoint3D a, StairPoint3D b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static StairPoint3D operator -(StairPoint3D a, StairPoint3D b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static StairPoint3D operator *(StairPoint3D a, double scale) => new(a.X * scale, a.Y * scale, a.Z * scale);

        public double DistanceTo(StairPoint3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    /// <summary>
    /// Dữ liệu biểu diễn một thanh thép hoặc một bộ thanh thép trong bản thang
    /// </summary>
    public class StairRebarData
    {
        public string Id { get; set; } = System.Guid.NewGuid().ToString("N");
        public string Name { get; set; } = "Thép bản thang";
        public StairRebarLayer Layer { get; set; } = StairRebarLayer.BottomMain;

        // Thông số tiết diện & khoảng cách
        public double BarDiameterMm { get; set; } = 10.0;
        public double SpacingMm { get; set; } = 150.0;
        public int BarCount { get; set; } = 1;

        // Bề rộng rải thép (mm)
        public double ArrayLengthMm { get; set; } = 1000.0;

        // Đường dẫn tim thép (Polylines các điểm nút)
        public List<StairPoint3D> CenterlinePoints { get; set; } = new();

        // Neo & Uốn
        public bool HasStartAnchor { get; set; } = true;
        public bool HasEndAnchor { get; set; } = true;
        public double StartAnchorLengthMm { get; set; } = 250.0;
        public double EndAnchorLengthMm { get; set; } = 250.0;
        public bool BendFlipStart { get; set; } = false;
        public bool BendFlipEnd { get; set; } = false;

        // Nguyên tắc góc lõm
        public bool IsDiagonalCrossingAtConcaveCorner { get; set; } = true;

        // Tùy chỉnh người dùng (Overrides)
        public double LenAdjustStartMm { get; set; } = 0.0;
        public double LenAdjustEndMm { get; set; } = 0.0;
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Tính tổng chiều dài thực tế của thanh thép
        /// </summary>
        public double CalculateTotalLengthMm()
        {
            if (CenterlinePoints == null || CenterlinePoints.Count < 2) return 0;
            double len = 0;
            for (int i = 0; i < CenterlinePoints.Count - 1; i++)
            {
                len += CenterlinePoints[i].DistanceTo(CenterlinePoints[i + 1]);
            }
            return len + LenAdjustStartMm + LenAdjustEndMm;
        }
    }
}
