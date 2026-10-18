using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Autodesk.Revit.DB;
using JNNTool.Tools.CreateView.Handlers;
using JNNTool.Tools.CreateView.Models;
using JNNTool.Tools.CreateView.Services;

namespace JNNTool.Tools.CreateView.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute) : this(_ => execute(), null) { }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public class CreateViewViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private readonly CreateViewHandler _handler;
        private readonly Document _doc;

        private string _levelSearchText = string.Empty;
        public string LevelSearchText
        {
            get => _levelSearchText;
            set
            {
                if (SetField(ref _levelSearchText, value))
                {
                    FilterLevels();
                }
            }
        }

        private string _statusMessage = "Sẵn sàng tạo View.";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        private DuplicateHandling _duplicateHandling = DuplicateHandling.AddSuffixNumber;
        public DuplicateHandling DuplicateHandling
        {
            get => _duplicateHandling;
            set => SetField(ref _duplicateHandling, value);
        }

        private string _selectedGroupingParameterName = "Sub-Discipline";
        public string SelectedGroupingParameterName
        {
            get => _selectedGroupingParameterName;
            set => SetField(ref _selectedGroupingParameterName, value);
        }

        private LevelItem? _selectedLevelForPreview;
        public LevelItem? SelectedLevelForPreview
        {
            get => _selectedLevelForPreview;
            set
            {
                if (SetField(ref _selectedLevelForPreview, value) && value != null)
                {
                    UpdatePreviewLevel(value.Name);
                }
            }
        }

        private ViewTypeTabItem? _selectedTab;
        public ViewTypeTabItem? SelectedTab
        {
            get => _selectedTab;
            set => SetField(ref _selectedTab, value);
        }

        public ObservableCollection<LevelItem> Levels { get; } = new ObservableCollection<LevelItem>();
        public ObservableCollection<LevelItem> FilteredLevels { get; } = new ObservableCollection<LevelItem>();
        public ObservableCollection<ViewTypeTabItem> Tabs { get; } = new ObservableCollection<ViewTypeTabItem>();
        public ObservableCollection<ViewTypeItem> ViewTypes { get; } = new ObservableCollection<ViewTypeItem>();
        public ObservableCollection<ViewTemplateItem> ViewTemplates { get; } = new ObservableCollection<ViewTemplateItem>();
        public ObservableCollection<ScopeBoxItem> ScopeBoxes { get; } = new ObservableCollection<ScopeBoxItem>();
        public ObservableCollection<string> GroupingParameterNames { get; } = new ObservableCollection<string>();

        // Commands
        public ICommand SelectAllLevelsCommand { get; }
        public ICommand UnselectAllLevelsCommand { get; }
        public ICommand AddViewCommand { get; }
        public ICommand DuplicateViewCommand { get; }
        public ICommand RemoveViewCommand { get; }
        public ICommand ResetDefaultTabsCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand CreateCurrentTabCommand { get; }
        public ICommand CreateAllTabsCommand { get; }

        public CreateViewViewModel(CreateViewHandler handler, Document doc)
        {
            _handler = handler;
            _handler.ViewModel = this;
            _doc = doc;

            SelectAllLevelsCommand = new RelayCommand(SelectAllLevels);
            UnselectAllLevelsCommand = new RelayCommand(UnselectAllLevels);
            AddViewCommand = new RelayCommand(AddView);
            DuplicateViewCommand = new RelayCommand(DuplicateView);
            RemoveViewCommand = new RelayCommand(RemoveView);
            ResetDefaultTabsCommand = new RelayCommand(ResetDefaultTabs);
            SavePresetCommand = new RelayCommand(SavePreset);
            LoadPresetCommand = new RelayCommand(LoadPreset);
            CreateCurrentTabCommand = new RelayCommand(CreateCurrentTab);
            CreateAllTabsCommand = new RelayCommand(CreateAllTabs);

            LoadDocumentData();
            LoadDefaultTabs();
        }

        private void LoadDocumentData()
        {
            // 1. Levels
            var levels = new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new LevelItem { Id = l.Id, Name = l.Name, Elevation = l.Elevation, IsSelected = true })
                .ToList();

            foreach (var lvl in levels)
            {
                Levels.Add(lvl);
                FilteredLevels.Add(lvl);
            }

            SelectedLevelForPreview = Levels.FirstOrDefault();

            // 2. View Family Types
            ViewTypes.Add(new ViewTypeItem { Id = ElementId.InvalidElementId, Name = "<Auto: Tự tạo theo tên Tab>", FamilyName = "Auto" });
            var vfts = new FilteredElementCollector(_doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(vt => vt.ViewFamily == ViewFamily.StructuralPlan || vt.ViewFamily == ViewFamily.FloorPlan)
                .OrderBy(vt => vt.ViewFamily != ViewFamily.StructuralPlan)
                .ThenBy(vt => vt.Name)
                .Select(vt => new ViewTypeItem { Id = vt.Id, Name = vt.Name, FamilyName = vt.ViewFamily.ToString() })
                .ToList();

            foreach (var vft in vfts)
                ViewTypes.Add(vft);

            // 3. View Templates
            ViewTemplates.Add(new ViewTemplateItem { Id = ElementId.InvalidElementId, Name = "<None>" });
            var templates = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .Select(v => new ViewTemplateItem { Id = v.Id, Name = v.Name })
                .ToList();

            foreach (var t in templates)
                ViewTemplates.Add(t);

            // 4. Scope Boxes
            ScopeBoxes.Add(new ScopeBoxItem { Id = ElementId.InvalidElementId, Name = "<None>" });
            var scopeBoxes = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .OrderBy(sb => sb.Name)
                .Select(sb => new ScopeBoxItem { Id = sb.Id, Name = sb.Name })
                .ToList();

            foreach (var sb in scopeBoxes)
                ScopeBoxes.Add(sb);

            // 5. Grouping Parameters
            GroupingParameterNames.Add("Sub-Discipline");
            GroupingParameterNames.Add("Views $ Group");
            GroupingParameterNames.Add("View Group");
            GroupingParameterNames.Add("Phân loại");
            GroupingParameterNames.Add("Thư mục View");
            GroupingParameterNames.Add("None");
            SelectedGroupingParameterName = "Sub-Discipline";
        }

        public LevelItem? FindFoundationLevel()
        {
            if (Levels.Count == 0) return null;

            string[] keywords = new[] { "MONG", "MÓNG", "FOUNDATION", "FND", "COC", "CỌC", "PILE", "BASE", "DAY MONG", "ĐÁY MÓNG", "HAM", "HẦM", "TRET", "TRỆT", "GROUND" };
            foreach (var kw in keywords)
            {
                var match = Levels.FirstOrDefault(l => l.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
                if (match != null) return match;
            }

            return Levels.OrderBy(l => l.Elevation).FirstOrDefault();
        }

        public void LoadDefaultTabs()
        {
            Tabs.Clear();
            var preset = CreateViewPresetService.GetDefaultAthenaPreset();
            var foundationLevel = FindFoundationLevel() ?? Levels.FirstOrDefault();
            string sampleLevel = SelectedLevelForPreview?.Name ?? "TANG 1";

            foreach (var tDto in preset.Tabs)
            {
                var tabItem = new ViewTypeTabItem
                {
                    IsEnabled = tDto.IsEnabled,
                    TabId = tDto.TabId,
                    Title = tDto.Title,
                    ViewFamilyTypeName = tDto.ViewFamilyTypeName,
                    BrowserSubDiscipline = tDto.BrowserSubDiscipline,
                    DefaultTemplateHint = tDto.DefaultTemplateName,
                    AutoCreateViewType = true,
                    SelectedScopeBox = ScopeBoxes.FirstOrDefault(),
                    LevelScope = tDto.LevelScope == "SingleLevel" ? LevelScopeMode.SingleLevel : LevelScopeMode.AllSelectedLevels,
                    TargetLevel = foundationLevel
                };

                // Match ViewFamilyType in project if already present
                var matchedVft = ViewTypes.FirstOrDefault(vt => 
                    vt.Id != ElementId.InvalidElementId && vt.Name.Equals(tDto.ViewFamilyTypeName, StringComparison.OrdinalIgnoreCase))
                    ?? ViewTypes.FirstOrDefault(); // Fallback to Auto
                tabItem.SelectedViewType = matchedVft;

                // Match ViewTemplate
                var matchedTemplate = ViewTemplates.FirstOrDefault(tmpl => 
                    !tmpl.IsNone && tmpl.Name.IndexOf(tDto.DefaultTemplateName, StringComparison.OrdinalIgnoreCase) >= 0)
                    ?? ViewTemplates.FirstOrDefault();
                tabItem.SelectedViewTemplate = matchedTemplate;

                // Populate views inside this tab
                foreach (var vDto in tDto.Views)
                {
                    var rule = new ViewRuleItem
                    {
                        IsSelected = vDto.IsSelected,
                        ViewNamePattern = vDto.ViewNamePattern,
                        BrowserSubDiscipline = tabItem.BrowserSubDiscipline,
                        SelectedViewType = tabItem.SelectedViewType,
                        SelectedScopeBox = tabItem.SelectedScopeBox,
                        SampleLevelName = sampleLevel
                    };

                    // Specific view template if specified, otherwise tab default
                    var vTemplate = !string.IsNullOrWhiteSpace(vDto.TemplateName)
                        ? ViewTemplates.FirstOrDefault(tmpl => !tmpl.IsNone && tmpl.Name.IndexOf(vDto.TemplateName, StringComparison.OrdinalIgnoreCase) >= 0)
                        : null;

                    rule.SelectedViewTemplate = vTemplate ?? tabItem.SelectedViewTemplate;
                    tabItem.Views.Add(rule);
                }

                if (tabItem.Views.Count > 0)
                    tabItem.SelectedView = tabItem.Views[0];

                Tabs.Add(tabItem);
            }

            if (Tabs.Count > 0)
                SelectedTab = Tabs[0];
        }

        private void UpdatePreviewLevel(string levelName)
        {
            foreach (var tab in Tabs)
            {
                foreach (var v in tab.Views)
                {
                    v.SampleLevelName = levelName;
                }
            }
        }

        private void FilterLevels()
        {
            FilteredLevels.Clear();
            var matched = string.IsNullOrWhiteSpace(LevelSearchText)
                ? Levels
                : Levels.Where(l => l.Name.IndexOf(LevelSearchText, StringComparison.OrdinalIgnoreCase) >= 0);

            foreach (var l in matched)
                FilteredLevels.Add(l);
        }

        private void SelectAllLevels()
        {
            foreach (var l in FilteredLevels) l.IsSelected = true;
        }

        private void UnselectAllLevels()
        {
            foreach (var l in FilteredLevels) l.IsSelected = false;
        }

        private void AddView()
        {
            if (SelectedTab == null) return;
            string sampleLevel = SelectedLevelForPreview?.Name ?? "TANG 1";
            SelectedTab.AddView($"XR-{SelectedTab.Title} {{Level}}", sampleLevel);
        }

        private void DuplicateView()
        {
            if (SelectedTab == null) return;
            string sampleLevel = SelectedLevelForPreview?.Name ?? "TANG 1";
            SelectedTab.DuplicateSelected(sampleLevel);
        }

        private void RemoveView()
        {
            if (SelectedTab == null) return;
            SelectedTab.RemoveSelected();
        }

        private void ResetDefaultTabs()
        {
            LoadDefaultTabs();
            StatusMessage = "Đã khôi phục 5 Tab Type View chuẩn ATHENA.";
        }

        private void SavePreset()
        {
            var preset = new CreateViewPreset
            {
                PresetName = "User 5 Tabs Preset",
                Tabs = Tabs.Select(t => new TabPresetDto
                {
                    IsEnabled = t.IsEnabled,
                    TabId = t.TabId,
                    Title = t.Title,
                    ViewFamilyTypeName = t.ViewFamilyTypeName,
                    BrowserSubDiscipline = t.BrowserSubDiscipline,
                    DefaultTemplateName = t.SelectedViewTemplate?.Name ?? "",
                    DefaultScopeBoxName = t.SelectedScopeBox?.Name ?? "",
                    LevelScope = t.LevelScope.ToString(),
                    TargetLevelKeyword = t.TargetLevel?.Name ?? "MONG",
                    Views = t.Views.Select(v => new ViewRulePresetDto
                    {
                        IsSelected = v.IsSelected,
                        ViewNamePattern = v.ViewNamePattern,
                        TemplateName = v.SelectedViewTemplate?.Name ?? "",
                        ScopeBoxName = v.SelectedScopeBox?.Name ?? ""
                    }).ToList()
                }).ToList()
            };

            CreateViewPresetService.SavePresets(new List<CreateViewPreset> { preset });
            StatusMessage = "Đã lưu bộ cấu hình 5 Tabs vào Preset thành công.";
        }

        private void LoadPreset()
        {
            var presets = CreateViewPresetService.LoadPresets();
            var preset = presets.FirstOrDefault();
            if (preset == null || preset.Tabs.Count == 0)
            {
                StatusMessage = "Chưa có preset nào được lưu.";
                return;
            }

            Tabs.Clear();
            string sampleLevel = SelectedLevelForPreview?.Name ?? "TANG 1";
            var foundationLevel = FindFoundationLevel() ?? Levels.FirstOrDefault();

            foreach (var tDto in preset.Tabs)
            {
                var tabItem = new ViewTypeTabItem
                {
                    IsEnabled = tDto.IsEnabled,
                    TabId = tDto.TabId,
                    Title = tDto.Title,
                    ViewFamilyTypeName = tDto.ViewFamilyTypeName,
                    BrowserSubDiscipline = tDto.BrowserSubDiscipline,
                    AutoCreateViewType = true,
                    LevelScope = tDto.LevelScope == "SingleLevel" ? LevelScopeMode.SingleLevel : LevelScopeMode.AllSelectedLevels,
                    TargetLevel = foundationLevel
                };

                tabItem.SelectedViewType = ViewTypes.FirstOrDefault(vt => vt.Name.Equals(tDto.ViewFamilyTypeName, StringComparison.OrdinalIgnoreCase)) ?? ViewTypes.FirstOrDefault();
                tabItem.SelectedViewTemplate = ViewTemplates.FirstOrDefault(tmpl => tmpl.Name.Equals(tDto.DefaultTemplateName, StringComparison.OrdinalIgnoreCase)) ?? ViewTemplates.FirstOrDefault();
                tabItem.SelectedScopeBox = ScopeBoxes.FirstOrDefault(sb => sb.Name.Equals(tDto.DefaultScopeBoxName, StringComparison.OrdinalIgnoreCase)) ?? ScopeBoxes.FirstOrDefault();

                foreach (var vDto in tDto.Views)
                {
                    var rule = new ViewRuleItem
                    {
                        IsSelected = vDto.IsSelected,
                        ViewNamePattern = vDto.ViewNamePattern,
                        BrowserSubDiscipline = tabItem.BrowserSubDiscipline,
                        SelectedViewType = tabItem.SelectedViewType,
                        SelectedScopeBox = tabItem.SelectedScopeBox,
                        SampleLevelName = sampleLevel
                    };
                    rule.SelectedViewTemplate = ViewTemplates.FirstOrDefault(tmpl => tmpl.Name.Equals(vDto.TemplateName, StringComparison.OrdinalIgnoreCase)) ?? tabItem.SelectedViewTemplate;
                    tabItem.Views.Add(rule);
                }

                if (tabItem.Views.Count > 0)
                    tabItem.SelectedView = tabItem.Views[0];

                Tabs.Add(tabItem);
            }

            if (Tabs.Count > 0)
                SelectedTab = Tabs[0];

            StatusMessage = $"Đã tải preset: {preset.PresetName} ({Tabs.Count} Tabs).";
        }

        private void CreateCurrentTab()
        {
            int selectedLevelCount = Levels.Count(l => l.IsSelected);
            if (selectedLevelCount == 0)
            {
                StatusMessage = "Vui lòng chọn ít nhất 1 Level.";
                return;
            }

            if (SelectedTab == null || SelectedTab.Views.Count(v => v.IsSelected) == 0)
            {
                StatusMessage = "Tab hiện hành không có View nào được chọn.";
                return;
            }

            StatusMessage = $"Đang tạo các View cho Tab [{SelectedTab.Title}]...";
            _handler.ProcessAllTabs = false;
            _handler.Raise();
        }

        private void CreateAllTabs()
        {
            int selectedLevelCount = Levels.Count(l => l.IsSelected);
            if (selectedLevelCount == 0)
            {
                StatusMessage = "Vui lòng chọn ít nhất 1 Level.";
                return;
            }

            var enabledTabs = Tabs.Where(t => t.IsEnabled).ToList();
            int totalViewsInTabs = enabledTabs.Sum(t => t.Views.Count(v => v.IsSelected));

            if (totalViewsInTabs == 0)
            {
                StatusMessage = "Không có View nào được chọn trong các Tab được bật.";
                return;
            }

            StatusMessage = $"Đang tạo {selectedLevelCount * totalViewsInTabs} view cho {enabledTabs.Count} Tabs...";
            _handler.ProcessAllTabs = true;
            _handler.Raise();
        }
    }
}
