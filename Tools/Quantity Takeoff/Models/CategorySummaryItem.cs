using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class CategorySummaryItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public BuiltInCategory BuiltInCategory { get; set; } = BuiltInCategory.INVALID;
        public int Count { get; set; }
        public double TotalVolume { get; set; } // m3
        public double TotalArea { get; set; } // m2
        public double TotalLength { get; set; } // m
        public double TotalWeight { get; set; } // Tons

        public List<ElementId> ElementIds { get; set; } = new List<ElementId>();

        public string TotalVolumeDisplay => TotalVolume > 0.0001 ? TotalVolume.ToString("N2") : "-";
        public string TotalAreaDisplay => TotalArea > 0.0001 ? TotalArea.ToString("N2") : "-";
        public string TotalWeightDisplay => TotalWeight > 0.0001 ? TotalWeight.ToString("N3") : "-";
    }
}
