using JNNTool.Tools.IFCEtabs.Core.Enums;

namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Định nghĩa tiết diện từ ETABS hoặc IFC.
    /// </summary>
    public class ETABSSection
    {
        public string Name { get; set; } = string.Empty;
        public SectionShapeType Shape { get; set; } = SectionShapeType.Rectangular;
        public string MaterialName { get; set; } = string.Empty;

        // Kích thước hình học (đơn vị: mm)
        public double WidthMm { get; set; }        // b
        public double DepthMm { get; set; }        // h (t2 / depth)
        public double ThicknessMm { get; set; }    // cho bản sàn / vách
        public double FlangeWidthMm { get; set; }  // cánh chữ I/T
        public double FlangeThicknessMm { get; set; }
        public double WebThicknessMm { get; set; }
        public double DiameterMm { get; set; }     // cho tiết diện tròn

        public bool IsCircular => Shape == SectionShapeType.Circular || (DiameterMm > 0 && WidthMm <= 0);

        public override string ToString()
        {
            if (IsCircular)
                return $"{Name} (Ø{DiameterMm:F0} mm, {MaterialName})";
            return $"{Name} ({WidthMm:F0}×{DepthMm:F0} mm, {MaterialName})";
        }
    }
}
