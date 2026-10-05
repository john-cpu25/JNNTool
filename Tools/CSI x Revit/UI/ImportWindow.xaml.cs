using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CSIxRevit.Mapping;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.UI
{
    public partial class ImportWindow : Window
    {
        private readonly Document _doc;
        private readonly FamilyMapper _mapper;
        private readonly Dictionary<string, SectionDefinition> _sectionDefs;

        public ObservableCollection<CreateSectionItem> CreateSectionItems { get; set; }
        public ObservableCollection<SectionMappingItem> MappingItems { get; set; }

        public bool ApplyCardinalPoint => ChkApplyCardinalPoint.IsChecked == true;
        public bool ApplyEdgeAlignment => ChkApplyEdgeAlignment.IsChecked == true;

        public ImportWindow(
            Document doc,
            List<SectionMappingItem> items,
            List<Level> levels,
            FamilyMapper mapper = null,
            Dictionary<string, SectionDefinition> sectionDefs = null)
        {
            InitializeComponent();

            _doc = doc;
            _mapper = mapper ?? new FamilyMapper(doc);
            _sectionDefs = sectionDefs ?? new Dictionary<string, SectionDefinition>(StringComparer.OrdinalIgnoreCase);

            CmbLevels.ItemsSource = levels;
            if (levels.Count > 0) CmbLevels.SelectedIndex = 0;

            MappingItems = new ObservableCollection<SectionMappingItem>(items);
            DgMapping.ItemsSource = MappingItems;

            InitCreateSectionsTab();
            AutoMatch();

            // If there are missing sections, show Tab 1 first; otherwise Tab 2
            if (CreateSectionItems.Any(x => x.IsMissing))
            {
                MainTabControl.SelectedIndex = 0;
            }
            else
            {
                MainTabControl.SelectedIndex = 1;
            }
        }

        private void InitCreateSectionsTab()
        {
            // Set up template selectors
            CmbDefaultBeamTemplate.ItemsSource = _mapper.BeamSymbols;
            if (_mapper.BeamSymbols.Count > 0) CmbDefaultBeamTemplate.SelectedIndex = 0;

            CmbDefaultColTemplate.ItemsSource = _mapper.ColumnSymbols;
            if (_mapper.ColumnSymbols.Count > 0) CmbDefaultColTemplate.SelectedIndex = 0;

            var defaultBeamSymbol = _mapper.BeamSymbols.FirstOrDefault();
            var defaultColSymbol = _mapper.ColumnSymbols.FirstOrDefault();

            var list = new List<CreateSectionItem>();

            foreach (var item in MappingItems)
            {
                if (item.ElementType != "Dầm (Beam)" && item.ElementType != "Cột (Column)")
                    continue;

                bool isCol = item.ElementType == "Cột (Column)";
                string cleanName = CleanName(item.EtabsSectionName);

                bool exists = false;
                if (isCol)
                {
                    exists = _mapper.ColumnSymbols.Any(s => CleanName(s.Name) == cleanName || CleanName(s.Name).Contains(cleanName));
                }
                else
                {
                    exists = _mapper.BeamSymbols.Any(s => CleanName(s.Name) == cleanName || CleanName(s.Name).Contains(cleanName));
                }

                _sectionDefs.TryGetValue(item.EtabsSectionName, out var def);

                double widthMm = def?.WidthMm ?? 0;
                double depthMm = def?.DepthMm ?? 0;
                bool isCircular = def?.IsCircular ?? false;

                // Fallback: parse dimensions from name (e.g. B-300x600, C-500)
                if (widthMm <= 0 && depthMm <= 0)
                {
                    var matchRect = Regex.Match(item.EtabsSectionName, @"(\d+)[xX*_-](\d+)");
                    if (matchRect.Success)
                    {
                        double.TryParse(matchRect.Groups[1].Value, out widthMm);
                        double.TryParse(matchRect.Groups[2].Value, out depthMm);
                    }
                    else
                    {
                        var matchCir = Regex.Match(item.EtabsSectionName, @"[Cc]-?(\d+)");
                        if (matchCir.Success)
                        {
                            double.TryParse(matchCir.Groups[1].Value, out depthMm);
                            widthMm = depthMm;
                            isCircular = true;
                        }
                    }
                }

                string dimSummary = def != null && !string.IsNullOrEmpty(def.DimensionSummary)
                    ? def.DimensionSummary
                    : (isCircular ? $"Ø{Math.Round(depthMm, 0)} mm" : (widthMm > 0 && depthMm > 0 ? $"{Math.Round(widthMm, 0)}×{Math.Round(depthMm, 0)} mm" : ""));

                var availableTemplates = isCol ? _mapper.ColumnSymbols : _mapper.BeamSymbols;

                FamilySymbol selectedTemplate = null;
                if (isCol)
                {
                    if (isCircular)
                    {
                        selectedTemplate = _mapper.ColumnSymbols.FirstOrDefault(s => s.Name.IndexOf("Circle", StringComparison.OrdinalIgnoreCase) >= 0 || s.Name.IndexOf("Round", StringComparison.OrdinalIgnoreCase) >= 0 || s.Name.IndexOf("Tron", StringComparison.OrdinalIgnoreCase) >= 0)
                            ?? defaultColSymbol;
                    }
                    else
                    {
                        selectedTemplate = defaultColSymbol;
                    }
                }
                else
                {
                    selectedTemplate = defaultBeamSymbol;
                }

                list.Add(new CreateSectionItem
                {
                    IsSelected = !exists,
                    IsMissing = !exists,
                    SectionName = item.EtabsSectionName,
                    ElementType = item.ElementType,
                    DimensionSummary = dimSummary,
                    WidthMm = widthMm,
                    DepthMm = depthMm,
                    IsCircular = isCircular,
                    Status = exists ? "✅ Đã có trong Revit" : "⚠️ Chưa có trong Revit",
                    AvailableTemplates = availableTemplates,
                    SelectedTemplate = selectedTemplate
                });
            }

            CreateSectionItems = new ObservableCollection<CreateSectionItem>(list);
            DgCreateSections.ItemsSource = CreateSectionItems;
        }

        private void BtnApplyGlobalTemplate_Click(object sender, RoutedEventArgs e)
        {
            var beamTemplate = CmbDefaultBeamTemplate.SelectedItem as FamilySymbol;
            var colTemplate = CmbDefaultColTemplate.SelectedItem as FamilySymbol;

            foreach (var item in CreateSectionItems)
            {
                if (item.ElementType == "Dầm (Beam)" && beamTemplate != null)
                {
                    item.SelectedTemplate = beamTemplate;
                }
                else if (item.ElementType == "Cột (Column)" && colTemplate != null)
                {
                    item.SelectedTemplate = colTemplate;
                }
            }

            DgCreateSections.Items.Refresh();
        }

        private void BtnSelectAllMissing_Click(object sender, RoutedEventArgs e)
        {
            bool anyUnchecked = CreateSectionItems.Any(x => x.IsMissing && !x.IsSelected);
            foreach (var item in CreateSectionItems)
            {
                if (item.IsMissing) item.IsSelected = anyUnchecked;
            }
            DgCreateSections.Items.Refresh();
        }

        private void BtnCreateSelected_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = CreateSectionItems.Where(x => x.IsSelected).ToList();
            if (selectedItems.Count == 0)
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn ít nhất một tiết diện để tạo.");
                return;
            }

            int createdCount = 0;
            int errorCount = 0;

            using (Transaction tx = new Transaction(_doc, "JNN - Tạo tiết diện từ ETABS"))
            {
                tx.Start();

                foreach (var item in selectedItems)
                {
                    if (item.SelectedTemplate == null)
                    {
                        errorCount++;
                        continue;
                    }

                    try
                    {
                        FamilySymbol targetSymbol = null;
                        var family = item.SelectedTemplate.Family;

                        // Check if type with this name already exists in the family
                        foreach (ElementId id in family.GetFamilySymbolIds())
                        {
                            var s = _doc.GetElement(id) as FamilySymbol;
                            if (s != null && s.Name.Equals(item.SectionName, StringComparison.OrdinalIgnoreCase))
                            {
                                targetSymbol = s;
                                break;
                            }
                        }

                        if (targetSymbol == null)
                        {
                            targetSymbol = item.SelectedTemplate.Duplicate(item.SectionName) as FamilySymbol;
                        }

                        if (targetSymbol != null)
                        {
                            SetSymbolDimensionParameters(targetSymbol, item.IsCircular, item.WidthMm, item.DepthMm);

                            item.Status = "✅ Đã tạo trong Revit";
                            item.IsMissing = false;
                            item.IsSelected = false;
                            createdCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        item.Status = "❌ Lỗi: " + ex.Message;
                    }
                }

                tx.Commit();
            }

            // Reload FamilyMapper
            _mapper.Reload();

            // Refresh available types in mapping tab
            RefreshMappingAvailableTypes();

            // Re-run automatch
            AutoMatch();

            DgCreateSections.Items.Refresh();
            DgMapping.Items.Refresh();

            // Switch to mapping tab
            MainTabControl.SelectedIndex = 1;

            string msg = $"Đã tạo thành công {createdCount} tiết diện trong Revit!";
            if (errorCount > 0) msg += $"\nCó {errorCount} tiết diện gặp lỗi.";
            msg += "\n\nĐã chuyển sang Tab 'Khớp tiết diện' và tự động khớp.";
            TaskDialog.Show("Thành công", msg);
        }

        private static void SetSymbolDimensionParameters(FamilySymbol symbol, bool isCircular, double widthMm, double depthMm)
        {
            double widthFeet = UnitUtils.ConvertToInternalUnits(widthMm > 0 ? widthMm : 300, UnitTypeId.Millimeters);
            double depthFeet = UnitUtils.ConvertToInternalUnits(depthMm > 0 ? depthMm : 500, UnitTypeId.Millimeters);

            if (isCircular)
            {
                var names = new[] { "D", "d", "Diameter", "Đường kính", "b", "h", "Width", "Height" };
                SetSymbolParameter(symbol, names, depthFeet);
            }
            else
            {
                var widthNames = new[] { "b", "B", "Width", "width", "Chiều rộng", "b (Width)" };
                var heightNames = new[] { "h", "H", "Height", "height", "Chiều cao", "h (Height)", "d", "Depth" };

                SetSymbolParameter(symbol, widthNames, widthFeet);
                SetSymbolParameter(symbol, heightNames, depthFeet);
            }
        }

        private static bool SetSymbolParameter(FamilySymbol symbol, IEnumerable<string> paramNames, double valueInFeet)
        {
            var nameSet = new HashSet<string>(paramNames, StringComparer.OrdinalIgnoreCase);
            foreach (Parameter p in symbol.Parameters)
            {
                if (!p.IsReadOnly && p.StorageType == StorageType.Double && nameSet.Contains(p.Definition.Name))
                {
                    p.Set(valueInFeet);
                    return true;
                }
            }
            return false;
        }

        private void RefreshMappingAvailableTypes()
        {
            foreach (var item in MappingItems)
            {
                if (item.ElementType == "Dầm (Beam)")
                    item.AvailableTypes = _mapper.BeamSymbols.Cast<ElementType>().ToList();
                else if (item.ElementType == "Cột (Column)")
                    item.AvailableTypes = _mapper.ColumnSymbols.Cast<ElementType>().ToList();
                else if (item.ElementType == "Vách (Wall)")
                    item.AvailableTypes = _mapper.WallTypes.Cast<ElementType>().ToList();
                else if (item.ElementType == "Sàn (Floor)")
                    item.AvailableTypes = _mapper.FloorTypes.Cast<ElementType>().ToList();
            }
        }

        private void BtnGoToMapping_Click(object sender, RoutedEventArgs e)
        {
            MainTabControl.SelectedIndex = 1;
        }

        private void AutoMatch()
        {
            foreach (var item in MappingItems)
            {
                if (item.AvailableTypes == null || item.AvailableTypes.Count == 0) continue;

                string etabsName = CleanName(item.EtabsSectionName);

                // 1. Exact match
                var exactMatch = item.AvailableTypes.FirstOrDefault(s => CleanName(s.Name) == etabsName);
                if (exactMatch != null)
                {
                    item.SelectedType = exactMatch;
                    continue;
                }

                // 2. Partial match
                var partialMatch = item.AvailableTypes.FirstOrDefault(s =>
                {
                    string sName = CleanName(s.Name);
                    return sName.Contains(etabsName) || etabsName.Contains(sName);
                });

                if (partialMatch != null)
                {
                    item.SelectedType = partialMatch;
                    continue;
                }

                // Do not fallback to random first item! Keep null if not matched
                item.SelectedType = null;
            }
        }

        private static string CleanName(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace(" ", "").Replace("_", "").Replace("-", "").ToLowerInvariant();
        }

        private void BtnAutoMatch_Click(object sender, RoutedEventArgs e)
        {
            AutoMatch();
            DgMapping.Items.Refresh();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            _mapper.Reload();
            RefreshMappingAvailableTypes();
            AutoMatch();
            DgMapping.Items.Refresh();
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            // Verify if any active section is unmapped
            var unmapped = MappingItems.Where(x => x.SelectedType == null).ToList();
            if (unmapped.Count > 0)
            {
                string unmappedNames = string.Join(", ", unmapped.Take(5).Select(x => x.EtabsSectionName));
                if (unmapped.Count > 5) unmappedNames += $", ... (+{unmapped.Count - 5})";

                var res = TaskDialog.Show("Cảnh báo",
                    $"Có {unmapped.Count} tiết diện chưa được khớp Family Revit:\n{unmappedNames}\n\nCác đối tượng này sẽ bị bỏ qua khi dựng hình. Bạn có muốn tiếp tục không?",
                    TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No);

                if (res != TaskDialogResult.Yes) return;
            }

            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public Dictionary<string, ElementType> GetBeamMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Dầm (Beam)" && x.SelectedType != null).ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetColumnMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Cột (Column)" && x.SelectedType != null).ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetFloorMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Sàn (Floor)" && x.SelectedType != null).ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public Dictionary<string, ElementType> GetWallMapping()
        {
            return MappingItems.Where(x => x.ElementType == "Vách (Wall)" && x.SelectedType != null).ToDictionary(x => x.EtabsSectionName, x => x.SelectedType);
        }

        public ElementId GetSelectedLevelId()
        {
            if (CmbLevels.SelectedItem is Level level) return level.Id;
            return ElementId.InvalidElementId;
        }
    }

    public class CreateSectionItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private FamilySymbol _selectedTemplate;
        private string _status = "Chưa có trong Revit";

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public bool IsMissing { get; set; } = true;
        public string SectionName { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string DimensionSummary { get; set; } = "";
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public bool IsCircular { get; set; }

        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public List<FamilySymbol> AvailableTemplates { get; set; } = new List<FamilySymbol>();

        public FamilySymbol SelectedTemplate
        {
            get => _selectedTemplate;
            set { _selectedTemplate = value; OnPropertyChanged(nameof(SelectedTemplate)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SectionMappingItem : INotifyPropertyChanged
    {
        private ElementType _selectedType;

        public string EtabsSectionName { get; set; }
        public string SectionDetails { get; set; }
        public string ElementType { get; set; }
        public int ObjectCount { get; set; }

        public ElementType SelectedType
        {
            get => _selectedType;
            set { _selectedType = value; OnPropertyChanged(nameof(SelectedType)); }
        }

        public List<ElementType> AvailableTypes { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
