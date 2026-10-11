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
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.Tools.AutoDimColumn.Commands;
using JNNTool.Tools.AutoDimColumn.Models;
using JNNTool.Tools.AutoDimColumn.Services;

namespace JNNTool.Tools.AutoDimColumn.ViewModels
{
    public class DimensionTypeOption
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class PlacementSideOption
    {
        public ColumnPlacementSide Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public partial class AutoDimColumnViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        private AutoDimColumnSettings _settings;
        private AutoDimColumnHandler? _handler;
        private ExternalEvent? _exEvent;

        [ObservableProperty] private bool _isActiveViewScope = true;
        [ObservableProperty] private bool _isSelectionScope = false;

        [ObservableProperty] private bool _dimWidth = true;
        [ObservableProperty] private bool _dimLength = true;
        [ObservableProperty] private bool _dimGridToColumn = true;
        [ObservableProperty] private bool _includeOppositeFace = true;
        [ObservableProperty] private bool _skipExisting = true;

        [ObservableProperty] private double _baseOffsetMm = 350.0;
        [ObservableProperty] private double _chainSpacingMm = 250.0;
        [ObservableProperty] private double _gridSearchRangeMm = 2500.0;
        [ObservableProperty] private double _angleToleranceDeg = 5.0;

        [ObservableProperty] private DimensionTypeOption? _selectedDimensionType;
        [ObservableProperty] private PlacementSideOption? _selectedPlacementSide;

        [ObservableProperty] private int _totalColumnsCount;
        [ObservableProperty] private int _readyColumnsCount;
        [ObservableProperty] private int _plannedDimsCount;
        [ObservableProperty] private int _skippedCount;

        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private string _resultReport = string.Empty;

        public ObservableCollection<DimensionTypeOption> DimensionTypes { get; } = new ObservableCollection<DimensionTypeOption>();
        public ObservableCollection<PlacementSideOption> PlacementSides { get; } = new ObservableCollection<PlacementSideOption>();
        public ObservableCollection<ColumnDimPreviewItem> PreviewItems { get; } = new ObservableCollection<ColumnDimPreviewItem>();

        public ICommand PreviewCommand { get; }
        public ICommand CreateDimensionsCommand { get; }
        public ICommand ResetSettingsCommand { get; }
        public ICommand SelectAllCommand { get; }
        public ICommand UnselectAllCommand { get; }

        public AutoDimColumnViewModel()
        {
            _settings = SettingsService<AutoDimColumnSettings>.Load() ?? new AutoDimColumnSettings();
            LoadSettingsToProperties(_settings);

            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.Auto, Label = "Auto (Exterior / Clear side)" });
            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.TopLeft, Label = "Top - Left" });
            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.BottomRight, Label = "Bottom - Right" });
            SelectedPlacementSide = PlacementSides.FirstOrDefault(p => p.Value == _settings.PlacementSide) ?? PlacementSides[0];

            PreviewCommand = new RelayCommand(RunPreview);
            CreateDimensionsCommand = new RelayCommand(RunCreateDimensions);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
            SelectAllCommand = new RelayCommand(() => SetSelection(true));
            UnselectAllCommand = new RelayCommand(() => SetSelection(false));
        }

        public void Initialize(UIApplication app, AutoDimColumnHandler handler, ExternalEvent exEvent)
        {
            _handler = handler;
            _exEvent = exEvent;

            var uidoc = app.ActiveUIDocument;
            if (uidoc == null) return;
            var doc = uidoc.Document;

            // Nạp danh sách DimensionType tuyến tính
            DimensionTypes.Clear();
            var types = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .WhereElementIsElementType()
                .Cast<DimensionType>()
                .Where(dt => dt.StyleType == DimensionStyleType.Linear)
                .OrderBy(dt => dt.Name);

            foreach (var dt in types)
            {
                long idVal = ColumnCollector.GetIdValue(dt.Id);
                DimensionTypes.Add(new DimensionTypeOption
                {
                    Id = idVal.ToString(),
                    Name = dt.Name
                });
            }

            if (!string.IsNullOrEmpty(_settings.SelectedDimensionTypeId))
            {
                SelectedDimensionType = DimensionTypes.FirstOrDefault(d => d.Id == _settings.SelectedDimensionTypeId);
            }
            if (SelectedDimensionType == null && DimensionTypes.Count > 0)
            {
                SelectedDimensionType = DimensionTypes[0];
            }

            // Chạy phân tích cột đồng bộ ngay trên Revit UI thread trong Command.Execute
            DoAnalyze(app);
        }

        public void RunPreview()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Analyzing columns and grids...";

            SavePropertiesToSettings();
            SettingsService<AutoDimColumnSettings>.Save(_settings);

            if (_handler != null && _exEvent != null)
            {
                _handler.RequestAction = app => DoAnalyze(app);
                _exEvent.Raise();
            }
            else
            {
                IsBusy = false;
            }
        }

        public void DoAnalyze(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null)
                {
                    UpdateUiAfterAnalyze(new List<ColumnDimPreviewItem>(), 0, 0, 0, 0, "No active document.");
                    return;
                }

                var doc = uidoc.Document;
                var view = doc.ActiveView;

                if (view is not ViewPlan)
                {
                    UpdateUiAfterAnalyze(new List<ColumnDimPreviewItem>(), 0, 0, 0, 0, "Please open a Structural / Floor Plan View.");
                    return;
                }

                SavePropertiesToSettings();

                var rawCols = ColumnCollector.CollectColumns(uidoc, doc, view, _settings.ScopeMode);
                var grids = GridDetectionService.CollectGridsInView(doc, view);

                var items = new List<ColumnDimPreviewItem>();
                int plannedTotal = 0;
                int readyTotal = 0;
                int skippedTotal = 0;

                foreach (var c in rawCols)
                {
                    var geo = ColumnGeometryService.ExtractGeometry(c, view);
                    if (geo.IsSupported)
                    {
                        var matchedGrids = GridDetectionService.FindAssociatedGrids(geo, grids, _settings);
                        geo.AssociatedGrids = matchedGrids;

                        int count = 0;
                        if (_settings.DimWidth && geo.WidthPair != null) count++;
                        if (_settings.DimLength && geo.LengthPair != null) count++;
                        if (_settings.DimGridToColumn) count += matchedGrids.Count;

                        plannedTotal += count;
                        readyTotal++;

                        string gridNames = matchedGrids.Count > 0
                            ? string.Join(", ", matchedGrids.Select(g => g.GridName))
                            : "None";

                        items.Add(new ColumnDimPreviewItem
                        {
                            IsSelected = true,
                            IdText = ColumnCollector.GetIdValue(geo.ColumnId).ToString(),
                            Mark = string.IsNullOrEmpty(geo.Mark) ? "-" : geo.Mark,
                            TypeName = geo.TypeName,
                            SizeText = $"{Math.Round(geo.WidthMm)} × {Math.Round(geo.LengthMm)} mm",
                            RotationText = $"{geo.RotationAngleDeg}°",
                            GridsText = gridNames,
                            PlannedDimsCount = count,
                            Status = "Ready",
                            RawInfo = geo
                        });
                    }
                    else
                    {
                        skippedTotal++;
                        items.Add(new ColumnDimPreviewItem
                        {
                            IsSelected = false,
                            IdText = ColumnCollector.GetIdValue(c.Id).ToString(),
                            Mark = c.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)?.AsString() ?? "-",
                            TypeName = c.Symbol?.Name ?? "Column",
                            SizeText = "-",
                            RotationText = "-",
                            GridsText = "-",
                            PlannedDimsCount = 0,
                            Status = geo.StatusMessage,
                            RawInfo = geo
                        });
                    }
                }

                string statusMsg = rawCols.Count == 0
                    ? (_settings.ScopeMode == ColumnScopeMode.SelectedElements ? "No columns selected in model." : "No columns found in active view.")
                    : $"Found {rawCols.Count} columns ({readyTotal} ready, {skippedTotal} skipped), {plannedTotal} dims planned.";

                UpdateUiAfterAnalyze(items, rawCols.Count, readyTotal, plannedTotal, skippedTotal, statusMsg);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in DoAnalyze", ex);
                UpdateUiAfterAnalyze(new List<ColumnDimPreviewItem>(), 0, 0, 0, 0, $"Analysis Error: {ex.Message}");
            }
        }

        private void UpdateUiAfterAnalyze(List<ColumnDimPreviewItem> items, int total, int ready, int planned, int skipped, string status)
        {
            void Apply()
            {
                PreviewItems.Clear();
                foreach (var it in items) PreviewItems.Add(it);

                TotalColumnsCount = total;
                ReadyColumnsCount = ready;
                PlannedDimsCount = planned;
                SkippedCount = skipped;
                StatusText = status;
                IsBusy = false;
            }

            if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(Apply);
            }
            else
            {
                Apply();
            }
        }

        public void RunCreateDimensions()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Creating dimensions in model...";

            SavePropertiesToSettings();
            SettingsService<AutoDimColumnSettings>.Save(_settings);

            var selectedInfos = PreviewItems
                .Where(p => p.IsSelected && p.RawInfo != null && p.RawInfo.IsSupported)
                .Select(p => p.RawInfo!)
                .ToList();

            if (selectedInfos.Count == 0)
            {
                StatusText = "No ready columns selected.";
                IsBusy = false;
                return;
            }

            if (_handler != null && _exEvent != null)
            {
                _handler.RequestAction = app => DoCreate(app, selectedInfos);
                _exEvent.Raise();
            }
            else
            {
                IsBusy = false;
            }
        }

        private void DoCreate(UIApplication app, List<ColumnGeometryInfo> selectedInfos)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;
                var view = doc.ActiveView;

                ColumnDimExecutionReport report;
                using (var tg = new TransactionGroup(doc, "JNN - Auto Column Dimension"))
                {
                    tg.Start();
                    using (var tx = new Transaction(doc, "Create Column Dimensions"))
                    {
                        tx.Start();
                        report = ColumnDimensionBuilder.CreateDimensions(doc, view, selectedInfos, _settings);
                        tx.Commit();
                    }
                    tg.Assimilate();
                }

                void Update()
                {
                    StatusText = $"Done: Created {report.SuccessDims} dimensions. ({report.SkippedDims} skipped, {report.FailedCount} failed).";
                    ResultReport = $"Created {report.SuccessDims} dims across {report.TotalColumns} columns.";
                    IsBusy = false;

                    // Cập nhật lại phân tích
                    RunPreview();
                }

                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(Update);
                }
                else
                {
                    Update();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Error in DoCreate", ex);
                void Fail()
                {
                    StatusText = $"Creation Error: {ex.Message}";
                    IsBusy = false;
                }
                if (System.Windows.Application.Current?.Dispatcher?.CheckAccess() == false)
                    System.Windows.Application.Current.Dispatcher.Invoke(Fail);
                else
                    Fail();
            }
        }

        private void SetSelection(bool select)
        {
            foreach (var item in PreviewItems)
            {
                if (item.RawInfo != null && item.RawInfo.IsSupported)
                {
                    item.IsSelected = select;
                }
            }
        }

        private void ResetSettings()
        {
            _settings = new AutoDimColumnSettings();
            LoadSettingsToProperties(_settings);
            SettingsService<AutoDimColumnSettings>.Save(_settings);
            StatusText = "Settings reset to defaults.";
            RunPreview();
        }

        private void LoadSettingsToProperties(AutoDimColumnSettings s)
        {
            IsActiveViewScope = s.ScopeMode == ColumnScopeMode.ActiveView;
            IsSelectionScope = s.ScopeMode == ColumnScopeMode.SelectedElements;

            DimWidth = s.DimWidth;
            DimLength = s.DimLength;
            DimGridToColumn = s.DimGridToColumn;
            IncludeOppositeFace = s.IncludeOppositeFace;
            SkipExisting = s.SkipExisting;

            BaseOffsetMm = s.BaseOffsetMm;
            ChainSpacingMm = s.ChainSpacingMm;
            GridSearchRangeMm = s.GridSearchRangeMm;
            AngleToleranceDeg = s.AngleToleranceDeg;
        }

        private void SavePropertiesToSettings()
        {
            _settings.ScopeMode = IsActiveViewScope ? ColumnScopeMode.ActiveView : ColumnScopeMode.SelectedElements;
            _settings.DimWidth = DimWidth;
            _settings.DimLength = DimLength;
            _settings.DimGridToColumn = DimGridToColumn;
            _settings.IncludeOppositeFace = IncludeOppositeFace;
            _settings.SkipExisting = SkipExisting;

            _settings.BaseOffsetMm = BaseOffsetMm;
            _settings.ChainSpacingMm = ChainSpacingMm;
            _settings.GridSearchRangeMm = GridSearchRangeMm;
            _settings.AngleToleranceDeg = AngleToleranceDeg;

            _settings.SelectedDimensionTypeId = SelectedDimensionType?.Id ?? string.Empty;
            _settings.PlacementSide = SelectedPlacementSide?.Value ?? ColumnPlacementSide.Auto;
        }
    }
}
