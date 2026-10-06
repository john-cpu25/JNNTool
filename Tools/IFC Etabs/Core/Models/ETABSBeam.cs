using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện dầm kết cấu từ ETABS Frame hoặc IfcBeam.
    /// </summary>
    public class ETABSBeam : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Beam;

        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }

        public double LengthMm => StartPoint.DistanceTo(EndPoint);

        public LineSegment3D LineSegment => new LineSegment3D(StartPoint, EndPoint);

        // Kích thước tham chiếu nhanh (mm)
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }

        // Bù lệch cao độ đầu/cuối (Insertion / End Offsets)
        public double StartOffsetZ { get; set; } = 0.0;
        public double EndOffsetZ { get; set; } = 0.0;

        // Tên dải spandrel (nếu có gắn trong ETABS)
        public string SpandrelName { get; set; } = string.Empty;

        public override string ToString() => $"Beam: {Name} (Sec: {Section}, L: {LengthMm:F0}mm, Story: {Story})";
    }
}
