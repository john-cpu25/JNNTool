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

        [ObservableProperty] private string _statusText = "Ready";
        [ObservableProperty] private bool _isBusy;

        public ObservableCollection<DimensionTypeOption> DimensionTypes { get; } = new ObservableCollection<DimensionTypeOption>();
        public ObservableCollection<PlacementSideOption> PlacementSides { get; } = new ObservableCollection<PlacementSideOption>();

        public ICommand CreateDimensionsCommand { get; }
        public ICommand ResetSettingsCommand { get; }

        public AutoDimColumnViewModel()
        {
            _settings = SettingsService<AutoDimColumnSettings>.Load() ?? new AutoDimColumnSettings();
            LoadSettingsToProperties(_settings);

            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.Auto, Label = "Auto (Exterior / Clear side)" });
            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.TopLeft, Label = "Top - Left" });
            PlacementSides.Add(new PlacementSideOption { Value = ColumnPlacementSide.BottomRight, Label = "Bottom - Right" });
            SelectedPlacementSide = PlacementSides.FirstOrDefault(p => p.Value == _settings.PlacementSide) ?? PlacementSides[0];

            CreateDimensionsCommand = new RelayCommand(RunCreateDimensions);
            ResetSettingsCommand = new RelayCommand(ResetSettings);
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

            StatusText = "Ready";
            IsBusy = false;
        }

        public void RunCreateDimensions()
        {
            if (IsBusy) return;
            IsBusy = true;
            StatusText = "Creating dimensions in model...";

            SavePropertiesToSettings();
            SettingsService<AutoDimColumnSettings>.Save(_settings);

            if (_handler != null && _exEvent != null)
            {
                _handler.RequestAction = app => DoCreate(app);
                _exEvent.Raise();
            }
            else
            {
                IsBusy = false;
                StatusText = "External event handler not initialized.";
            }
        }

        private void DoCreate(UIApplication app)
        {
            try
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null)
                {
                    UpdateStatus("No active document.");
                    return;
                }

                var doc = uidoc.Document;
                var view = doc.ActiveView;

                if (view is not ViewPlan)
                {
                    UpdateStatus("Please open a Structural / Floor Plan View.");
                    return;
                }

                SavePropertiesToSettings();

                // 1. Thu thập danh sách cột
                var rawCols = ColumnCollector.CollectColumns(uidoc, doc, view, _settings.ScopeMode);
                if (rawCols.Count == 0)
                {
                    string msg = _settings.ScopeMode == ColumnScopeMode.SelectedElements
                        ? "No columns selected in model."
                        : "No columns found in active view.";
                    UpdateStatus(msg);
                    return;
                }

                // 2. Thu thập danh sách lưới trục
                var grids = GridDetectionService.CollectGridsInView(doc, view);

                // 3. Trích xuất hình học và bounding box cho từng cột
                var readyCols = new List<ColumnGeometryInfo>();
                foreach (var c in rawCols)
                {
                    var geo = ColumnGeometryService.ExtractGeometry(c, view);
                    if (geo.IsSupported)
                    {
                        if (_settings.DimGridToColumn)
                        {
                            geo.AssociatedGrids = GridDetectionService.FindAssociatedGrids(geo, grids, _settings);
                        }
                        readyCols.Add(geo);
                    }
                }

                if (readyCols.Count == 0)
                {
                    UpdateStatus("No supported rectangular columns found.");
                    return;
                }

                // 4. Tạo Dimension trong Transaction
                ColumnDimExecutionReport report;
                using (var tg = new TransactionGroup(doc, "JNN - Auto Column Dimension"))
                {
                    tg.Start();
                    using (var tx = new Transaction(doc, "Create Column Dimensions"))
                    {
                        tx.Start();
                        report = ColumnDimensionBuilder.CreateDimensions(doc, view, readyCols, _settings);
                        tx.Commit();
                    }
                    tg.Assimilate();
                }

                string summary = $"Done: Created {report.SuccessDims} dimensions ({readyCols.Count} columns).";
                if (report.FailedCount > 0)
                {
                    summary += $" ({report.FailedCount} failed)";
                }
                UpdateStatus(summary);
            }
            catch (Exception ex)
            {
                Logger.Error("Error in DoCreate Auto Column Dimension", ex);
                UpdateStatus($"Creation Error: {ex.Message}");
            }
        }

        private void UpdateStatus(string message)
        {
            void Apply()
            {
                StatusText = message;
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

        private void ResetSettings()
        {
            _settings = new AutoDimColumnSettings();
            LoadSettingsToProperties(_settings);
            SettingsService<AutoDimColumnSettings>.Save(_settings);
            StatusText = "Settings reset to defaults.";
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
