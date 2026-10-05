using System;

namespace JNNTool.Tools.CSIxRevit.Models
{
    public class SectionDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Material { get; set; } = string.Empty;
        public string Shape { get; set; } = string.Empty; // "Concrete Rectangular", "Concrete Circle", etc.
        public double DepthMm { get; set; }
        public double WidthMm { get; set; }
        public string ElementType { get; set; } = string.Empty; // "Dầm (Beam)", "Cột (Column)", "Vách (Wall)", "Sàn (Floor)"

        public bool IsCircular => !string.IsNullOrEmpty(Shape) && Shape.IndexOf("Circle", StringComparison.OrdinalIgnoreCase) >= 0;

        public string DimensionSummary
        {
            get
            {
                if (IsCircular)
                    return $"Ø{Math.Round(DepthMm, 0)} mm";
                if (DepthMm > 0 && WidthMm > 0)
                    return $"{Math.Round(WidthMm, 0)}×{Math.Round(DepthMm, 0)} mm";
                if (DepthMm > 0)
                {
                    if (DepthMm <= 1.0) return "Dày 1 mm (Sàn ảo)";
                    return $"Dày {Math.Round(DepthMm, 0)} mm";
                }
                return string.Empty;
            }
        }
    }
}
