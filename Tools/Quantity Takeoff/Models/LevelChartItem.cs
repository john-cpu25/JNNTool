using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class LevelChartItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string LevelName { get; set; } = string.Empty;
        public double Elevation { get; set; }
        public double Volume { get; set; } // m3
        public double Formwork { get; set; } // m2
        public double Rebar { get; set; } // Tons
        public double Percentage { get; set; } // 0 - 100%
        public double BarWidthRatio { get; set; } // 0.0 - 1.0 (relative to max)

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<ElementId> ElementIds { get; set; } = new List<ElementId>();

        public string ElevationDisplay => $"{Elevation:N2} m";
        public string VolumeDisplay => $"{Volume:N2} m³";
        public string PercentageDisplay => $"{Percentage:N1}%";
        public double RelativePercentage => Math.Max(5.0, Math.Min(100.0, BarWidthRatio * 100.0));
        public string TooltipText => $"{LevelName} ({Elevation:N2} m)\n• Bê tông: {Volume:N2} m³ ({Percentage:N1}%)\n• Cốp pha: {Formwork:N2} m²\n• Thép: {Rebar:N3} Tấn";

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
