using System.Collections.Generic;
using JNNTool.Tools.IFCEtabs.Core.Enums;
using JNNTool.Tools.IFCEtabs.Core.Geometry;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện sàn kết cấu từ ETABS Area hoặc IfcSlab.
    /// </summary>
    public class ETABSSlab : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Slab;

        /// <summary>
        /// Đường bao ngoài của sàn (Point3D khép kín).
        /// </summary>
        public List<Point3D> OuterBoundary { get; } = new();

        /// <summary>
        /// Danh sách các lỗ mở trong sàn (nếu có).
        /// </summary>
        public List<List<Point3D>> Openings { get; } = new();

        public double ThicknessMm { get; set; }
        public string DiaphragmName { get; set; } = string.Empty;
        public bool IsOpening { get; set; } = false;

        public override string ToString() => $"Slab: {Name} (Thk: {ThicknessMm:F0}mm, Story: {Story}, Pts: {OuterBoundary.Count})";
    }
}
