namespace JNNTool.Tools.CSIxRevit.Models
{
    public class Point3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Point3D() { }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(Point3D other)
        {
            if (other == null) return 0;
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }

    public class PointData
    {
        public string Name { get; set; } = string.Empty;
        public Point3D Position { get; set; } = new Point3D();
    }
}
