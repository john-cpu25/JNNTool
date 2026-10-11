using Autodesk.Revit.DB;

namespace JNNTool.Tools.AutoDimColumn.Models
{
    /// <summary>
    /// Đại diện cho một cặp mặt phẳng song song đối diện của cột.
    /// </summary>
    public class ColumnFacePair
    {
        public PlanarFace? Face1 { get; set; }
        public PlanarFace? Face2 { get; set; }

        public Reference? Ref1 { get; set; }
        public Reference? Ref2 { get; set; }

        /// <summary>Vector đơn vị pháp tuyến (chỉ hướng từ Face1 sang Face2 hoặc hướng chính).</summary>
        public XYZ Normal { get; set; } = XYZ.BasisX;

        /// <summary>Khoảng cách giữa hai mặt phẳng (feet).</summary>
        public double DistanceFt { get; set; }

        /// <summary>Khoảng cách giữa hai mặt phẳng (mm).</summary>
        public double DistanceMm => DistanceFt * 304.8;

        public bool IsValid => Ref1 != null && Ref2 != null && DistanceFt > 1e-4;
    }
}
