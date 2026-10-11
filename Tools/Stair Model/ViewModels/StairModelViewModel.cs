#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.Compat;
using JNNTool.Core.ExternalEvents;
using JNNTool.Tools.StairModel.Engine;
using JNNTool.Tools.StairModel.Services;

namespace JNNTool.Tools.StairModel.ViewModels
{
    public class NamedItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public override string ToString() => Name;
    }

    public class LevelComboItem : NamedItem
    {
        public double ElevationMm { get; set; }
    }

    public class StairModelViewModel : ObservableObject
    {
        private StairType _stairType = StairType.UShaped;
        private TurnDirection _turnDirection = TurnDirection.Left;
        private SupportBeamOption _supportBeamOption = SupportBeamOption.LandingBeamOnly;

        private LevelComboItem? _selectedBaseLevel;
        private LevelComboItem? _selectedTopLevel;
        private double _baseOffsetMm = 0.0;
        private double _topOffsetMm = 0.0;

        private int _totalRisers = 21;
        private int _flight1Risers = 11;
        private int _flight2Risers = 10;
        private double _treadDepthMm = 280.0;
        private double _stairWidthMm = 1100.0;
        private double _landingLengthMm = 1200.0;
        private double _gapBetweenFlightsMm = 100.0;

        private double _waistSlabThicknessMm = 120.0;
        private double _landingSlabThicknessMm = 120.0;

        private NamedItem? _selectedFloorType;
        private NamedItem? _selectedBeamSymbol;

        private double _insertionPointX = 0.0;
        private double _insertionPointY = 0.0;
        private double _rotationAngleDeg = 0.0;

        private double _riserHeightMm;
        private double _blondelNumberMm;
        private double _inclineAngleDeg;
        private string _blondelStatus = "Chuẩn TCVN";
        private string _statusMessage = "Sẵn sàng tạo cầu thang kết cấu";

        private StairGeometryResult _geometryResult = new StairGeometryResult();

        public event Action? RequestPreviewUpdate;
        public Action? RequestClose;

        public StairModelViewModel()
        {
            CreateStairCommand = new RelayCommand(OnCreateStair);
            PickPointCommand = new RelayCommand(OnPickPoint);
            ResetCommand = new RelayCommand(OnReset);

            Recalculate();
        }

        #region Collections

        public ObservableCollection<LevelComboItem> Levels { get; } = new ObservableCollection<LevelComboItem>();
        public ObservableCollection<NamedItem> FloorTypes { get; } = new ObservableCollection<NamedItem>();
        public ObservableCollection<NamedItem> BeamSymbols { get; } = new ObservableCollection<NamedItem>();

        #endregion

        #region Properties

        public StairType StairType
        {
            get => _stairType;
            set
            {
                if (SetProperty(ref _stairType, value))
                {
                    OnPropertyChanged(nameof(SelectedStairTypeIndex));
                    OnPropertyChanged(nameof(IsTwoFlight));
                    OnPropertyChanged(nameof(IsUShaped));
                    OnPropertyChanged(nameof(IsLShaped));
                    Recalculate();
                }
            }
        }

        public int SelectedStairTypeIndex
        {
            get => (int)StairType;
            set
            {
                if (value >= 0 && value <= 2 && (int)StairType != value)
                {
                    StairType = (StairType)value;
                }
            }
        }

        public bool IsTwoFlight => StairType == StairType.UShaped || StairType == StairType.LShaped;
        public bool IsUShaped => StairType == StairType.UShaped;
        public bool IsLShaped => StairType == StairType.LShaped;

        public TurnDirection TurnDirection
        {
            get => _turnDirection;
            set
            {
                if (SetProperty(ref _turnDirection, value))
                {
                    OnPropertyChanged(nameof(IsTurnLeft));
                    OnPropertyChanged(nameof(IsTurnRight));
                    Recalculate();
                }
            }
        }

        public bool IsTurnLeft
        {
            get => TurnDirection == TurnDirection.Left;
            set
            {
                if (value)
                {
                    TurnDirection = TurnDirection.Left;
                }
            }
        }

        public bool IsTurnRight
        {
            get => TurnDirection == TurnDirection.Right;
            set
            {
                if (value)
                {
                    TurnDirection = TurnDirection.Right;
                }
            }
        }

        public SupportBeamOption SupportBeamOption
        {
            get => _supportBeamOption;
            set
            {
                if (SetProperty(ref _supportBeamOption, value))
                {
                    OnPropertyChanged(nameof(SelectedSupportBeamOptionIndex));
                    OnPropertyChanged(nameof(HasBeams));
                    Recalculate();
                }
            }
        }

        public int SelectedSupportBeamOptionIndex
        {
            get => (int)SupportBeamOption;
            set
            {
                if (value >= 0 && value <= 2 && (int)SupportBeamOption != value)
                {
                    SupportBeamOption = (SupportBeamOption)value;
                }
            }
        }

        public bool HasBeams => SupportBeamOption != SupportBeamOption.None;

        public LevelComboItem? SelectedBaseLevel
        {
            get => _selectedBaseLevel;
            set
            {
                if (SetProperty(ref _selectedBaseLevel, value))
                    Recalculate();
            }
        }

        public LevelComboItem? SelectedTopLevel
        {
            get => _selectedTopLevel;
            set
            {
                if (SetProperty(ref _selectedTopLevel, value))
                    Recalculate();
            }
        }

        public double BaseOffsetMm
        {
            get => _baseOffsetMm;
            set
            {
                if (SetProperty(ref _baseOffsetMm, value))
                    Recalculate();
            }
        }

        public double TopOffsetMm
        {
            get => _topOffsetMm;
            set
            {
                if (SetProperty(ref _topOffsetMm, value))
                    Recalculate();
            }
        }

        public double TotalRiseMm
        {
            get
            {
                double baseElev = (SelectedBaseLevel?.ElevationMm ?? 0.0) + BaseOffsetMm;
                double topElev = (SelectedTopLevel?.ElevationMm ?? 3600.0) + TopOffsetMm;
                return Math.Max(topElev - baseElev, 0.0);
            }
        }

        public int TotalRisers
        {
            get => _totalRisers;
            set
            {
                if (value >= 2 && SetProperty(ref _totalRisers, value))
                {
                    // Tự động phân bổ lại 2 vế
                    _flight1Risers = (value + 1) / 2;
                    _flight2Risers = value - _flight1Risers;
                    OnPropertyChanged(nameof(Flight1Risers));
                    OnPropertyChanged(nameof(Flight2Risers));
                    Recalculate();
                }
            }
        }

        public int Flight1Risers
        {
            get => _flight1Risers;
            set
            {
                if (value >= 1 && value < TotalRisers && SetProperty(ref _flight1Risers, value))
                {
                    _flight2Risers = TotalRisers - value;
                    OnPropertyChanged(nameof(Flight2Risers));
                    Recalculate();
                }
            }
        }

        public int Flight2Risers
        {
            get => _flight2Risers;
            set
            {
                if (value >= 1 && value < TotalRisers && SetProperty(ref _flight2Risers, value))
                {
                    _flight1Risers = TotalRisers - value;
                    OnPropertyChanged(nameof(Flight1Risers));
                    Recalculate();
                }
            }
        }

        public double TreadDepthMm
        {
            get => _treadDepthMm;
            set
            {
                if (SetProperty(ref _treadDepthMm, value))
                    Recalculate();
            }
        }

        public double StairWidthMm
        {
            get => _stairWidthMm;
            set
            {
                if (SetProperty(ref _stairWidthMm, value))
                    Recalculate();
            }
        }

        public double LandingLengthMm
        {
            get => _landingLengthMm;
            set
            {
                if (SetProperty(ref _landingLengthMm, value))
                    Recalculate();
            }
        }

        public double GapBetweenFlightsMm
        {
            get => _gapBetweenFlightsMm;
            set
            {
                if (SetProperty(ref _gapBetweenFlightsMm, value))
                    Recalculate();
            }
        }

        public double WaistSlabThicknessMm
        {
            get => _waistSlabThicknessMm;
            set
            {
                if (SetProperty(ref _waistSlabThicknessMm, value))
                    Recalculate();
            }
        }

        public double LandingSlabThicknessMm
        {
            get => _landingSlabThicknessMm;
            set
            {
                if (SetProperty(ref _landingSlabThicknessMm, value))
                    Recalculate();
            }
        }

        public NamedItem? SelectedFloorType
        {
            get => _selectedFloorType;
            set => SetProperty(ref _selectedFloorType, value);
        }

        public NamedItem? SelectedBeamSymbol
        {
            get => _selectedBeamSymbol;
            set => SetProperty(ref _selectedBeamSymbol, value);
        }

        public double InsertionPointX
        {
            get => _insertionPointX;
            set
            {
                if (SetProperty(ref _insertionPointX, value))
                    Recalculate();
            }
        }

        public double InsertionPointY
        {
            get => _insertionPointY;
            set
            {
                if (SetProperty(ref _insertionPointY, value))
                    Recalculate();
            }
        }

        public double RotationAngleDeg
        {
            get => _rotationAngleDeg;
            set
            {
                if (SetProperty(ref _rotationAngleDeg, value))
                    Recalculate();
            }
        }

        public double RiserHeightMm
        {
            get => _riserHeightMm;
            private set => SetProperty(ref _riserHeightMm, value);
        }

        public double BlondelNumberMm
        {
            get => _blondelNumberMm;
            private set => SetProperty(ref _blondelNumberMm, value);
        }

        public double InclineAngleDeg
        {
            get => _inclineAngleDeg;
            private set => SetProperty(ref _inclineAngleDeg, value);
        }

        public string BlondelStatus
        {
            get => _blondelStatus;
            private set => SetProperty(ref _blondelStatus, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public StairGeometryResult GeometryResult
        {
            get => _geometryResult;
            private set => SetProperty(ref _geometryResult, value);
        }

        #endregion

        #region Commands

        public ICommand CreateStairCommand { get; }
        public ICommand PickPointCommand { get; }
        public ICommand ResetCommand { get; }

        #endregion

        #region Logic & Recalculate

        public StairConfig GetCurrentConfig()
        {
            return new StairConfig
            {
                StairType = StairType,
                TurnDirection = TurnDirection,
                SupportBeamOption = SupportBeamOption,

                BaseLevelId = SelectedBaseLevel?.Id ?? string.Empty,
                TopLevelId = SelectedTopLevel?.Id ?? string.Empty,
                BaseElevationMm = SelectedBaseLevel?.ElevationMm ?? 0.0,
                TopElevationMm = SelectedTopLevel?.ElevationMm ?? 3600.0,
                BaseOffsetMm = BaseOffsetMm,
                TopOffsetMm = TopOffsetMm,

                TotalRisers = TotalRisers,
                Flight1Risers = Flight1Risers,
                Flight2Risers = Flight2Risers,
                TreadDepthMm = TreadDepthMm,
                StairWidthMm = StairWidthMm,
                LandingLengthMm = LandingLengthMm,
                GapBetweenFlightsMm = GapBetweenFlightsMm,

                WaistSlabThicknessMm = WaistSlabThicknessMm,
                LandingSlabThicknessMm = LandingSlabThicknessMm,

                SelectedFloorTypeId = SelectedFloorType?.Id ?? string.Empty,
                SelectedBeamSymbolId = SelectedBeamSymbol?.Id ?? string.Empty,

                InsertionPointX = InsertionPointX,
                InsertionPointY = InsertionPointY,
                RotationAngleDeg = RotationAngleDeg
            };
        }

        public void Recalculate()
        {
            OnPropertyChanged(nameof(TotalRiseMm));

            var config = GetCurrentConfig();
            GeometryResult = StairGeometryEngine.Generate(config);

            if (GeometryResult.Success)
            {
                RiserHeightMm = GeometryResult.RiserHeightMm;
                BlondelNumberMm = GeometryResult.BlondelNumberMm;
                InclineAngleDeg = GeometryResult.InclineAngleDeg;

                if (BlondelNumberMm >= 600.0 && BlondelNumberMm <= 640.0)
                {
                    BlondelStatus = "Chuẩn TCVN (Lý tưởng)";
                }
                else if (BlondelNumberMm >= 580.0 && BlondelNumberMm <= 660.0)
                {
                    BlondelStatus = "Chấp nhận được";
                }
                else
                {
                    BlondelStatus = "Cần điều chỉnh bậc";
                }

                StatusMessage = GeometryResult.WarningMessage ?? "Sẵn sàng tạo cầu thang kết cấu";
            }
            else
            {
                StatusMessage = GeometryResult.ErrorMessage ?? "Thông số chưa hợp lệ";
            }

            RequestPreviewUpdate?.Invoke();
        }

        public void LoadRevitData(Document doc)
        {
            if (doc == null) return;

            Levels.Clear();
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            foreach (var lvl in levels)
            {
                double elevMm = UnitUtils.ConvertFromInternalUnits(lvl.Elevation, UnitTypeId.Millimeters);
                Levels.Add(new LevelComboItem
                {
                    Id = lvl.Id.GetIdValue().ToString(),
                    Name = $"{lvl.Name} ({elevMm:F0} mm)",
                    ElevationMm = elevMm
                });
            }

            if (Levels.Count >= 2)
            {
                SelectedBaseLevel = Levels[0];
                SelectedTopLevel = Levels[1];
            }
            else if (Levels.Count == 1)
            {
                SelectedBaseLevel = Levels[0];
                SelectedTopLevel = Levels[0];
            }

            // Floor Types
            FloorTypes.Clear();
            var floorTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .OrderBy(ft => ft.Name)
                .ToList();

            foreach (var ft in floorTypes)
            {
                FloorTypes.Add(new NamedItem
                {
                    Id = ft.Id.GetIdValue().ToString(),
                    Name = ft.Name
                });
            }

            if (FloorTypes.Count > 0)
            {
                // Ưu tiên chọn loại sàn có từ khóa "Stair", "Thang" hoặc "Concrete"
                var defaultFt = FloorTypes.FirstOrDefault(f => f.Name.IndexOf("thang", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                               f.Name.IndexOf("stair", StringComparison.OrdinalIgnoreCase) >= 0)
                                ?? FloorTypes.FirstOrDefault(f => f.Name.IndexOf("concrete", StringComparison.OrdinalIgnoreCase) >= 0)
                                ?? FloorTypes[0];
                SelectedFloorType = defaultFt;
            }

            // Beam Symbols
            BeamSymbols.Clear();
            var beamSymbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_StructuralFraming)
                .Cast<FamilySymbol>()
                .OrderBy(s => s.FamilyName).ThenBy(s => s.Name)
                .ToList();

            foreach (var sym in beamSymbols)
            {
                BeamSymbols.Add(new NamedItem
                {
                    Id = sym.Id.GetIdValue().ToString(),
                    Name = $"{sym.FamilyName}: {sym.Name}"
                });
            }

            if (BeamSymbols.Count > 0)
            {
                SelectedBeamSymbol = BeamSymbols[0];
            }

            Recalculate();
        }

        private void OnPickPoint()
        {
            StatusMessage = "Vui lòng click chọn điểm đặt trên màn hình Revit...";

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;

                try
                {
                    var pt = uidoc.Selection.PickPoint("Chọn điểm đặt chân thang");
                    InsertionPointX = UnitUtils.ConvertFromInternalUnits(pt.X, UnitTypeId.Millimeters);
                    InsertionPointY = UnitUtils.ConvertFromInternalUnits(pt.Y, UnitTypeId.Millimeters);
                    StatusMessage = $"Đã chọn điểm đặt: ({InsertionPointX:F0}, {InsertionPointY:F0}) mm";
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    StatusMessage = "Đã hủy thao tác chọn điểm đặt.";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Lỗi chọn điểm: {ex.Message}";
                }
            });
        }

        private void OnCreateStair()
        {
            if (!GeometryResult.Success)
            {
                TaskDialog.Show("JNN Stair", GeometryResult.ErrorMessage ?? "Thông số thang chưa hợp lệ.");
                return;
            }

            if (SelectedFloorType == null)
            {
                TaskDialog.Show("JNN Stair", "Vui lòng chọn loại sàn (Floor Type) cho thang.");
                return;
            }

            StatusMessage = "Đang tạo cấu kiện thang trong Revit...";
            var config = GetCurrentConfig();

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                var (success, message, createdIds) = StairModelBuilder.BuildStair(doc, config);

                StatusMessage = message;

                if (success)
                {
                    if (createdIds != null && createdIds.Count > 0)
                    {
                        try
                        {
                            uidoc.Selection.SetElementIds(createdIds);
                            uidoc.ShowElements(createdIds);
                        }
                        catch { }
                    }

                    string idListStr = (createdIds != null && createdIds.Count > 0) 
                        ? string.Join(", ", createdIds.Select(id => id.GetIdValue())) 
                        : string.Empty;

                    TaskDialog.Show("JNN Auto Structural Stair", 
                        $"{message}\n\nĐã tự động chọn (Select) và Zoom tới {createdIds?.Count ?? 0} cấu kiện vừa tạo (IDs: {idListStr}).\n\n" +
                        "Lưu ý: Thang kết cấu được tạo dưới dạng Floors (Sàn nghiêng + Chiếu nghỉ) và Structural Framing (Dầm đỡ), không phải category Stairs kiến trúc.");
                }
                else
                {
                    TaskDialog.Show("Lỗi tạo thang", message);
                }
            });
        }

        private void OnReset()
        {
            StairType = StairType.UShaped;
            TurnDirection = TurnDirection.Left;
            SupportBeamOption = SupportBeamOption.LandingBeamOnly;

            TotalRisers = 21;
            Flight1Risers = 11;
            Flight2Risers = 10;
            TreadDepthMm = 280.0;
            StairWidthMm = 1100.0;
            LandingLengthMm = 1200.0;
            GapBetweenFlightsMm = 100.0;

            WaistSlabThicknessMm = 120.0;
            LandingSlabThicknessMm = 120.0;

            BaseOffsetMm = 0.0;
            TopOffsetMm = 0.0;
            InsertionPointX = 0.0;
            InsertionPointY = 0.0;
            RotationAngleDeg = 0.0;

            Recalculate();
        }

        #endregion
    }
}
