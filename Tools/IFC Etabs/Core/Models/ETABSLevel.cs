using JNNTool.Tools.IFCEtabs.Core.Enums;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Đại diện tầng / Story từ ETABS hoặc IfcBuildingStorey.
    /// </summary>
    public class ETABSLevel : StructuralElement
    {
        public override StructuralElementType ElementType => StructuralElementType.Level;

        /// <summary>
        /// Cao độ tầng (đơn vị: mm).
        /// </summary>
        public double ElevationMm { get; set; }

        /// <summary>
        /// Chiều cao tầng (đơn vị: mm).
        /// </summary>
        public double HeightMm { get; set; }

        /// <summary>
        /// Tầng chuẩn / Master Story trong ETABS.
        /// </summary>
        public bool IsMasterStory { get; set; }

        /// <summary>
        /// Tên tầng chuẩn tương đồng nếu là Similar Story.
        /// </summary>
        public string SimilarToStory { get; set; } = string.Empty;

        /// <summary>
        /// Có gắn diaphragm tự động ở tầng này không.
        /// </summary>
        public bool HasDiaphragm { get; set; }

        public override string ToString() => $"Level: {Name} (Elev: {ElevationMm:F0}mm, H: {HeightMm:F0}mm)";
    }
}
