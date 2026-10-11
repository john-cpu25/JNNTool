using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Models
{
    public class CategoryFilterItem : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name { get; set; } = string.Empty;
        public string VietnameseName { get; set; } = string.Empty;
        public BuiltInCategory BuiltInCategory { get; set; } = BuiltInCategory.INVALID;
        public bool IsStructural { get; set; }

        private bool _isSelected = true;
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

        private int _count;
        public int Count
        {
            get => _count;
            set
            {
                if (_count != value)
                {
                    _count = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName => string.IsNullOrWhiteSpace(VietnameseName) 
            ? Name 
            : $"{Name} ({VietnameseName})";

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
