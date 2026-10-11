using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.AutoDimColumn.Models
{
    /// <summary>
    /// Chứa toàn bộ thông tin hình học, cặp mặt đối diện và Grid liên kết của 1 cột.
    /// </summary>
    public class ColumnGeometryInfo
    {
        public ElementId ColumnId { get; set; } = ElementId.InvalidElementId;
        public string Mark { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;

        public XYZ Center { get; set; } = XYZ.Zero;
        public double WidthMm { get; set; }
        public double LengthMm { get; set; }
        public double RotationAngleDeg { get; set; }

        public ColumnFacePair? WidthPair { get; set; }
        public ColumnFacePair? LengthPair { get; set; }

        public List<GridRelationInfo> AssociatedGrids { get; set; } = new List<GridRelationInfo>();

        public bool IsSupported { get; set; } = true;
        public string StatusMessage { get; set; } = "OK";
    }
}
