using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện cột kết cấu từ ETABS Frame hoặc IfcColumn.
    /// </summary>
    public class ETABSColumn : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Column;

        /// <summary>
        /// Điểm chân cột (Z thấp hơn).
        /// </summary>
        public Point3D BasePoint { get; set; }

        /// <summary>
        /// Điểm đỉnh cột (Z cao hơn).
        /// </summary>
        public Point3D TopPoint { get; set; }

        public double HeightMm => BasePoint.DistanceTo(TopPoint);

        public LineSegment3D LineSegment => new LineSegment3D(BasePoint, TopPoint);

        public string BaseStory { get; set; } = string.Empty;
        public string TopStory { get; set; } = string.Empty;

        // Kích thước hình học tham chiếu (mm)
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public double DiameterMm { get; set; }
        public bool IsCircular { get; set; }

        // Cột nghiêng (Slanted Column)
        public bool IsSlanted => !LineSegment.IsVertical(1.0);

        // Góc xoay tiết diện cột quanh trục Z (độ)
        public double AngleDegrees { get; set; } = 0.0;

        // Tên vách/pier liên kết (nếu có)
        public string PierName { get; set; } = string.Empty;

        public override string ToString() => $"Column: {Name} (Sec: {Section}, H: {HeightMm:F0}mm, Stories: {BaseStory}->{TopStory})";
    }
}
