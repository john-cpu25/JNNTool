using Autodesk.Revit.DB;

namespace JNNTool.Tools.AutoDimColumn.Models
{
    /// <summary>
    /// Lưu thông tin quan hệ hình học giữa một Grid và cột.
    /// </summary>
    public class GridRelationInfo
    {
        public ElementId GridId { get; set; } = ElementId.InvalidElementId;
        public string GridName { get; set; } = string.Empty;
        public Line? GridLine { get; set; }
        public XYZ GridDir { get; set; } = XYZ.BasisX;
        public XYZ GridNormal { get; set; } = XYZ.BasisY;

        /// <summary>Khoảng cách vuông góc từ tâm cột đến Grid (feet).</summary>
        public double PerpendicularDistanceFt { get; set; }
        public double PerpendicularDistanceMm => PerpendicularDistanceFt * 304.8;

        /// <summary>Độ lệch góc so với mặt tương ứng của cột (độ).</summary>
        public double AngleDiffDeg { get; set; }

        /// <summary>0 = Song song với phương U (bề rộng), 1 = Song song với phương V (chiều dài).</summary>
        public int TargetDirection { get; set; }

        public Reference? GridReference { get; set; }
    }
}
