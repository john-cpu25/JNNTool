using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CreateView.Models
{
    public class LevelItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool _isSelected = true;

        public ElementId Id { get; set; } = ElementId.InvalidElementId;
        public string Name { get; set; } = string.Empty;
        public double Elevation { get; set; }

        public string ElevationDisplay
        {
            get
            {
                double meters = UnitUtils.ConvertFromInternalUnits(Elevation, UnitTypeId.Meters);
                return meters >= 0 ? $"+{meters:F3} m" : $"{meters:F3} m";
            }
        }

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

        public override string ToString() => $"{Name} ({ElevationDisplay})";
    }
}
