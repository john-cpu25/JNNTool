using System;
using System.Collections.Generic;

namespace JNNTool.RebarSuite.Slab.Models
{
    /// <summary>
    /// Điểm 2D dạng milimet (mm) độc lập Revit API.
    /// </summary>
    public struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:F1}, {Y:F1})";
    }

    /// <summary>
    /// Điểm 3D dạng milimet (mm).
    /// </summary>
    public struct Point3D
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Point3D operator +(Point3D a, Point3D b) => new Point3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Point3D operator -(Point3D a, Point3D b) => new Point3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Point3D operator *(Point3D a, double s) => new Point3D(a.X * s, a.Y * s, a.Z * s);
        public static Point3D operator *(double s, Point3D a) => new Point3D(a.X * s, a.Y * s, a.Z * s);
        public static Point3D operator /(Point3D a, double s) => new Point3D(a.X / s, a.Y / s, a.Z / s);

        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }

    /// <summary>
    /// Thông tin dầm đỡ sàn được nhận diện từ mô hình.
    /// </summary>
    public sealed class BeamSupportInfo
    {
        public long ElementId { get; set; }
        public Point2D StartPoint { get; set; }
        public Point2D EndPoint { get; set; }
        public double WidthMm { get; set; } = 300.0;
        public double DepthMm { get; set; } = 500.0;
        public bool IsEdgeBeam { get; set; } = false;
    }

    /// <summary>
    /// Domain Model đại diện cho một sàn kết cấu (POCO, đơn vị mm).
    /// </summary>
    public sealed class SlabModel
    {
        public long ElementId { get; set; }
        public string HostMark { get; set; } = "";
        public string LevelName { get; set; } = "";
        public double ThicknessMm { get; set; } = 120.0;
        public double TopElevationMm { get; set; } = 0.0;

        /// <summary>
        /// Đường bao ngoài của sàn (đa giác khép kín).
        /// </summary>
        public List<Point2D> OuterBoundary { get; set; } = new List<Point2D>();

        /// <summary>
        /// Danh sách các lỗ mở trong sàn.
        /// </summary>
        public List<List<Point2D>> Openings { get; set; } = new List<List<Point2D>>();

        /// <summary>
        /// Các dầm đỡ sàn tìm thấy.
        /// </summary>
        public List<BeamSupportInfo> SupportingBeams { get; set; } = new List<BeamSupportInfo>();

        /// <summary>
        /// Tính Bounding Box (MinX, MinY, MaxX, MaxY) của sàn.
        /// </summary>
        public (double MinX, double MinY, double MaxX, double MaxY) GetBounds()
        {
            if (OuterBoundary == null || OuterBoundary.Count == 0)
                return (0, 0, 0, 0);

            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var pt in OuterBoundary)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            return (minX, minY, maxX, maxY);
        }
    }
}
