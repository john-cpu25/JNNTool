using System;
using System.Collections.Generic;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Lớp cơ sở đại diện cho mọi đối tượng kết cấu trích xuất từ ETABS hoặc IFC.
    /// Hoàn toàn độc lập với Revit API để đảm bảo kiến trúc sạch (clean-room) và test được.
    /// </summary>
    public abstract class StructuralElement
    {
        public string Source { get; set; } = "ETABS";
        public string SourceId { get; set; } = string.Empty;
        public string Guid { get; set; } = System.Guid.NewGuid().ToString("D");
        public abstract StructuralElementType ElementType { get; }
        public string Name { get; set; } = string.Empty;
        public string Story { get; set; } = string.Empty;
        public string Level { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;

        public double Rotation { get; set; } = 0.0;
        public Vector3D LocalAxis { get; set; } = Vector3D.UnitX;
        public string Releases { get; set; } = string.Empty;
        public CardinalPoint CardinalPoint { get; set; } = CardinalPoint.Centroid;

        public string AnalyticalType { get; set; } = string.Empty;
        public string DesignType { get; set; } = string.Empty;

        public string Hash { get; set; } = string.Empty;
        public ConversionStatus Status { get; set; } = ConversionStatus.New;

        public Dictionary<string, object> UserProperties { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, object> IFCProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

        public override string ToString() => $"[{ElementType}] ID={SourceId}, Name={Name}, Story={Story}, Sec={Section}";
    }
}
