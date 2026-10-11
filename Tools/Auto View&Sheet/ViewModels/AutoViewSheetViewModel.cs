using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using Autodesk.Revit.DB;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RincoNhan.Tools.AutoViewSheet.Models;

namespace RincoNhan.Tools.AutoViewSheet.ViewModels
{
    public partial class AutoViewSheetViewModel : ObservableObject
    {
        private readonly AutoViewSheetHandler _handler;
        
        // Settings Options
        public ObservableCollection<TitleBlockItem> TitleBlocks { get; set; } = new ObservableCollection<TitleBlockItem>();
        [ObservableProperty] private TitleBlockItem _selectedTitleBlock;

        public ObservableCollection<string> SheetSeriesOptions { get; set; } = new ObservableCollection<string>();
        [ObservableProperty] private string _selectedSheetSeries;

        public ObservableCollection<string> SheetNamingTypes { get; set; } = new ObservableCollection<string> 
        { 
            "MẶT BẰNG KẾT CẤU", 
            "GA PLAN", 
            "LP PLAN", 
            "NORMAL" 
        };
        [ObservableProperty] private string _selectedSheetNamingType = "MẶT BẰNG KẾT CẤU";

        partial void OnSelectedSheetSeriesChanged(string value)
        {
            UpdateAllRows();
        }

        partial void OnSelectedSheetNamingTypeChanged(string value)
        {
            if (AutoRows.Any() || PartRows.Any())
            {
                var response = MessageBox.Show("Do you want to regenerate all rows to apply the new Sheet Naming Type?", 
                    "Regenerate Rows", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (response == MessageBoxResult.Yes)
                {
                    AddAllLevels();
                    AddAllLevelsForPart();
                }
            }
        }

        public ObservableCollection<ViewportTitleItem> ViewportTitleTypes { get; set; } = new ObservableCollection<ViewportTitleItem>();
        [ObservableProperty] private ViewportTitleItem _selectedViewportTitle;

        [ObservableProperty] private bool _stackViews = true;

        // Data Row Options
        public List<LevelItem> ProjectLevels { get; set; } = new List<LevelItem>();
        public ObservableCollection<ViewTypeItem> ViewTypes { get; set; } = new ObservableCollection<ViewTypeItem>();
        public ObservableCollection<ViewTemplateItem> ViewTemplates { get; set; } = new ObservableCollection<ViewTemplateItem>();
        public ObservableCollection<ScopeBoxItem> ScopeBoxOptions { get; set; } = new ObservableCollection<ScopeBoxItem>();
        [ObservableProperty] private ScopeBoxItem _selectedGlobalScopeBox;

        public ObservableCollection<AutoViewSheetRow> AutoRows { get; set; } = new ObservableCollection<AutoViewSheetRow>();
        public ICollectionView AutoRowsView { get; private set; }

        public ObservableCollection<AutoViewSheetRow> PartRows { get; set; } = new ObservableCollection<AutoViewSheetRow>();
        public ICollectionView PartRowsView { get; private set; }

        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    AutoRowsView.Refresh();
                }
            }
        }

        private string _partSearchText;
        public string PartSearchText
        {
            get => _partSearchText;
            set
            {
                if (SetProperty(ref _partSearchText, value))
                {
                    PartRowsView.Refresh();
                }
            }
        }

        [ObservableProperty] private string _statusMessage = "Ready.";

        public AutoViewSheetViewModel(AutoViewSheetHandler handler, Document doc)
        {
            _handler = handler;
            _handler.ViewModel = this;

            AutoRowsView = CollectionViewSource.GetDefaultView(AutoRows);
            AutoRowsView.GroupDescriptions.Add(new PropertyGroupDescription("SheetNumberAndName"));
            AutoRowsView.Filter = FilterRows;
            AutoRowsView.SortDescriptions.Add(new SortDescription("SheetNumber", ListSortDirection.Ascending));

            PartRowsView = CollectionViewSource.GetDefaultView(PartRows);
            PartRowsView.Filter = FilterPartRows;
            PartRowsView.GroupDescriptions.Add(new PropertyGroupDescription("SelectedLevel.Name"));
            PartRowsView.GroupDescriptions.Add(new PropertyGroupDescription("GroupId"));
            PartRowsView.SortDescriptions.Add(new SortDescription("SelectedLevel.Elevation", ListSortDirection.Ascending));
            PartRowsView.SortDescriptions.Add(new SortDescription("SheetNumber", ListSortDirection.Ascending));

            LoadData(doc);
        }

        private bool FilterRows(object obj)
        {
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            if (obj is AutoViewSheetRow row)
            {
                return (row.SheetName != null && row.SheetName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (row.SheetNumber != null && row.SheetNumber.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (row.SelectedLevel != null && row.SelectedLevel.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        private bool FilterPartRows(object obj)
        {
            if (string.IsNullOrWhiteSpace(PartSearchText)) return true;
            if (obj is AutoViewSheetRow row)
            {
                return (row.SheetName != null && row.SheetName.IndexOf(PartSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (row.SheetNumber != null && row.SheetNumber.IndexOf(PartSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (row.SelectedLevel != null && row.SelectedLevel.Name.IndexOf(PartSearchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                       (row.ZoneName != null && row.ZoneName.IndexOf(PartSearchText, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            return false;
        }

        private bool _sortAscending = true;
        [RelayCommand]
        private void ToggleSort()
        {
            _sortAscending = !_sortAscending;
            AutoRowsView.SortDescriptions.Clear();
            AutoRowsView.SortDescriptions.Add(new SortDescription("SheetNumber", _sortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
        }

        private bool _partSortAscending = true;
        [RelayCommand]
        private void TogglePartSort()
        {
            _partSortAscending = !_partSortAscending;
            PartRowsView.SortDescriptions.Clear();
            PartRowsView.SortDescriptions.Add(new SortDescription("SelectedLevel.Elevation", _partSortAscending ? ListSortDirection.Ascending : ListSortDirection.Descending));
            PartRowsView.SortDescriptions.Add(new SortDescription("SheetNumber", ListSortDirection.Ascending));
        }

        [RelayCommand]
        private void SelectAll(string action)
        {
            bool check = action == "True";
            foreach (var row in AutoRows)
            {
                row.IsGroupSelected = check;
            }
        }

        [RelayCommand]
        private void SelectAllPart(string action)
        {
            bool check = action == "True";
            // Group the part rows by their Level + GroupId since checking the first row automatically checks the rest via callbacks
            var grouped = PartRows.GroupBy(r => new { LevelId = r.SelectedLevel?.Id, r.GroupId });
            foreach (var group in grouped)
            {
                var firstRow = group.First();
                firstRow.IsGroupSelected = check;
            }
        }

        private void LoadData(Document doc)
        {
            // Levels
            ProjectLevels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new LevelItem { Name = l.Name, Id = l.Id, Elevation = l.Elevation })
                .ToList();

            // View Types
            var viewTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(vt => vt.ViewFamily == ViewFamily.StructuralPlan)
                .OrderBy(vt => vt.Name)
                .ToList();
            foreach (var vt in viewTypes) ViewTypes.Add(new ViewTypeItem { Name = vt.Name, Id = vt.Id });

            // Templates
            ViewTemplates.Add(new ViewTemplateItem { Name = "<None>", Id = ElementId.InvalidElementId });
            var templates = new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .ToList();
            foreach (var vt in templates) ViewTemplates.Add(new ViewTemplateItem { Name = vt.Name, Id = vt.Id });

            // Scope Boxes
            ScopeBoxOptions.Add(new ScopeBoxItem { Name = "<None>", Id = ElementId.InvalidElementId });
            var scopeBoxes = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_VolumeOfInterest)
                .WhereElementIsNotElementType()
                .OrderBy(sb => sb.Name)
                .ToList();
            foreach (var sb in scopeBoxes) ScopeBoxOptions.Add(new ScopeBoxItem { Name = sb.Name, Id = sb.Id });
            SelectedGlobalScopeBox = ScopeBoxOptions.FirstOrDefault();

            // TitleBlocks
            var titleBlocks = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .WhereElementIsElementType()
                .Cast<FamilySymbol>()
                .OrderBy(tb => tb.Name)
                .ToList();
            foreach (var tb in titleBlocks) TitleBlocks.Add(new TitleBlockItem { Name = $"{tb.FamilyName} - {tb.Name}", Id = tb.Id });
            SelectedTitleBlock = TitleBlocks.FirstOrDefault();

            // Sheet Series
            LoadSheetSeries(doc);

            // Viewport Titles
            ViewportTitleTypes.Add(new ViewportTitleItem { Name = "<None>", Id = ElementId.InvalidElementId });
            var allViewports = new FilteredElementCollector(doc).OfClass(typeof(Viewport)).Cast<Viewport>().ToList();
            if (allViewports.Any())
            {
                var validTypeIds = allViewports.First().GetValidTypes();
                foreach (var typeId in validTypeIds)
                {
                    var vpType = doc.GetElement(typeId) as ElementType;
                    if (vpType != null) ViewportTitleTypes.Add(new ViewportTitleItem { Name = vpType.Name, Id = vpType.Id });
                }
            }
            SelectedViewportTitle = ViewportTitleTypes.FirstOrDefault(vt => vt.Name.Equals("RINCO_Title_GA", StringComparison.OrdinalIgnoreCase)) ?? ViewportTitleTypes.FirstOrDefault();

            // Auto-populate all levels
            AddAllLevels();
            AddAllLevelsForPart();
        }

        private void LoadSheetSeries(Document doc)
        {
            SheetSeriesOptions.Clear();
            var seriesValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var allSheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .ToList();

            foreach (var sheet in allSheets)
            {
                var param = sheet.LookupParameter("RINCO_TB_SHEET SERIES");
                if (param != null && param.HasValue && !string.IsNullOrWhiteSpace(param.AsString()))
                {
                    seriesValues.Add(param.AsString());
                }
            }

            seriesValues.Add("S.03 - MẶT BẰNG KẾT CẤU");
            if (seriesValues.Count == 1)
            {
                seriesValues.Add("S0000 SERIES - GENERAL");
                seriesValues.Add("S1000 SERIES - SITE RETENTION & FOUNDATION PLANS");
                seriesValues.Add("S2000 SERIES - GENERAL ARRANGEMENT PLANS");
                seriesValues.Add("S3000 SERIES - WALL ELEVATIONS");
                seriesValues.Add("S4000 SERIES - CONCRETE DETAILS");
                seriesValues.Add("S5000 SERIES - PEO & PT PLANS");
            }

            foreach (var s in seriesValues.OrderBy(x => x)) SheetSeriesOptions.Add(s);

            SelectedSheetSeries = SheetSeriesOptions.FirstOrDefault(s => s.Contains("MẶT BẰNG KẾT CẤU")) 
                               ?? SheetSeriesOptions.FirstOrDefault(s => s.Contains("GENERAL ARRANGEMENT")) 
                               ?? SheetSeriesOptions.FirstOrDefault();
        }

        private string GenerateSheetName(LevelItem level)
        {
            if (level == null) return "";
            string rawName = level.Name.Trim();
            
            if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                string upper = rawName.ToUpper();
                string vnLevelName;
                
                var mTangT = Regex.Match(upper, @"TẦNG\s*T\s*(\d+)");
                var mTang = Regex.Match(upper, @"TẦNG\s*(\d+)");
                var mT = Regex.Match(upper, @"(?:^|\b)T[\.\-_\s]*(\d+)");
                var mLevel = Regex.Match(upper, @"(?:LEVEL|LVL|L)\s*(\d+)");
                var mF = Regex.Match(upper, @"(?:^|\D)(\d+)\s*(?:F|FL)");
                var mFL = Regex.Match(upper, @"FL\s*(\d+)");
                var mHam = Regex.Match(upper, @"(?:BASEMENT|HẦM|TẦNG HẦM|B)\s*(\d*)");
                
                if (mTangT.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mTangT.Groups[1].Value)}";
                }
                else if (mTang.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mTang.Groups[1].Value)}";
                }
                else if (mT.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mT.Groups[1].Value)}";
                }
                else if (mLevel.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mLevel.Groups[1].Value)}";
                }
                else if (mF.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mF.Groups[1].Value)}";
                }
                else if (mFL.Success)
                {
                    vnLevelName = $"TẦNG {int.Parse(mFL.Groups[1].Value)}";
                }
                else if (upper.Contains("GROUND") || upper.Contains("TRỆT"))
                {
                    vnLevelName = "TẦNG 1";
                }
                else if (upper.Contains("ROOF") || upper.Contains("MÁI"))
                {
                    vnLevelName = "TẦNG MÁI";
                }
                else if (upper.Contains("TUM"))
                {
                    vnLevelName = "TẦNG TUM";
                }
                else if (mHam.Success && !string.IsNullOrEmpty(mHam.Groups[1].Value))
                {
                    vnLevelName = $"TẦNG HẦM {int.Parse(mHam.Groups[1].Value)}";
                }
                else if (upper.Contains("BASEMENT") || upper.Contains("HẦM"))
                {
                    vnLevelName = "TẦNG HẦM";
                }
                else
                {
                    var mOnlyT = Regex.Match(upper, @"^T[\.\-_\s]*(\d+)");
                    if (mOnlyT.Success)
                    {
                        vnLevelName = $"TẦNG {int.Parse(mOnlyT.Groups[1].Value)}";
                    }
                    else
                    {
                        vnLevelName = upper.StartsWith("TẦNG") ? upper : $"TẦNG {upper}";
                    }
                }

                return $"MẶT BẰNG KẾT CẤU {vnLevelName}";
            }
            
            string nameSuffix = "GENERAL ARRANGEMENT PLAN";
            if (SelectedSheetNamingType == "LP PLAN") nameSuffix = "LOADING PLAN";
            else if (SelectedSheetNamingType == "NORMAL") nameSuffix = "PLAN";
            return $"{level.Name} {nameSuffix}";
        }

        private void AddGroupedViewRow(LevelItem level, string suffix, string viewTypeKey, string templateKey, string groupId, string baseSheetNum, int offset)
        {
            // Fuzzy match view type based on suffix
            var matchedViewType = ViewTypes.FirstOrDefault(vt => vt.Name.Equals(viewTypeKey, StringComparison.OrdinalIgnoreCase));
            if (matchedViewType == null)
            {
                matchedViewType = ViewTypes.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.EndsWith("OVER", StringComparison.OrdinalIgnoreCase) || 
                    (vt.Name.IndexOf("OVER", StringComparison.OrdinalIgnoreCase) >= 0 && vt.Name.IndexOf("OVERLAY", StringComparison.OrdinalIgnoreCase) < 0));
            }
            if (matchedViewType == null) matchedViewType = ViewTypes.FirstOrDefault();
            
            // Fuzzy match template
            var matchedTemplate = ViewTemplates.FirstOrDefault(vt => vt.Name.Equals(templateKey, StringComparison.OrdinalIgnoreCase));
            if (matchedTemplate == null)
            {
                matchedTemplate = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.Contains("OVER 1/100"));
            }
            if (matchedTemplate == null) matchedTemplate = ViewTemplates.FirstOrDefault();

            var row = new AutoViewSheetRow
            {
                SelectedLevel = level,
                Suffix = suffix,
                SelectedViewType = matchedViewType,
                SelectedViewTemplate = matchedTemplate,
                SelectedScopeBox = ScopeBoxOptions.FirstOrDefault(),
                SheetNumber = ComputeSheetNumber(baseSheetNum, offset),
                SheetName = GenerateSheetName(level),
                GroupId = groupId
            };

            row.GroupSelectionChangedCallback = (changedRow, isSelected) =>
            {
                foreach (var r in AutoRows.Where(x => x.GroupId == changedRow.GroupId && x != changedRow))
                {
                    r.IsSelected = isSelected;
                }
            };

            row.LevelChangedCallback = (changedRow, oldLevel, newLevel) =>
            {
                if (newLevel != null)
                {
                    changedRow.SheetName = GenerateSheetName(newLevel);
                }
                foreach (var r in AutoRows.Where(x => x.GroupId == changedRow.GroupId && x != changedRow))
                {
                    r.SelectedLevel = newLevel;
                    if (newLevel != null)
                    {
                        r.SheetName = changedRow.SheetName;
                    }
                }
            };

            row.SheetNumberChangedCallback = (changedRow, newNum) =>
            {
                foreach (var r in AutoRows.Where(x => x.GroupId == changedRow.GroupId && x != changedRow))
                {
                    r.SheetNumber = newNum;
                }
            };

            row.SheetNameChangedCallback = (changedRow, newName) =>
            {
                foreach (var r in AutoRows.Where(x => x.GroupId == changedRow.GroupId && x != changedRow))
                {
                    r.SheetName = newName;
                }
            };

            row.ScopeBoxChangedCallback = (changedRow, newBox) =>
            {
                foreach (var r in AutoRows.Where(x => x != changedRow))
                {
                    r.SelectedScopeBox = newBox;
                }
            };

            AutoRows.Add(row);
        }

        private string ComputeSheetNumber(string baseNum, int offset)
        {
            if (string.IsNullOrEmpty(baseNum)) return "";
            if (offset == 0) return baseNum;

            var match = Regex.Match(baseNum, @"^(.*?)(\d+)([^\d]*)$");
            if (match.Success)
            {
                string prefix = match.Groups[1].Value;
                int num = int.Parse(match.Groups[2].Value);
                string suffix = match.Groups[3].Value;
                int padding = match.Groups[2].Value.Length;
                return $"{prefix}{(num + offset).ToString("D" + padding)}{suffix}";
            }
            return $"{baseNum}.{offset}";
        }

        private void UpdateAllRows()
        {
            if (AutoRows == null || !AutoRows.Any()) return;
            
            var levelGroups = AutoRows.GroupBy(r => r.GroupId).ToList();
            HashSet<string> usedBaseNums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string overTemplateName = "RINCO - GA - OVER 1/100";
            string suffixAddition = "";

            if (SelectedSheetNamingType == "LP PLAN") 
            {
                overTemplateName = "RINCO - LP - OVER 1/100";
                suffixAddition = " - LOADING PLAN";
            }
            else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
            }

            foreach (var group in levelGroups)
            {
                var firstRow = group.First();
                var level = firstRow.SelectedLevel;
                if (level == null) continue;
                
                string baseSheetNum = GenerateBaseSheetNumber(level, ProjectLevels);
                
                string uniqueBaseNum = baseSheetNum;
                int suffixCount = 1;
                while (usedBaseNums.Contains(uniqueBaseNum))
                {
                    uniqueBaseNum = $"{baseSheetNum}.{suffixCount}";
                    suffixCount++;
                }
                usedBaseNums.Add(uniqueBaseNum);

                firstRow.SheetNumber = uniqueBaseNum; // updates the whole group via callback
                firstRow.SheetName = GenerateSheetName(level); // updates the whole group via callback

                foreach (var row in AutoRows.Where(r => r.GroupId == firstRow.GroupId))
                {
                    string cleanSuffix = row.Suffix?.Replace(" - LOADING PLAN", "");
                    row.Suffix = cleanSuffix + suffixAddition;
                    var tmpl = ViewTemplates.FirstOrDefault(vt => vt.Name.Equals(overTemplateName, StringComparison.OrdinalIgnoreCase));
                    if (tmpl != null) row.SelectedViewTemplate = tmpl;
                }
            }
        }

        private string GenerateBaseSheetNumber(LevelItem level, List<LevelItem> allLevels)
        {
            string name = level.Name.ToUpper().Trim();

            if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                string prefix = "S.03.";

                // 1. Tầng hầm (Basement)
                if (name.Contains("BASEMENT") || name.Contains("HẦM"))
                {
                    var mH = Regex.Match(name, @"(\d+)");
                    if (mH.Success)
                    {
                        return $"{prefix}H{mH.Groups[1].Value}";
                    }
                    var basements = allLevels.Where(l => l.Name.ToUpper().Contains("BASEMENT") || l.Name.ToUpper().Contains("HẦM")).OrderBy(l => l.Elevation).ToList();
                    int bIndex = basements.IndexOf(level);
                    int count = basements.Count;
                    return $"{prefix}H{count - bIndex}";
                }

                // 2. Tầng trệt / Ground
                if (name.Contains("GROUND") || name.Contains("TRỆT"))
                {
                    return $"{prefix}01";
                }

                // 3. Tầng X, Level X, XF, TX
                var match = Regex.Match(name, @"(?:LEVEL|TẦNG|LVL|L|T)\s*(\d+)");
                if (!match.Success)
                {
                    match = Regex.Match(name, @"(?:^|\D)(\d+)\s*(?:F|FL)?$");
                }
                if (!match.Success)
                {
                    match = Regex.Match(name, @"FL\s*(\d+)");
                }

                if (match.Success && int.TryParse(match.Groups[1].Value, out int levelNum))
                {
                    return $"{prefix}{levelNum:D2}";
                }

                // 4. Các tầng khác (Mái, Tum...) -> Đánh số tiếp sau maxLevelNum
                int maxLevelNum = 0;
                foreach (var l in allLevels)
                {
                    var m = Regex.Match(l.Name.ToUpper(), @"(?:LEVEL|TẦNG|LVL|L|T)\s*(\d+)");
                    if (!m.Success) m = Regex.Match(l.Name.ToUpper(), @"(?:^|\D)(\d+)\s*(?:F|FL)?$");
                    if (!m.Success) m = Regex.Match(l.Name.ToUpper(), @"FL\s*(\d+)");
                    if (m.Success && int.TryParse(m.Groups[1].Value, out int num))
                    {
                        if (num > maxLevelNum) maxLevelNum = num;
                    }
                }

                var otherLevels = allLevels
                    .Where(l => !l.Name.ToUpper().Contains("BASEMENT") 
                             && !l.Name.ToUpper().Contains("HẦM") 
                             && !l.Name.ToUpper().Contains("GROUND") 
                             && !l.Name.ToUpper().Contains("TRỆT") 
                             && !Regex.IsMatch(l.Name.ToUpper(), @"(?:LEVEL|TẦNG|LVL|L|T)\s*\d+")
                             && !Regex.IsMatch(l.Name.ToUpper(), @"(?:^|\D)\d+\s*(?:F|FL)?$")
                             && !Regex.IsMatch(l.Name.ToUpper(), @"FL\s*\d+"))
                    .OrderBy(l => l.Elevation).ToList();
                int otherIndex = otherLevels.IndexOf(level);
                if (otherIndex < 0) otherIndex = 0;

                return $"{prefix}{(maxLevelNum + 1 + otherIndex):D2}";
            }

            string basePrefix = "S4"; // fallback
            
            if (!string.IsNullOrEmpty(SelectedSheetSeries))
            {
                var seriesMatch = Regex.Match(SelectedSheetSeries, @"^([A-Za-z0-9]{2})");
                if (seriesMatch.Success) basePrefix = seriesMatch.Groups[1].Value;
            }

            // BASEMENT levels → basePrefix + 099 (multiple basements count down)
            if (name.Contains("BASEMENT"))
            {
                var basements = allLevels.Where(l => l.Name.ToUpper().Contains("BASEMENT")).OrderBy(l => l.Elevation).ToList();
                int basementCount = basements.Count;
                int index = basements.IndexOf(level);
                return $"{basePrefix}{99 - (basementCount - 1 - index):D3}";
            }

            // GROUND level → basePrefix + 100
            if (name.Contains("GROUND")) return $"{basePrefix}100";

            // LEVEL X → basePrefix + (100 + X)
            var matchLvl = Regex.Match(name, @"LEVEL\s*(\d+)");
            if (matchLvl.Success)
            {
                int levelNum = int.Parse(matchLvl.Groups[1].Value);
                return $"{basePrefix}{100 + levelNum}";
            }

            // Other levels
            int maxLvl = 0;
            foreach (var l in allLevels)
            {
                var m = Regex.Match(l.Name.ToUpper(), @"LEVEL\s*(\d+)");
                if (m.Success)
                {
                    int num = int.Parse(m.Groups[1].Value);
                    if (num > maxLvl) maxLvl = num;
                }
            }

            var otherLvls = allLevels
                .Where(l => !l.Name.ToUpper().Contains("BASEMENT") && !l.Name.ToUpper().Contains("GROUND") && !Regex.IsMatch(l.Name.ToUpper(), @"LEVEL\s*\d+"))
                .OrderBy(l => l.Elevation).ToList();
            int oIndex = otherLvls.IndexOf(level);

            return $"{basePrefix}{100 + maxLvl + 1 + oIndex}";
        }

        [RelayCommand]
        private void AddRow()
        {
            if (!ProjectLevels.Any()) return;
            var level = ProjectLevels.First();
            string groupId = Guid.NewGuid().ToString();
            string baseSheet = GenerateBaseSheetNumber(level, ProjectLevels);

            string overTemplateName = "RINCO - GA - OVER 1/100";
            string suffixAddition = "";

            if (SelectedSheetNamingType == "LP PLAN") 
            {
                overTemplateName = "RINCO - LP - OVER 1/100";
                suffixAddition = " - LOADING PLAN";
            }
            else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
            }

            AddGroupedViewRow(level, "- OVER" + suffixAddition, "OVER_PLANS", overTemplateName, groupId, baseSheet, 0);
        }

        [RelayCommand]
        private void AddAllLevels()
        {
            AutoRows.Clear();
            var levels = ProjectLevels;
            HashSet<string> usedBaseNums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string overTemplateName = "RINCO - GA - OVER 1/100";
            string suffixAddition = "";

            if (SelectedSheetNamingType == "LP PLAN") 
            {
                overTemplateName = "RINCO - LP - OVER 1/100";
                suffixAddition = " - LOADING PLAN";
            }
            else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
            }

            foreach (var level in levels)
            {
                string baseSheetNum = GenerateBaseSheetNumber(level, levels);
                
                string uniqueBaseNum = baseSheetNum;
                int suffixCount = 1;
                while (usedBaseNums.Contains(uniqueBaseNum))
                {
                    uniqueBaseNum = $"{baseSheetNum}.{suffixCount}";
                    suffixCount++;
                }
                usedBaseNums.Add(uniqueBaseNum);

                string groupId = level.Id.ToString();

                AddGroupedViewRow(level, "- OVER" + suffixAddition, "OVER_PLANS", overTemplateName, groupId, uniqueBaseNum, 0);
            }
        }

        private void AddAllLevelsForPart()
        {
            PartRows.Clear();
            var levels = ProjectLevels;
            HashSet<string> usedBaseNums = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string overTemplateName = "RINCO - GA - OVER 1/100";
            string suffixAddition = "";

            if (SelectedSheetNamingType == "LP PLAN") 
            {
                overTemplateName = "RINCO - LP - OVER 1/100";
                suffixAddition = " - LOADING PLAN";
            }
            else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
            }

            foreach (var level in levels)
            {
                string baseSheetNum = GenerateBaseSheetNumber(level, levels);
                
                string uniqueBaseNum = baseSheetNum;
                int suffixCount = 1;
                while (usedBaseNums.Contains(uniqueBaseNum))
                {
                    uniqueBaseNum = $"{baseSheetNum}.{suffixCount}";
                    suffixCount++;
                }
                usedBaseNums.Add(uniqueBaseNum);

                string groupId = Guid.NewGuid().ToString();
                string defaultZoneName = "ZONE 1";
                string sheetName = GenerateSheetName(level);

                AddGroupedPartRow(level, "- OVER" + suffixAddition, "OVER_PLANS", overTemplateName, groupId, uniqueBaseNum, defaultZoneName, sheetName);
            }
        }

        [RelayCommand]
        private void AddPartRowLevel()
        {
            var usedLevelIds = PartRows.Where(x => x.SelectedLevel != null).Select(x => x.SelectedLevel.Id).ToHashSet();
            var level = ProjectLevels.FirstOrDefault(l => !usedLevelIds.Contains(l.Id)) ?? ProjectLevels.FirstOrDefault();
            
            if (level != null)
            {
                string overTemplateName = "RINCO - GA - OVER 1/100";
                string suffixAddition = "";

                if (SelectedSheetNamingType == "LP PLAN") 
                {
                    overTemplateName = "RINCO - LP - OVER 1/100";
                    suffixAddition = " - LOADING PLAN";
                }
                else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
                {
                    var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                        vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
                }

                string baseSheetNum = GenerateBaseSheetNumber(level, ProjectLevels);
                
                string uniqueBaseNum = baseSheetNum;
                int suffixCount = 1;
                while (PartRows.Any(r => r.SheetNumber == uniqueBaseNum))
                {
                    uniqueBaseNum = $"{baseSheetNum}.{suffixCount}";
                    suffixCount++;
                }

                string groupId = Guid.NewGuid().ToString();
                string defaultZoneName = "ZONE 1";
                string sheetName = GenerateSheetName(level);

                AddGroupedPartRow(level, "- OVER" + suffixAddition, "OVER_PLANS", overTemplateName, groupId, uniqueBaseNum, defaultZoneName, sheetName);
            }
        }

        private void RemoveRow(AutoViewSheetRow row)
        {
            if (row != null && AutoRows.Contains(row))
            {
                AutoRows.Remove(row);
            }
        }

        [RelayCommand]
        private void RunAutoProcess()
        {
            StatusMessage = "Processing...";
            _handler.Raise();
        }

        [RelayCommand]
        private void AddZoneForLevel(AutoViewSheetRow rowGroup)
        {
            if (rowGroup == null) return;
            var level = rowGroup.SelectedLevel;
            if (level == null) return;

            string groupId = Guid.NewGuid().ToString();

            int maxZone = 0;
            string lastSheetNumber = null;
            foreach (var r in PartRows.Where(x => x.SelectedLevel?.Id == level.Id))
            {
                var match = Regex.Match(r.ZoneName ?? "", @"ZONE (\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int z))
                {
                    if (z > maxZone)
                    {
                        maxZone = z;
                        lastSheetNumber = r.SheetNumber;
                    }
                }
            }
            string defaultZoneName = $"ZONE {maxZone + 1}";

            string baseSheetNum = lastSheetNumber;
            if (string.IsNullOrEmpty(baseSheetNum))
            {
                baseSheetNum = GenerateBaseSheetNumber(level, ProjectLevels);
            }
            else
            {
                // Increment trailing number
                var numMatch = Regex.Match(baseSheetNum, @"(\d+)$");
                if (numMatch.Success)
                {
                    string numStr = numMatch.Groups[1].Value;
                    if (long.TryParse(numStr, out long num))
                    {
                        string newNumStr = (num + 1).ToString().PadLeft(numStr.Length, '0');
                        baseSheetNum = baseSheetNum.Substring(0, numMatch.Index) + newNumStr;
                    }
                    else baseSheetNum += ".1";
                }
                else baseSheetNum += ".1";
            }

            string uniqueBaseNum = baseSheetNum;
            int suffixCount = 1;
            while (PartRows.Any(r => r.SheetNumber == uniqueBaseNum))
            {
                uniqueBaseNum = $"{baseSheetNum}.{suffixCount}";
                suffixCount++;
            }
            baseSheetNum = uniqueBaseNum;
            
            string overTemplateName = "RINCO - GA - OVER 1/100";
            string suffixAddition = "";

            if (SelectedSheetNamingType == "LP PLAN") 
            {
                overTemplateName = "RINCO - LP - OVER 1/100";
                suffixAddition = " - LOADING PLAN";
            }
            else if (SelectedSheetNamingType == "MẶT BẰNG KẾT CẤU")
            {
                var mbkcTmpl = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0);
                if (mbkcTmpl != null) overTemplateName = mbkcTmpl.Name;
            }

            string sheetName = !string.IsNullOrEmpty(rowGroup.SheetName) ? rowGroup.SheetName : GenerateSheetName(level);

            AddGroupedPartRow(level, "- OVER" + suffixAddition, "OVER_PLANS", overTemplateName, groupId, baseSheetNum, defaultZoneName, sheetName);
        }

        private void AddGroupedPartRow(LevelItem level, string suffix, string viewTypeKey, string templateKey, string groupId, string sheetNum, string zoneName, string sheetNameStr)
        {
            // Fuzzy match view type based on suffix
            var matchedViewType = ViewTypes.FirstOrDefault(vt => vt.Name.Equals(viewTypeKey, StringComparison.OrdinalIgnoreCase));
            if (matchedViewType == null)
            {
                matchedViewType = ViewTypes.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.EndsWith("OVER", StringComparison.OrdinalIgnoreCase) || 
                    (vt.Name.IndexOf("OVER", StringComparison.OrdinalIgnoreCase) >= 0 && vt.Name.IndexOf("OVERLAY", StringComparison.OrdinalIgnoreCase) < 0));
            }
            if (matchedViewType == null) matchedViewType = ViewTypes.FirstOrDefault();
            
            // Fuzzy match template
            var matchedTemplate = ViewTemplates.FirstOrDefault(vt => vt.Name.Equals(templateKey, StringComparison.OrdinalIgnoreCase));
            if (matchedTemplate == null)
            {
                matchedTemplate = ViewTemplates.FirstOrDefault(vt => 
                    vt.Name.IndexOf("MẶT BẰNG KẾT CẤU", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.IndexOf("MBKC", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    vt.Name.Contains("OVER 1/100"));
            }
            if (matchedTemplate == null) matchedTemplate = ViewTemplates.FirstOrDefault();
            
            var row = new AutoViewSheetRow
            {
                SelectedLevel = level,
                Suffix = suffix,
                SelectedViewType = matchedViewType,
                SelectedViewTemplate = matchedTemplate,
                SelectedScopeBox = ScopeBoxOptions.FirstOrDefault(),
                SheetNumber = sheetNum,
                SheetName = sheetNameStr,
                GroupId = groupId,
                IsGroupSelected = true,
                ZoneName = zoneName
            };

            row.GroupSelectionChangedCallback = (r, isSelected) =>
            {
                foreach (var groupRow in PartRows.Where(gr => gr.GroupId == r.GroupId && gr != r))
                    groupRow.IsSelected = isSelected;
            };

            row.LevelChangedCallback = (r, oldLvl, newLvl) =>
            {
                if (oldLvl == null || newLvl == null) return;
                var rowsToChange = PartRows.Where(gr => gr.SelectedLevel?.Id == oldLvl.Id && gr != r).ToList();
                foreach (var groupRow in rowsToChange)
                {
                    groupRow.SelectedLevel = newLvl;
                }
            };

            row.SheetNumberChangedCallback = (r, val) =>
            {
                foreach (var groupRow in PartRows.Where(gr => gr.GroupId == r.GroupId && gr != r))
                    groupRow.SheetNumber = val;
            };

            row.SheetNameChangedCallback = (r, val) =>
            {
                foreach (var groupRow in PartRows.Where(gr => gr.GroupId == r.GroupId && gr != r))
                    groupRow.SheetName = val;
            };

            row.ZoneNameChangedCallback = (r, val) =>
            {
                foreach (var groupRow in PartRows.Where(gr => gr.GroupId == r.GroupId && gr != r))
                    groupRow.ZoneName = val;
            };

            row.ScopeBoxChangedCallback = (r, val) =>
            {
                foreach (var groupRow in PartRows.Where(gr => gr.GroupId == r.GroupId && gr != r))
                    groupRow.SelectedScopeBox = val;
            };

            row.LevelSelectionChangedCallback = (r, val) =>
            {
                if (r.SelectedLevel == null) return;
                var levelId = r.SelectedLevel.Id;
                foreach (var groupRow in PartRows.Where(gr => gr.SelectedLevel?.Id == levelId))
                {
                    if (groupRow != r) groupRow.IsLevelSelected = val;
                }
            };

            PartRows.Add(row);
        }

        [RelayCommand]
        private void RemovePartRowForLevel(AutoViewSheetRow rowGroup)
        {
            if (rowGroup == null || rowGroup.SelectedLevel == null) return;
            var levelId = rowGroup.SelectedLevel.Id;
            var rowsToRemove = PartRows.Where(r => r.SelectedLevel?.Id == levelId && (r.IsGroupSelected || r.IsSelected)).ToList();
            foreach (var r in rowsToRemove)
            {
                PartRows.Remove(r);
            }
        }

        [RelayCommand]
        private void RunAutoProcessPart()
        {
            StatusMessage = "Processing Parts...";
            _handler.IsPartMode = true;
            _handler.Raise();
        }
    }
}
