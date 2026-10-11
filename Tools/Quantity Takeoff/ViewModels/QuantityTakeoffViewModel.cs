using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.Tools.QuantityTakeoff.Commands;
using JNNTool.Tools.QuantityTakeoff.Engine;
using JNNTool.Tools.QuantityTakeoff.Models;
using Microsoft.Win32;

namespace JNNTool.Tools.QuantityTakeoff.ViewModels
{
    public class SimpleRelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public SimpleRelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public SimpleRelayCommand(Action execute) : this(_ => execute(), null) { }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class QuantityTakeoffViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;
        private UIApplication? _uiApp;
        private QuantityTakeoffHandler? _handler;
        private ExternalEvent? _externalEvent;

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetField(ref _isBusy, value);
        }

        private string _statusMessage = "Sẵn sàng (Ready)";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public ObservableCollection<string> ScopeOptions { get; } = new ObservableCollection<string>
        {
            "Current Selection",
            "Active View",
            "Entire Project"
        };

        private string _selectedScopeOption = "Active View";
        public string SelectedScopeOption
        {
            get => _selectedScopeOption;
            set
            {
                if (SetField(ref _selectedScopeOption, value))
                {
                    if (value == "Current Selection") _scope = TakeoffScope.Selection;
                    else if (value == "Active View") _scope = TakeoffScope.ActiveView;
                    else _scope = TakeoffScope.EntireProject;
                    OnPropertyChanged(nameof(Scope));
                    RefreshData();
                }
            }
        }

        private TakeoffScope _scope = TakeoffScope.ActiveView;
        public TakeoffScope Scope
        {
            get => _scope;
            set
            {
                if (SetField(ref _scope, value))
                {
                    if (_scope == TakeoffScope.Selection) _selectedScopeOption = "Current Selection";
                    else if (_scope == TakeoffScope.ActiveView) _selectedScopeOption = "Active View";
                    else _selectedScopeOption = "Entire Project";
                    OnPropertyChanged(nameof(SelectedScopeOption));
                    RefreshData();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetField(ref _searchText, value))
                {
                    _elementsView?.Refresh();
                }
            }
        }

        private string _selectedCategoryFilter = "All Categories";
        public string SelectedCategoryFilter
        {
            get => _selectedCategoryFilter;
            set
            {
                if (SetField(ref _selectedCategoryFilter, value))
                {
                    _elementsView?.Refresh();
                }
            }
        }

        private string _selectedLevelFilter = "All Levels";
        public string SelectedLevelFilter
        {
            get => _selectedLevelFilter;
            set
            {
                if (SetField(ref _selectedLevelFilter, value))
                {
                    _elementsView?.Refresh();
                }
            }
        }

        // Summary Statistics Displays
        private string _totalConcreteVolumeDisplay = "0.00 m³";
        public string TotalConcreteVolumeDisplay
        {
            get => _totalConcreteVolumeDisplay;
            set => SetField(ref _totalConcreteVolumeDisplay, value);
        }

        private string _totalFormworkAreaDisplay = "0.00 m²";
        public string TotalFormworkAreaDisplay
        {
            get => _totalFormworkAreaDisplay;
            set => SetField(ref _totalFormworkAreaDisplay, value);
        }

        private string _totalSteelWeightDisplay = "0.000 Tấn";
        public string TotalSteelWeightDisplay
        {
            get => _totalSteelWeightDisplay;
            set => SetField(ref _totalSteelWeightDisplay, value);
        }

        private string _totalElementsCountDisplay = "0";
        public string TotalElementsCountDisplay
        {
            get => _totalElementsCountDisplay;
            set => SetField(ref _totalElementsCountDisplay, value);
        }

        // Preview Settings & State
        private bool _isPreviewVisible = true;
        public bool IsPreviewVisible
        {
            get => _isPreviewVisible;
            set => SetField(ref _isPreviewVisible, value);
        }

        private int _previewMode = 0; // 0: By Level Stack, 1: By Category
        public int PreviewMode
        {
            get => _previewMode;
            set
            {
                if (SetField(ref _previewMode, value))
                {
                    OnPropertyChanged(nameof(IsLevelPreviewMode));
                    OnPropertyChanged(nameof(IsCategoryPreviewMode));
                }
            }
        }

        public bool IsLevelPreviewMode => PreviewMode == 0;
        public bool IsCategoryPreviewMode => PreviewMode == 1;

        public ObservableCollection<LevelChartItem> LevelChartItems { get; } = new ObservableCollection<LevelChartItem>();
        public ObservableCollection<CategoryChartItem> CategoryChartItems { get; } = new ObservableCollection<CategoryChartItem>();

        private LevelChartItem? _selectedLevelChartItem;
        public LevelChartItem? SelectedLevelChartItem
        {
            get => _selectedLevelChartItem;
            set
            {
                if (SetField(ref _selectedLevelChartItem, value))
                {
                    foreach (var itm in LevelChartItems)
                    {
                        itm.IsSelected = itm == value;
                    }
                    if (value != null)
                    {
                        var matchingSummary = LevelSummaries.FirstOrDefault(l => l.LevelName == value.LevelName);
                        if (matchingSummary != null && SelectedLevelSummary != matchingSummary)
                        {
                            SelectedLevelSummary = matchingSummary;
                        }
                    }
                }
            }
        }

        // Collections
        public ObservableCollection<CategoryFilterItem> Categories { get; } = new ObservableCollection<CategoryFilterItem>();
        public ObservableCollection<LevelSummaryItem> LevelSummaries { get; } = new ObservableCollection<LevelSummaryItem>();
        public ObservableCollection<CategorySummaryItem> CategorySummaries { get; } = new ObservableCollection<CategorySummaryItem>();
        public ObservableCollection<TakeoffElementItem> Elements { get; } = new ObservableCollection<TakeoffElementItem>();

        public ObservableCollection<string> AvailableCategoryNames { get; } = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableLevelNames { get; } = new ObservableCollection<string>();

        private LevelSummaryItem? _selectedLevelSummary;
        public LevelSummaryItem? SelectedLevelSummary
        {
            get => _selectedLevelSummary;
            set
            {
                if (SetField(ref _selectedLevelSummary, value))
                {
                    CurrentLevelBreakdowns.Clear();
                    if (value?.CategoryBreakdowns != null)
                    {
                        foreach (var b in value.CategoryBreakdowns)
                        {
                            CurrentLevelBreakdowns.Add(b);
                        }
                    }

                    if (value != null)
                    {
                        var matchingChart = LevelChartItems.FirstOrDefault(c => c.LevelName == value.LevelName);
                        if (matchingChart != null && SelectedLevelChartItem != matchingChart)
                        {
                            _selectedLevelChartItem = matchingChart;
                            foreach (var itm in LevelChartItems)
                            {
                                itm.IsSelected = itm == matchingChart;
                            }
                            OnPropertyChanged(nameof(SelectedLevelChartItem));
                        }
                    }
                }
            }
        }

        public ObservableCollection<CategorySummaryItem> CurrentLevelBreakdowns { get; } = new ObservableCollection<CategorySummaryItem>();

        private CategorySummaryItem? _selectedCategorySummary;
        public CategorySummaryItem? SelectedCategorySummary
        {
            get => _selectedCategorySummary;
            set => SetField(ref _selectedCategorySummary, value);
        }

        private TakeoffElementItem? _selectedElement;
        public TakeoffElementItem? SelectedElement
        {
            get => _selectedElement;
            set => SetField(ref _selectedElement, value);
        }

        private ICollectionView? _elementsView;
        public ICollectionView? ElementsView => _elementsView;

        // Commands
        public ICommand RefreshDataCommand { get; }
        public ICommand Box3DCommand { get; }
        public ICommand SelectInModelCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand SelectAllCategoriesCommand { get; }
        public ICommand SelectStructuralOnlyCommand { get; }
        public ICommand ClearCategoriesCommand { get; }
        public ICommand ToggleSelectAllElementsCommand { get; }
        public ICommand TogglePreviewCommand { get; }
        public ICommand SetPreviewModeCommand { get; }
        public ICommand SelectChartLevelCommand { get; }

        public QuantityTakeoffViewModel()
        {
            RefreshDataCommand = new SimpleRelayCommand(RefreshData);
            Box3DCommand = new SimpleRelayCommand(p => Box3D(p));
            SelectInModelCommand = new SimpleRelayCommand(p => SelectInModel(p));
            ExportExcelCommand = new SimpleRelayCommand(ExportExcel);
            SelectAllCategoriesCommand = new SimpleRelayCommand(SelectAllCategories);
            SelectStructuralOnlyCommand = new SimpleRelayCommand(SelectStructuralOnly);
            ClearCategoriesCommand = new SimpleRelayCommand(ClearCategories);
            ToggleSelectAllElementsCommand = new SimpleRelayCommand(p => ToggleSelectAllElements(p as bool?));
            TogglePreviewCommand = new SimpleRelayCommand(() => IsPreviewVisible = !IsPreviewVisible);
            SetPreviewModeCommand = new SimpleRelayCommand(p =>
            {
                if (int.TryParse(p?.ToString(), out int m))
                {
                    PreviewMode = m;
                }
            });
            SelectChartLevelCommand = new SimpleRelayCommand(p =>
            {
                if (p is LevelChartItem itm)
                {
                    SelectedLevelChartItem = itm;
                }
            });
        }

        public void Initialize(UIApplication uiApp, QuantityTakeoffHandler handler, ExternalEvent externalEvent)
        {
            _uiApp = uiApp;
            _handler = handler;
            _externalEvent = externalEvent;

            // Tự động nhận diện Scope ban đầu:
            // Nếu người dùng đã chọn sẵn cấu kiện trên Revit canvas -> Dùng "Current Selection"
            // Nếu không chọn -> Dùng "Active View" (không bao giờ tự ý quét Entire Project để tránh quá tải)
            var selIds = uiApp.ActiveUIDocument?.Selection?.GetElementIds();
            if (selIds != null && selIds.Count > 0)
            {
                _scope = TakeoffScope.Selection;
                _selectedScopeOption = "Current Selection";
            }
            else
            {
                _scope = TakeoffScope.ActiveView;
                _selectedScopeOption = "Active View";
            }
            OnPropertyChanged(nameof(SelectedScopeOption));
            OnPropertyChanged(nameof(Scope));

            Categories.Clear();
            foreach (var cat in QuantityExtractorEngine.DefaultCategories)
            {
                cat.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(CategoryFilterItem.IsSelected))
                    {
                        RefreshData();
                    }
                };
                Categories.Add(cat);
            }

            _elementsView = CollectionViewSource.GetDefaultView(Elements);
            _elementsView.Filter = FilterElementPredicate;
            OnPropertyChanged(nameof(ElementsView));

            RefreshData();
        }

        private bool FilterElementPredicate(object obj)
        {
            if (obj is not TakeoffElementItem item) return false;

            if (!string.IsNullOrEmpty(SelectedCategoryFilter) && SelectedCategoryFilter != "All Categories")
            {
                if (!string.Equals(item.CategoryName, SelectedCategoryFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(SelectedLevelFilter) && SelectedLevelFilter != "All Levels")
            {
                if (!string.Equals(item.LevelName, SelectedLevelFilter, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string search = SearchText.Trim().ToLowerInvariant();
                bool matches = item.IdValue.ToString().Contains(search) ||
                               item.CategoryName.ToLowerInvariant().Contains(search) ||
                               item.LevelName.ToLowerInvariant().Contains(search) ||
                               item.FamilyName.ToLowerInvariant().Contains(search) ||
                               item.TypeName.ToLowerInvariant().Contains(search) ||
                               item.Mark.ToLowerInvariant().Contains(search) ||
                               item.MaterialName.ToLowerInvariant().Contains(search);
                if (!matches) return false;
            }

            return true;
        }

        public void RefreshData()
        {
            if (_uiApp?.ActiveUIDocument == null) return;
            IsBusy = true;
            StatusMessage = "Đang trích xuất khối lượng...";

            _handler?.SetAction(app =>
            {
                try
                {
                    var uidoc = app.ActiveUIDocument;
                    if (uidoc == null)
                    {
                        _dispatcher.Invoke(() => IsBusy = false);
                        return;
                    }

                    var doc = uidoc.Document;
                    var activeView = uidoc.ActiveView;
                    var selectedIds = uidoc.Selection.GetElementIds();

                    var selectedCats = Categories.Where(c => c.IsSelected)
                                                 .Select(c => c.BuiltInCategory)
                                                 .ToList();

                    var extracted = QuantityExtractorEngine.ExtractElements(
                        doc,
                        Scope,
                        selectedCats,
                        activeView,
                        selectedIds);

                    _dispatcher.Invoke(() =>
                    {
                        try
                        {
                            Elements.Clear();
                            foreach (var elem in extracted)
                            {
                                Elements.Add(elem);
                            }

                            var catCounts = extracted.GroupBy(e => e.BuiltInCategory)
                                                     .ToDictionary(g => g.Key, g => g.Count());
                            foreach (var cat in Categories)
                            {
                                cat.Count = catCounts.TryGetValue(cat.BuiltInCategory, out int cnt) ? cnt : 0;
                            }

                            var lvlSummaries = QuantityExtractorEngine.BuildLevelSummaries(extracted);
                            LevelSummaries.Clear();
                            foreach (var ls in lvlSummaries)
                            {
                                LevelSummaries.Add(ls);
                            }
                            if (LevelSummaries.Count > 0)
                            {
                                SelectedLevelSummary = LevelSummaries.FirstOrDefault();
                            }

                            var catSummaries = QuantityExtractorEngine.BuildCategorySummaries(extracted);
                            CategorySummaries.Clear();
                            foreach (var cs in catSummaries)
                            {
                                CategorySummaries.Add(cs);
                            }

                            // Cập nhật biểu đồ LevelChartItems (theo chiều cao từ trên xuống)
                            LevelChartItems.Clear();
                            double maxLvlVol = lvlSummaries.Count > 0 ? lvlSummaries.Max(l => l.TotalVolume) : 0;
                            double sumLvlVol = lvlSummaries.Sum(l => l.TotalVolume);
                            foreach (var ls in lvlSummaries.OrderByDescending(l => l.Elevation))
                            {
                                double pct = sumLvlVol > 0 ? (ls.TotalVolume / sumLvlVol) * 100.0 : 0;
                                double ratio = maxLvlVol > 0 ? (ls.TotalVolume / maxLvlVol) : 0;
                                LevelChartItems.Add(new LevelChartItem
                                {
                                    LevelName = ls.LevelName,
                                    Elevation = ls.Elevation,
                                    Volume = ls.TotalVolume,
                                    Formwork = ls.FormworkArea,
                                    Rebar = ls.SteelRebarWeight,
                                    Percentage = pct,
                                    BarWidthRatio = Math.Max(0.08, ratio),
                                    ElementIds = ls.ElementIds,
                                    IsSelected = SelectedLevelSummary?.LevelName == ls.LevelName
                                });
                            }

                            // Cập nhật biểu đồ CategoryChartItems
                            CategoryChartItems.Clear();
                            double sumCatTotal = catSummaries.Sum(c => c.TotalVolume);
                            string[] palette = { "#2563EB", "#0284C7", "#10B981", "#F59E0B", "#8B5CF6", "#EC4899", "#EF4444", "#14B8A6", "#64748B" };
                            int cIdx = 0;
                            foreach (var cs in catSummaries.OrderByDescending(c => c.TotalVolume))
                            {
                                double pct = sumCatTotal > 0 ? (cs.TotalVolume / sumCatTotal) * 100.0 : 0;
                                CategoryChartItems.Add(new CategoryChartItem
                                {
                                    CategoryName = cs.CategoryName,
                                    Volume = cs.TotalVolume,
                                    Percentage = pct,
                                    ElementCount = cs.Count,
                                    ColorHex = palette[cIdx % palette.Length],
                                    ElementIds = cs.ElementIds
                                });
                                cIdx++;
                            }

                            UpdateFilterDropdowns(extracted);

                            double totalConcrete = lvlSummaries.Sum(l => l.ConcreteVolume);
                            double totalFormwork = lvlSummaries.Sum(l => l.FormworkArea);
                            double totalSteel = lvlSummaries.Sum(l => l.SteelRebarWeight);
                            int totalCount = extracted.Sum(e => e.Count);

                            TotalConcreteVolumeDisplay = $"{totalConcrete:N2} m³";
                            TotalFormworkAreaDisplay = $"{totalFormwork:N2} m²";
                            TotalSteelWeightDisplay = $"{totalSteel:N3} Tấn";
                            TotalElementsCountDisplay = $"{totalCount:N0} cấu kiện";

                            _elementsView?.Refresh();

                            IsBusy = false;
                            StatusMessage = $"Đã tải {extracted.Count} cấu kiện ({DateTime.Now:HH:mm:ss})";
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("Lỗi cập nhật UI Quantity Takeoff", ex);
                            IsBusy = false;
                            StatusMessage = "Lỗi xử lý dữ liệu: " + ex.Message;
                        }
                    });
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trích xuất khối lượng", ex);
                    _dispatcher.Invoke(() =>
                    {
                        IsBusy = false;
                        StatusMessage = "Lỗi trích xuất: " + ex.Message;
                    });
                }
            });

            _externalEvent?.Raise();
        }

        private void UpdateFilterDropdowns(List<TakeoffElementItem> items)
        {
            var currentCat = SelectedCategoryFilter;
            AvailableCategoryNames.Clear();
            AvailableCategoryNames.Add("All Categories");
            foreach (var cat in items.Select(i => i.CategoryName).Distinct().OrderBy(c => c))
            {
                AvailableCategoryNames.Add(cat);
            }
            SelectedCategoryFilter = AvailableCategoryNames.Contains(currentCat) ? currentCat : "All Categories";

            var currentLvl = SelectedLevelFilter;
            AvailableLevelNames.Clear();
            AvailableLevelNames.Add("All Levels");
            foreach (var lvl in items.Select(i => i.LevelName).Distinct().OrderBy(l => l))
            {
                AvailableLevelNames.Add(lvl);
            }
            SelectedLevelFilter = AvailableLevelNames.Contains(currentLvl) ? currentLvl : "All Levels";
        }

        public void Box3D(object? parameter)
        {
            List<ElementId> targetIds = GetTargetElementIds(parameter);

            if (targetIds == null || targetIds.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn ít nhất một cấu kiện hoặc tầng để tạo Box 3D.",
                                "JNN — 3D Section Box",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            StatusMessage = $"Đang tạo Box 3D cho {targetIds.Count} cấu kiện...";

            _handler?.SetAction(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;

                bool success = SectionBoxEngine.FocusSectionBox(uidoc, targetIds);

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    StatusMessage = success
                        ? $"Đã tạo Box 3D cho {targetIds.Count} cấu kiện thành công!"
                        : "Không thể tạo Box 3D cho các cấu kiện này.";
                });
            });

            _externalEvent?.Raise();
        }

        public void SelectInModel(object? parameter)
        {
            List<ElementId> targetIds = GetTargetElementIds(parameter);
            if (targetIds == null || targetIds.Count == 0) return;

            _handler?.SetAction(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;

                try
                {
                    uidoc.Selection.SetElementIds(targetIds);
                    uidoc.ShowElements(targetIds);
                }
                catch { }
            });

            _externalEvent?.Raise();
        }

        private List<ElementId> GetTargetElementIds(object? parameter)
        {
            var list = new List<ElementId>();

            if (parameter is TakeoffElementItem singleItem && singleItem.ElementId != null)
            {
                list.Add(singleItem.ElementId);
            }
            else if (parameter is LevelSummaryItem levelItem && levelItem.ElementIds != null)
            {
                list.AddRange(levelItem.ElementIds);
            }
            else if (parameter is LevelChartItem chartLvl && chartLvl.ElementIds != null)
            {
                list.AddRange(chartLvl.ElementIds);
            }
            else if (parameter is CategorySummaryItem catItem && catItem.ElementIds != null)
            {
                list.AddRange(catItem.ElementIds);
            }
            else if (parameter is CategoryChartItem chartCat && chartCat.ElementIds != null)
            {
                list.AddRange(chartCat.ElementIds);
            }
            else
            {
                var checkedItems = Elements.Where(e => e.IsSelected && e.ElementId != null)
                                           .Select(e => e.ElementId)
                                           .ToList();
                if (checkedItems.Count > 0)
                {
                    list.AddRange(checkedItems);
                }
                else if (SelectedElement?.ElementId != null)
                {
                    list.Add(SelectedElement.ElementId);
                }
                else if (SelectedLevelSummary?.ElementIds != null)
                {
                    list.AddRange(SelectedLevelSummary.ElementIds);
                }
                else if (SelectedCategorySummary?.ElementIds != null)
                {
                    list.AddRange(SelectedCategorySummary.ElementIds);
                }
            }

            return list;
        }

        public void ExportExcel()
        {
            if (Elements.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu khối lượng để xuất. Vui lòng bấm 'Refresh' trước.",
                                "JNN — Export Excel",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            var saveDlg = new SaveFileDialog
            {
                Title = "Xuất Bảng Khối Lượng Excel (JNN QTO)",
                Filter = "Excel XML Workbook (*.xls)|*.xls|CSV (Comma Delimited) (*.csv)|*.csv",
                FileName = $"JNN_QuantityTakeoff_{DateTime.Now:yyyyMMdd_HHmm}.xls"
            };

            if (saveDlg.ShowDialog() != true) return;

            try
            {
                string projectName = _uiApp?.ActiveUIDocument?.Document?.Title ?? "Revit Project";
                string ext = Path.GetExtension(saveDlg.FileName).ToLowerInvariant();

                if (ext == ".csv")
                {
                    ExcelExportEngine.ExportToCsv(saveDlg.FileName, Elements.ToList());
                }
                else
                {
                    ExcelExportEngine.ExportToExcelXml(
                        saveDlg.FileName,
                        projectName,
                        LevelSummaries.ToList(),
                        CategorySummaries.ToList(),
                        Elements.ToList());
                }

                StatusMessage = $"Xuất file thành công: {Path.GetFileName(saveDlg.FileName)}";

                var openResult = MessageBox.Show(
                    $"Xuất bảng khối lượng thành công!\n\nĐường dẫn:\n{saveDlg.FileName}\n\nBạn có muốn mở file ngay bây giờ không?",
                    "JNN — Export Hoàn Tất",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (openResult == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(saveDlg.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi xuất file Excel trong Quantity Takeoff", ex);
                MessageBox.Show($"Lỗi xuất file: {ex.Message}", "JNN — Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void SelectAllCategories()
        {
            foreach (var cat in Categories) cat.IsSelected = true;
        }

        public void SelectStructuralOnly()
        {
            foreach (var cat in Categories) cat.IsSelected = cat.IsStructural;
        }

        public void ClearCategories()
        {
            foreach (var cat in Categories) cat.IsSelected = false;
        }

        public void ToggleSelectAllElements(bool? selectAll)
        {
            bool val = selectAll ?? true;
            foreach (var e in Elements)
            {
                e.IsSelected = val;
            }
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
