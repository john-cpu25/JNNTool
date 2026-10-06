using System.Collections.Generic;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện vách kết cấu từ ETABS Area hoặc IfcWall.
    /// </summary>
    public class ETABSWall : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Wall;

        public List<Point3D> BoundaryPoints { get; } = new();

        public double ThicknessMm { get; set; }
        public double HeightMm { get; set; }
        public double LengthMm { get; set; }

        public Point3D BasePointStart { get; set; }
        public Point3D BasePointEnd { get; set; }

        public string PierName { get; set; } = string.Empty;
        public string SpandrelName { get; set; } = string.Empty;

        public override string ToString() => $"Wall: {Name} (Pier: {PierName}, Thk: {ThicknessMm:F0}mm, Story: {Story})";
    }
}
