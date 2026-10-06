using System;

namespace JNNTool.Tools.IFCEtabs.Core.Geometry
{
    /// <summary>
    /// Vector 3 chiều thuần C# phục vụ tính toán hình học hướng dầm, cột, mặt phẳng.
    /// </summary>
    public struct Vector3D : IEquatable<Vector3D>
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public static Vector3D UnitX => new Vector3D(1, 0, 0);
        public static Vector3D UnitY => new Vector3D(0, 1, 0);
        public static Vector3D UnitZ => new Vector3D(0, 0, 1);
        public static Vector3D Zero => new Vector3D(0, 0, 0);

        public Vector3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);
        public double LengthSquared => X * X + Y * Y + Z * Z;

        public Vector3D Normalize()
        {
            double len = Length;
            if (len < 1e-9) return Zero;
            return new Vector3D(X / len, Y / len, Z / len);
        }

        public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

        public Vector3D Cross(Vector3D other)
        {
            return new Vector3D(
                Y * other.Z - Z * other.Y,
                Z * other.X - X * other.Z,
                X * other.Y - Y * other.X
            );
        }

        public double AngleTo(Vector3D other)
        {
            double dot = Normalize().Dot(other.Normalize());
            dot = Math.Max(-1.0, Math.Min(1.0, dot));
            return Math.Acos(dot); // Radian
        }

        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D v, double scalar) => new Vector3D(v.X * scalar, v.Y * scalar, v.Z * scalar);
        public static Vector3D operator /(Vector3D v, double scalar) => new Vector3D(v.X / scalar, v.Y / scalar, v.Z / scalar);
        public static Vector3D operator -(Vector3D v) => new Vector3D(-v.X, -v.Y, -v.Z);

        public bool Equals(Vector3D other)
        {
            return Math.Abs(X - other.X) < 1e-5 &&
                   Math.Abs(Y - other.Y) < 1e-5 &&
                   Math.Abs(Z - other.Z) < 1e-5;
        }

        public override bool Equals(object? obj) => obj is Vector3D other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + X.GetHashCode();
                hash = hash * 31 + Y.GetHashCode();
                hash = hash * 31 + Z.GetHashCode();
                return hash;
            }
        }

        public override string ToString() => $"Vector({X:F2}, {Y:F2}, {Z:F2})";
    }
}
