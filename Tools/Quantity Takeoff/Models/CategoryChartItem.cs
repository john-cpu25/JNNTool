using System.Collections.Generic;
using System.Windows.Media;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class CategoryChartItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public double Volume { get; set; } // m3
        public double Percentage { get; set; } // 0 - 100%
        public int ElementCount { get; set; }
        public string ColorHex { get; set; } = "#0284C7";
        public SolidColorBrush ColorBrush => (SolidColorBrush)new BrushConverter().ConvertFromString(ColorHex);

        public List<ElementId> ElementIds { get; set; } = new List<ElementId>();

        public string VolumeDisplay => $"{Volume:N2} m³ ({Percentage:N1}%)";
        public string TooltipText => $"{CategoryName}\n• Thể tích: {Volume:N2} m³\n• Chiếm: {Percentage:N1}%\n• Số lượng: {ElementCount} cấu kiện";
    }
}
