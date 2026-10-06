using System;

namespace JNNTool.Tools.IFCEtabs.Core.Geometry
{
    /// <summary>
    /// Bộ chuyển đổi hệ tọa độ giữa ETABS / IFC và Revit.
    /// Hỗ trợ dời gốc (Offset), xoay góc quanh trục Z (Project North / True North) và tỷ lệ (Scale).
    /// </summary>
    public class CoordinateTransform
    {
        public Point3D OriginOffset { get; set; } = Point3D.Origin;
        public double RotationDegrees { get; set; } = 0.0;
        public double Scale { get; set; } = 1.0;

        public static CoordinateTransform Identity => new CoordinateTransform();

        public CoordinateTransform() { }

        public CoordinateTransform(Point3D originOffset, double rotationDegrees = 0.0, double scale = 1.0)
        {
            OriginOffset = originOffset;
            RotationDegrees = rotationDegrees;
            Scale = scale;
        }

        public Point3D TransformPoint(Point3D pt)
        {
            // 1. Áp dụng tỷ lệ
            double x = pt.X * Scale;
            double y = pt.Y * Scale;
            double z = pt.Z * Scale;

            // 2. Áp dụng xoay quanh trục Z
            if (Math.Abs(RotationDegrees) > 1e-6)
            {
                double rad = RotationDegrees * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rotX = x * cos - y * sin;
                double rotY = x * sin + y * cos;
                x = rotX;
                y = rotY;
            }

            // 3. Áp dụng dời gốc tọa độ
            return new Point3D(x + OriginOffset.X, y + OriginOffset.Y, z + OriginOffset.Z);
        }

        public Vector3D TransformVector(Vector3D v)
        {
            double x = v.X * Scale;
            double y = v.Y * Scale;
            double z = v.Z * Scale;

            if (Math.Abs(RotationDegrees) > 1e-6)
            {
                double rad = RotationDegrees * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rotX = x * cos - y * sin;
                double rotY = x * sin + y * cos;
                x = rotX;
                y = rotY;
            }

            return new Vector3D(x, y, z);
        }

        public Point3D InverseTransformPoint(Point3D pt)
        {
            // 1. Trừ gốc tọa độ
            double x = pt.X - OriginOffset.X;
            double y = pt.Y - OriginOffset.Y;
            double z = pt.Z - OriginOffset.Z;

            // 2. Xoay ngược lại (-RotationDegrees)
            if (Math.Abs(RotationDegrees) > 1e-6)
            {
                double rad = -RotationDegrees * Math.PI / 180.0;
                double cos = Math.Cos(rad);
                double sin = Math.Sin(rad);
                double rotX = x * cos - y * sin;
                double rotY = x * sin + y * cos;
                x = rotX;
                y = rotY;
            }

            // 3. Chia tỷ lệ
            if (Math.Abs(Scale) > 1e-9)
            {
                x /= Scale;
                y /= Scale;
                z /= Scale;
            }

            return new Point3D(x, y, z);
        }

        public double TransformRotation(double rotationAngleRad)
        {
            return rotationAngleRad + (RotationDegrees * Math.PI / 180.0);
        }
    }
}
