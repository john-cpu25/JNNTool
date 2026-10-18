using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;
using JNNTool.Tools.QuantityTakeoff.Helpers;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class TakeoffElementItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ElementId ElementId { get; set; }
        public long IdValue => ElementId != null ? ElementId.GetIdValue() : 0;

        public string CategoryName { get; set; } = string.Empty;
        public BuiltInCategory BuiltInCategory { get; set; } = BuiltInCategory.INVALID;

        public string LevelName { get; set; } = "Unassigned";
        public double LevelElevation { get; set; } // in meters

        public string FamilyName { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string Mark { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;

        public double Volume { get; set; } // m3
        public double Area { get; set; } // m2
        public double Length { get; set; } // m
        public double Weight { get; set; } // Tons (1 Ton = 1000 kg)
        public double WeightKg => Weight * 1000.0;
        public int Count { get; set; } = 1;

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

        public string VolumeDisplay => Volume > 0.0001 ? Volume.ToString("N2") : "-";
        public string AreaDisplay => Area > 0.0001 ? Area.ToString("N2") : "-";
        public string LengthDisplay => Length > 0.0001 ? Length.ToString("N2") : "-";
        public string WeightDisplay => Weight > 0.0001 ? Weight.ToString("N3") : "-";

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
