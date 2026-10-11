using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class LevelSummaryItem
    {
        public string LevelName { get; set; } = "Unassigned";
        public double Elevation { get; set; } // m
        public int ElementCount { get; set; }

        public double ConcreteVolume { get; set; } // m3
        public double FormworkArea { get; set; } // m2
        public double SteelRebarWeight { get; set; } // Tons
        public double TotalVolume { get; set; } // m3

        public List<ElementId> ElementIds { get; set; } = new List<ElementId>();
        public List<CategorySummaryItem> CategoryBreakdowns { get; set; } = new List<CategorySummaryItem>();

        public string ElevationDisplay => $"{Elevation:N2} m";
        public string ConcreteVolumeDisplay => ConcreteVolume > 0.0001 ? ConcreteVolume.ToString("N2") : "-";
        public string FormworkAreaDisplay => FormworkArea > 0.0001 ? FormworkArea.ToString("N2") : "-";
        public string SteelRebarWeightDisplay => SteelRebarWeight > 0.0001 ? SteelRebarWeight.ToString("N3") : "-";
        public string TotalVolumeDisplay => TotalVolume > 0.0001 ? TotalVolume.ToString("N2") : "-";
    }
}
