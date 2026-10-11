using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using JNN.Stair.Models;

namespace JNN.Stair.ViewModels
{
    /// <summary>
    /// ViewModel điều khiển cửa sổ Bố Trí Cốt Thép Cầu Thang
    /// </summary>
    public class StairDetailingViewModel : INotifyPropertyChanged
    {
        // 1. Thép dọc lớp dưới
        private double _bottomBarDiameterMm = 10.0;
        public double BottomBarDiameterMm
        {
            get => _bottomBarDiameterMm;
            set { _bottomBarDiameterMm = value; OnPropertyChanged(); }
        }

        private double _bottomBarSpacingMm = 150.0;
        public double BottomBarSpacingMm
        {
            get => _bottomBarSpacingMm;
            set { _bottomBarSpacingMm = value; OnPropertyChanged(); }
        }

        // 2. Thép dọc lớp trên (thép mũ / mô-men âm)
        private double _topBarDiameterMm = 10.0;
        public double TopBarDiameterMm
        {
            get => _topBarDiameterMm;
            set { _topBarDiameterMm = value; OnPropertyChanged(); }
        }

        private double _topBarSpacingMm = 150.0;
        public double TopBarSpacingMm
        {
            get => _topBarSpacingMm;
            set { _topBarSpacingMm = value; OnPropertyChanged(); }
        }

        private double _topBarRatioL = 0.25; // L/4
        public double TopBarRatioL
        {
            get => _topBarRatioL;
            set { _topBarRatioL = value; OnPropertyChanged(); }
        }

        // 3. Thép phân bố ngang
        private double _distBarDiameterMm = 8.0;
        public double DistBarDiameterMm
        {
            get => _distBarDiameterMm;
            set { _distBarDiameterMm = value; OnPropertyChanged(); }
        }

        private double _distBarSpacingMm = 200.0;
        public double DistBarSpacingMm
        {
            get => _distBarSpacingMm;
            set { _distBarSpacingMm = value; OnPropertyChanged(); }
        }

        // 4. Lớp bảo vệ & Neo
        private double _concreteCoverMm = 15.0;
        public double ConcreteCoverMm
        {
            get => _concreteCoverMm;
            set { _concreteCoverMm = value; OnPropertyChanged(); }
        }

        private double _anchorMultiplier = 35.0; // 35d
        public double AnchorMultiplier
        {
            get => _anchorMultiplier;
            set { _anchorMultiplier = value; OnPropertyChanged(); }
        }

        private bool _enableConcaveDiagonalCrossing = true;
        public bool EnableConcaveDiagonalCrossing
        {
            get => _enableConcaveDiagonalCrossing;
            set { _enableConcaveDiagonalCrossing = value; OnPropertyChanged(); }
        }

        // 5. Danh sách thanh thép & Section hiện tại
        public ObservableCollection<StairRebarData> GeneratedRebars { get; } = new();

        private StairRebarData? _selectedRebar;
        public StairRebarData? SelectedRebar
        {
            get => _selectedRebar;
            set { _selectedRebar = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
