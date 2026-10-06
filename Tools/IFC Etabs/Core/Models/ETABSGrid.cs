using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện lưới trục kết cấu từ ETABS hoặc IfcGrid.
    /// </summary>
    public class ETABSGrid : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Grid;

        public string GridSystem { get; set; } = "GLOBAL";
        public Point3D StartPoint { get; set; }
        public Point3D EndPoint { get; set; }
        public string Direction { get; set; } = "X"; // X, Y, hoặc Angle
        public bool IsCurved { get; set; } = false;
        public double RadiusMm { get; set; } = 0.0;

        public LineSegment3D LineSegment => new LineSegment3D(StartPoint, EndPoint);

        public override string ToString() => $"Grid: {Name} [{Direction}] {StartPoint} -> {EndPoint}";
    }
}
