using System;
using System.ComponentModel;

namespace JNNTool.Tools.CSIxRevit.Models
{
    /// <summary>
    /// Model đại diện cho một tầng (Story) từ ETABS để cấu hình đổi tên Level hoặc map vào Level Revit sẵn có.
    /// </summary>
    public class LevelMappingItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _targetLevelName = "";
        private string _mappedLevelName = "";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string EtabsStoryName { get; set; } = "";
        public double ElevationFeet { get; set; }
        public double ElevationM => Math.Round(ElevationFeet * 0.3048, 3);
        public double HeightFeet { get; set; }
        public double HeightM => Math.Round(HeightFeet * 0.3048, 3);

        /// <summary>Tên Level mong muốn trong Revit (người dùng có thể sửa đổi tự do).</summary>
        public string TargetLevelName
        {
            get => _targetLevelName;
            set { _targetLevelName = value ?? ""; OnPropertyChanged(nameof(TargetLevelName)); }
        }

        /// <summary>Tên Level Revit sẵn có được chọn để map vào (tùy chọn, để trống nếu tạo Level mới).</summary>
        public string MappedLevelName
        {
            get => _mappedLevelName;
            set { _mappedLevelName = value ?? ""; OnPropertyChanged(nameof(MappedLevelName)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
