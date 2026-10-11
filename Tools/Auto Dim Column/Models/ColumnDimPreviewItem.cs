#nullable enable
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JNNTool.Tools.AutoDimColumn.Models
{
    /// <summary>
    /// DTO đại diện cho một hàng cột hiển thị trên Preview DataGrid.
    /// </summary>
    public class ColumnDimPreviewItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private string _idText = string.Empty;
        private string _mark = string.Empty;
        private string _typeName = string.Empty;
        private string _sizeText = string.Empty;
        private string _rotationText = string.Empty;
        private string _gridsText = string.Empty;
        private int _plannedDimsCount;
        private string _status = "Ready";

        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged(); } }
        }

        public string IdText
        {
            get => _idText;
            set { if (_idText != value) { _idText = value; OnPropertyChanged(); } }
        }

        public string Mark
        {
            get => _mark;
            set { if (_mark != value) { _mark = value; OnPropertyChanged(); } }
        }

        public string TypeName
        {
            get => _typeName;
            set { if (_typeName != value) { _typeName = value; OnPropertyChanged(); } }
        }

        public string SizeText
        {
            get => _sizeText;
            set { if (_sizeText != value) { _sizeText = value; OnPropertyChanged(); } }
        }

        public string RotationText
        {
            get => _rotationText;
            set { if (_rotationText != value) { _rotationText = value; OnPropertyChanged(); } }
        }

        public string GridsText
        {
            get => _gridsText;
            set { if (_gridsText != value) { _gridsText = value; OnPropertyChanged(); } }
        }

        public int PlannedDimsCount
        {
            get => _plannedDimsCount;
            set { if (_plannedDimsCount != value) { _plannedDimsCount = value; OnPropertyChanged(); } }
        }

        public string Status
        {
            get => _status;
            set { if (_status != value) { _status = value; OnPropertyChanged(); } }
        }

        public ColumnGeometryInfo? RawInfo { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
