using System;

namespace JNNTool.Tools.IFCEtabs.Core.Geometry
{
    /// <summary>
    /// Đoạn thẳng 3 chiều (đơn vị: mm).
    /// </summary>
    public struct LineSegment3D
    {
        public Point3D StartPoint { get; }
        public Point3D EndPoint { get; }

        public LineSegment3D(Point3D start, Point3D end)
        {
            StartPoint = start;
            EndPoint = end;
        }

        public double Length => StartPoint.DistanceTo(EndPoint);

        public Vector3D Direction => (EndPoint - StartPoint).Normalize();

        public Point3D MidPoint => new Point3D(
            (StartPoint.X + EndPoint.X) * 0.5,
            (StartPoint.Y + EndPoint.Y) * 0.5,
            (StartPoint.Z + EndPoint.Z) * 0.5
        );

        public bool IsVertical(double toleranceDegrees = 1.0)
        {
            Vector3D dir = Direction;
            double verticalDot = Math.Abs(dir.Dot(Vector3D.UnitZ));
            double cosThreshold = Math.Cos(toleranceDegrees * Math.PI / 180.0);
            return verticalDot >= cosThreshold;
        }

        public override string ToString() => $"{StartPoint} -> {EndPoint} (L={Length:F0}mm)";
    }
}
