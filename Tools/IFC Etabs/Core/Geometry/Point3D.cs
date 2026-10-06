using System;

namespace JNNTool.Tools.IFCEtabs.Core.Geometry
{
    /// <summary>
    /// Điểm 3 chiều (đơn vị: mm), độc lập với Revit API để phục vụ tính toán và Unit Test.
    /// </summary>
    public struct Point3D : IEquatable<Point3D>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Point3D Origin => new Point3D(0, 0, 0);

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public double DistanceToXY(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public bool IsAlmostEqualTo(Point3D other, double toleranceMm = 1.0)
        {
            return Math.Abs(X - other.X) <= toleranceMm &&
                   Math.Abs(Y - other.Y) <= toleranceMm &&
                   Math.Abs(Z - other.Z) <= toleranceMm;
        }

        public static Point3D operator +(Point3D p, Vector3D v) => new Point3D(p.X + v.X, p.Y + v.Y, p.Z + v.Z);
        public static Point3D operator -(Point3D p, Vector3D v) => new Point3D(p.X - v.X, p.Y - v.Y, p.Z - v.Z);
        public static Vector3D operator -(Point3D p1, Point3D p2) => new Vector3D(p1.X - p2.X, p1.Y - p2.Y, p1.Z - p2.Z);

        public static bool operator ==(Point3D left, Point3D right) => left.IsAlmostEqualTo(right, 1e-4);
        public static bool operator !=(Point3D left, Point3D right) => !(left == right);

        public bool Equals(Point3D other) => IsAlmostEqualTo(other, 1e-4);

        public override bool Equals(object? obj) => obj is Point3D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + Math.Round(X, 1).GetHashCode();
                hash = hash * 31 + Math.Round(Y, 1).GetHashCode();
                hash = hash * 31 + Math.Round(Z, 1).GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"({X:F1}, {Y:F1}, {Z:F1})";
    }
}
