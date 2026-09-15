using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Autodesk.Revit.DB.Structure;

namespace JNNTool
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class RebarBeamViewModel : ObservableObject
    {
        public ObservableCollection<RebarBarType> AvailableRebarTypes { get; set; }

        private RebarBarType _selectedTopRebarType;
        public RebarBarType SelectedTopRebarType
        {
            get => _selectedTopRebarType;
            set => SetProperty(ref _selectedTopRebarType, value);
        }

        private int _topRebarQuantity = 2;
        public int TopRebarQuantity
        {
            get => _topRebarQuantity;
            set => SetProperty(ref _topRebarQuantity, value);
        }

        private RebarBarType _selectedBottomRebarType;
        public RebarBarType SelectedBottomRebarType
        {
            get => _selectedBottomRebarType;
            set => SetProperty(ref _selectedBottomRebarType, value);
        }

        private int _bottomRebarQuantity = 2;
        public int BottomRebarQuantity
        {
            get => _bottomRebarQuantity;
            set => SetProperty(ref _bottomRebarQuantity, value);
        }

        // Additional Rebars
        private bool _enableTopAdd = false;
        public bool EnableTopAdd
        {
            get => _enableTopAdd;
            set => SetProperty(ref _enableTopAdd, value);
        }

        private RebarBarType _selectedTopAddRebarType;
        public RebarBarType SelectedTopAddRebarType
        {
            get => _selectedTopAddRebarType;
            set => SetProperty(ref _selectedTopAddRebarType, value);
        }

        private int _topAddQuantity = 2;
        public int TopAddQuantity
        {
            get => _topAddQuantity;
            set => SetProperty(ref _topAddQuantity, value);
        }

        private bool _enableBottomAdd = false;
        public bool EnableBottomAdd
        {
            get => _enableBottomAdd;
            set => SetProperty(ref _enableBottomAdd, value);
        }

        private RebarBarType _selectedBottomAddRebarType;
        public RebarBarType SelectedBottomAddRebarType
        {
            get => _selectedBottomAddRebarType;
            set => SetProperty(ref _selectedBottomAddRebarType, value);
        }

        private int _bottomAddQuantity = 2;
        public int BottomAddQuantity
        {
            get => _bottomAddQuantity;
            set => SetProperty(ref _bottomAddQuantity, value);
        }

        private RebarBarType _selectedStirrupRebarType;
        public RebarBarType SelectedStirrupRebarType
        {
            get => _selectedStirrupRebarType;
            set => SetProperty(ref _selectedStirrupRebarType, value);
        }

        private double _stirrupSupportSpacing = 100;
        public double StirrupSupportSpacing
        {
            get => _stirrupSupportSpacing;
            set => SetProperty(ref _stirrupSupportSpacing, value);
        }

        private double _stirrupMidSpacing = 200;
        public double StirrupMidSpacing
        {
            get => _stirrupMidSpacing;
            set => SetProperty(ref _stirrupMidSpacing, value);
        }

        public ObservableCollection<RebarHookType> AvailableHookTypes { get; set; }

        private RebarHookType _selectedTopHookType;
        public RebarHookType SelectedTopHookType
        {
            get => _selectedTopHookType;
            set => SetProperty(ref _selectedTopHookType, value);
        }

        private RebarHookType _selectedBottomHookType;
        public RebarHookType SelectedBottomHookType
        {
            get => _selectedBottomHookType;
            set => SetProperty(ref _selectedBottomHookType, value);
        }

        private RebarHookType _selectedStirrupHookType;
        public RebarHookType SelectedStirrupHookType
        {
            get => _selectedStirrupHookType;
            set => SetProperty(ref _selectedStirrupHookType, value);
        }

        private double _topAnchorageD = 30;
        public double TopAnchorageD
        {
            get => _topAnchorageD;
            set => SetProperty(ref _topAnchorageD, value);
        }

        private double _bottomAnchorageD = 15;
        public double BottomAnchorageD
        {
            get => _bottomAnchorageD;
            set => SetProperty(ref _bottomAnchorageD, value);
        }
    }
}
