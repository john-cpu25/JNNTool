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
using JNNTool.Tools.CSIxRevit.Builder;
using JNNTool.Core.ExternalEvents;

namespace JNNTool.Tools.CSIxRevit.UI
{
    public partial class ImportWindow : Window
    {
        private readonly Document _doc;
        private readonly FamilyMapper _mapper;
        private readonly Dictionary<string, SectionDefinition> _sectionDefs;
        private readonly ActionEventHandler _eventHandler;
        private readonly TypeDimensionReader _dimReader = new TypeDimensionReader();

        // Type vừa tạo / đã có ở Tab 1 → ghim cho Tab 2, để AutoMatch không ghi đè khi tên Type Revit khác tên ETABS.
        private readonly Dictionary<string, ElementId> _pinnedTypes = new Dictionary<string, ElementId>(StringComparer.OrdinalIgnoreCase);

        private Func<bool> _importAction;
        private bool _hasImported = false;
        private bool _busy = false;

        /// <summary>Cửa sổ đang mở (modeless) – chặn mở trùng nhiều cửa sổ cùng lúc.</summary>
        public static ImportWindow Current { get; private set; }

        public ObservableCollection<CreateSectionItem> CreateSectionItems { get; set; }
        public ObservableCollection<SectionMappingItem> MappingItems { get; set; }
        public ObservableCollection<LevelMappingItem> LevelItems { get; set; }
        public List<string> ExistingLevelNames { get; set; } = new List<string>();

        // Scope options: cho phép người dùng chọn cấu kiện nào cần/không cần dựng
        public bool ImportGrids => ChkScopeGrids?.IsChecked == true;
        public bool ImportLevels => ChkScopeLevels?.IsChecked == true;
        public bool ImportColumns => ChkScopeColumns?.IsChecked == true;
        public bool ImportBeams => ChkScopeBeams?.IsChecked == true;
        public bool ImportWalls => ChkScopeWalls?.IsChecked == true;
        public bool ImportFloors => ChkScopeFloors?.IsChecked == true;

        public bool ApplyCardinalPoint => ChkApplyCardinalPoint.IsChecked == true;
        // Đã bỏ tùy chọn căn lề biên khỏi UI: luôn giữ nguyên tim cột/dầm theo E2K.
        public bool ApplyEdgeAlignment => false;
        public bool FitScopeBox => ChkFitScopeBox.IsChecked == true;

        public void SetImportAction(Func<bool> action) => _importAction = action;

        public List<LevelMappingItem> GetLevelItems() => LevelItems?.ToList() ?? new List<LevelMappingItem>();

        public ImportWindow(
            Document doc,
            List<SectionMappingItem> items,
            List<Level> levels,
            FamilyMapper mapper = null,
            Dictionary<string, SectionDefinition> sectionDefs = null,
            ActionEventHandler eventHandler = null,
            List<StoryData> stories = null)
        {
            InitializeComponent();

            _doc = doc;
            _mapper = mapper ?? new FamilyMapper(doc);
            _sectionDefs = sectionDefs ?? new Dictionary<string, SectionDefinition>(StringComparer.OrdinalIgnoreCase);
            _eventHandler = eventHandler;

            Current = this;
            Closed += (s, e) =>
            {
                if (ReferenceEquals(Current, this)) Current = null;
                _importAction = null;
                System.Windows.Input.Mouse.OverrideCursor = null;
            };

            CmbLevels.ItemsSource = levels;
            if (levels.Count > 0) CmbLevels.SelectedIndex = 0;

            MappingItems = new ObservableCollection<SectionMappingItem>(items);
            DgMapping.ItemsSource = MappingItems;

            // Nạp danh sách tên Level hiện có trong Revit để người dùng chọn map
            ExistingLevelNames = new List<string> { "" };
            if (levels != null)
            {
                ExistingLevelNames.AddRange(levels.Select(l => l.Name).OrderBy(n => n));
            }

            // Nạp danh sách Story từ ETABS để cấu hình Level trong Tab 3
            var lvlList = new List<LevelMappingItem>();
            if (stories != null)
            {
                foreach (var s in stories)
                {
                    string targetName = s.Name;
                    if (Regex.IsMatch(s.Name, @"^[Tt](\d+)$"))
                    {
                        var m = Regex.Match(s.Name, @"^[Tt](\d+)$");
                        targetName = "Tầng " + m.Groups[1].Value;
                    }
                    else if (s.Name.Equals("M", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("ROOF", StringComparison.OrdinalIgnoreCase))
                    {
                        targetName = "Tầng Mái";
                    }

                    string matchedName = "";
                    if (levels != null)
                    {
                        var existing = levels.FirstOrDefault(l => Math.Abs(l.Elevation - s.Elevation) < 0.05);
                        if (existing != null) matchedName = existing.Name;
                    }

                    lvlList.Add(new LevelMappingItem
                    {
                        IsSelected = s.IsSelected,
                        EtabsStoryName = s.Name,
                        ElevationFeet = s.Elevation,
                        HeightFeet = s.Height,
                        TargetLevelName = !string.IsNullOrEmpty(s.TargetName) ? s.TargetName : targetName,
                        MappedLevelName = matchedName
                    });
                }
            }
            LevelItems = new ObservableCollection<LevelMappingItem>(lvlList);
            DgLevels.ItemsSource = LevelItems;

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

        #region Modeless → Revit API context

        /// <summary>
        /// Đẩy thao tác đọc/ghi model về Revit API context qua ExternalEvent.
        /// Cửa sổ modeless không được mở Transaction trực tiếp từ sự kiện WPF.
        /// </summary>
        private void RunInRevit(Action action)
        {
            if (_busy) return;
            if (_eventHandler == null)
            {
                MessageBox.Show(this, "ExternalEvent is not initialized. Please close and re-run the command.", "JNN - CSI x Revit",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SetBusy(true);
            _eventHandler.Post(app =>
            {
                try
                {
                    if (_doc == null || !_doc.IsValidObject)
                    {
                        TaskDialog.Show("JNN - CSI x Revit", "The Revit document has been closed. This window will now close.");
                        SafeClose();
                        return;
                    }
                    action();
                }
                catch (Exception ex)
                {
                    JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi khi thực thi trên Revit", ex);
                    TaskDialog.Show("JNN - CSI x Revit", "An error occurred: " + ex.Message + "\n\nDetails have been written to the log.");
                }
                finally
                {
                    SetBusy(false);
                }
            });
        }

        private void SetBusy(bool busy)
        {
            try
            {
                _busy = busy;
                MainTabControl.IsEnabled = !busy;
                BtnAutoMatch.IsEnabled = !busy;
                BtnReload.IsEnabled = !busy;
                BtnImport.IsEnabled = !busy;
                System.Windows.Input.Mouse.OverrideCursor = busy ? System.Windows.Input.Cursors.Wait : null;
            }
            catch { }
        }

        private void SafeClose()
        {
            try { Close(); } catch { }
        }

        #endregion

        #region Tab 1 - Tạo tiết diện thiếu

        private void InitCreateSectionsTab()
        {
            BindDefaultTemplateCombos();

            var list = new List<CreateSectionItem>();

            foreach (var item in MappingItems)
            {
                string kind = item.ElementType;
                _sectionDefs.TryGetValue(item.EtabsSectionName ?? "", out var def);

                double widthMm = def?.WidthMm ?? 0;
                double depthMm = def?.DepthMm ?? 0;
                bool isCircular = def?.IsCircular ?? false;

                if (kind == ElementKinds.Column || kind == ElementKinds.Beam)
                {
                    if (widthMm <= 0 && depthMm <= 0)
                    {
                        var matchRect = Regex.Match(item.EtabsSectionName ?? "", @"(\d+)[xX*_-](\d+)");
                        if (matchRect.Success)
                        {
                            double.TryParse(matchRect.Groups[1].Value, out widthMm);
                            double.TryParse(matchRect.Groups[2].Value, out depthMm);
                        }
                    }
                }
                else if (kind == ElementKinds.Slab || kind == ElementKinds.FoundationSlab || kind == ElementKinds.Wall)
                {
                    if (depthMm <= 0)
                    {
                        var m = Regex.Match(item.EtabsSectionName ?? "", @"\d+");
                        if (m.Success && double.TryParse(m.Value, out double d)) depthMm = d;
                        else depthMm = kind == ElementKinds.Wall ? 200 : 100;
                    }
                }
                else if (kind != ElementKinds.VirtualSlab)
                {
                    continue;
                }

                string dimSummary = "";
                if (ElementKinds.IsFloorLike(kind) || kind == ElementKinds.Wall)
                {
                    if (kind == ElementKinds.VirtualSlab) dimSummary = "Thk ≤ 1 mm (virtual)";
                    else if (depthMm > 0) dimSummary = $"Thk {Math.Round(depthMm, 0)} mm";
                }
                else
                {
                    dimSummary = def != null && !string.IsNullOrEmpty(def.DimensionSummary)
                        ? def.DimensionSummary
                        : (isCircular ? $"Ø{Math.Round(depthMm, 0)} mm" : (widthMm > 0 && depthMm > 0 ? $"{Math.Round(widthMm, 0)}×{Math.Round(depthMm, 0)} mm" : ""));
                }

                list.Add(new CreateSectionItem
                {
                    SectionName = item.EtabsSectionName,
                    ElementType = kind,
                    DimensionSummary = dimSummary,
                    WidthMm = widthMm,
                    DepthMm = depthMm,
                    IsCircular = isCircular,
                    TargetTypeName = TypeNameSuggester.Suggest(item.EtabsSectionName, kind, widthMm, depthMm, isCircular)
                });
            }

            CreateSectionItems = new ObservableCollection<CreateSectionItem>(list);
            foreach (var row in CreateSectionItems)
            {
                ApplyRowTemplates(row, null, keepSelection: false);
            }
            DgCreateSections.ItemsSource = CreateSectionItems;
        }

        /// <summary>
        /// Nạp lại danh sách Family / Type mẫu cho 1 dòng và xác định trạng thái "đã có / chưa có".
        /// "Đã có" = có type trùng tên HOẶC trùng kích thước thực (đọc từ type, không đoán theo tên);
        /// khi đó chọn sẵn type đó cho người dùng và ghim cho Tab 2.
        /// </summary>
        public static bool IsSteelOrVoidSection(string sectionName, SectionDefinition def, string elementType = null, string selectedFamilyOrTypeName = null)
        {
            // Chỉ áp dụng cho Beam và Column. Wall, Slab, FoundationSlab tuyệt đối không phải steel beam/void!
            if (!string.IsNullOrEmpty(elementType) &&
                elementType != ElementKinds.Beam && elementType != ElementKinds.Column)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(selectedFamilyOrTypeName))
            {
                string s = selectedFamilyOrTypeName.ToLowerInvariant();
                if (s.Contains("void") || s.Contains("dummy") || s.Contains("null"))
                    return true;
            }

            if (def != null)
            {
                if (!string.IsNullOrEmpty(def.Shape))
                {
                    string sh = def.Shape.ToLowerInvariant();
                    if (sh.Contains("steel") || sh.Contains("channel") || sh.Contains("i/wide") ||
                        sh.Contains("angle") || sh.Contains("tube") || sh.Contains("pipe") || sh.Contains("tee"))
                        return true;
                }
                if (!string.IsNullOrEmpty(def.Material))
                {
                    string mat = def.Material.ToUpperInvariant();
                    if (mat.Contains("Q235") || mat.Contains("Q345") || mat.Contains("SS400") ||
                        mat.Contains("A36") || mat.Contains("STEEL") || mat.Contains("THEP"))
                        return true;
                }
            }

            if (!string.IsNullOrEmpty(sectionName))
            {
                string sn = sectionName.Trim();
                // Dầm thép chữ I: i-250x125, I-250, UB, UC, HE, IPE... (Wide Flange Mỹ là W14x90 chứ KHÔNG phải W20)
                if (Regex.IsMatch(sn, @"^(i|I)[-_]", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(sn, @"^(UB|UC|HE|IPE|IPN)\d+", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(sn, @"^W\d+[xX]\d+", RegexOptions.IgnoreCase))
                    return true;

                // Xà gồ: XG-C, C250x75, Purlin, Z...
                if (Regex.IsMatch(sn, @"^(XG|PURLIN)[-_]", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(sn, @"^C\d+x\d+", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(sn, @"^Z\d+x\d+", RegexOptions.IgnoreCase))
                    return true;

                if (sn.IndexOf("void", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sn.IndexOf("dummy", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    sn.IndexOf("null", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Nạp lại danh sách Family / Type mẫu cho 1 dòng và xác định trạng thái "đã có / chưa có".
        /// "Đã có" = có type trùng tên HOẶC trùng kích thước thực (đọc từ type, không đoán theo tên);
        /// khi đó chọn sẵn type đó cho người dùng và ghim cho Tab 2.
        /// </summary>
        private void ApplyRowTemplates(CreateSectionItem row, ElementId preferredId, bool keepSelection)
        {
            if (row.ElementType == ElementKinds.VirtualSlab)
            {
                row.SetTemplates(new List<ElementType>(), null);
                row.CanEdit = false;
                row.IsSelected = false;
                row.IsMissing = false;
                row.Status = "⏭️ Virtual slab – skipped";
                return;
            }

            var types = _mapper.GetTypesForKind(row.ElementType);
            if (types.Count == 0)
            {
                row.SetTemplates(types, null);
                row.CanEdit = false;
                row.IsSelected = false;
                row.IsMissing = true;
                row.Status = row.ElementType == ElementKinds.FoundationSlab
                    ? "⚠️ No Foundation Slab type in project"
                    : "⚠️ No base type in project";
                return;
            }

            row.CanEdit = true;

            _sectionDefs.TryGetValue(row.SectionName ?? "", out var def);
            bool isSteelOrVoid = IsSteelOrVoidSection(row.SectionName, def, row.ElementType, null);
            row.IsSteelOrVoid = isSteelOrVoid;

            // Kiểm tra type đã có trong Revit chưa
            var existing = FindExistingType(row, types);
            if (existing != null)
            {
                row.SetTemplates(types, existing);
                row.IsMissing = false;
                row.IsSelected = false;
                row.TargetTypeName = existing.Name;
                row.Status = $"✅ Exists: {existing.Name}";
                _pinnedTypes[PinKey(row.ElementType, row.SectionName)] = existing.Id;
                return;
            }

            // Nếu CHƯA CÓ: VẪN TẠO! (row.IsSelected = true)
            ElementType preferred = null;
            if (preferredId != null)
            {
                preferred = types.FirstOrDefault(t => t.Id == preferredId);
            }

            if (preferred == null)
            {
                if (isSteelOrVoid)
                {
                    // Ưu tiên family thép phù hợp nếu có trong dự án:
                    string sn = (row.SectionName ?? "").ToLowerInvariant();
                    bool isChannel = sn.StartsWith("c") || sn.Contains("xg") || sn.Contains("channel");

                    if (isChannel)
                    {
                        preferred = types.FirstOrDefault(t =>
                        {
                            string fn = (t.FamilyName ?? "").ToLowerInvariant();
                            return fn.Contains("pfc") || fn.Contains("channel") || fn.Contains("c-");
                        });
                    }
                    else
                    {
                        preferred = types.FirstOrDefault(t =>
                        {
                            string fn = (t.FamilyName ?? "").ToLowerInvariant();
                            return fn.Contains("ub") || fn.Contains("universal") || fn.Contains("i-") || fn.Contains("wide");
                        });
                    }

                    // Nếu không có family thép thì mới tìm Void_Beam
                    if (preferred == null)
                    {
                        preferred = types.FirstOrDefault(t =>
                        {
                            string fn = (t.FamilyName ?? "").ToLowerInvariant();
                            string tn = (t.Name ?? "").ToLowerInvariant();
                            return fn.Contains("void") || tn.Contains("void");
                        });
                    }
                }

                if (preferred == null)
                {
                    preferred = GetDefaultTemplate(row.ElementType, types) ?? types[0];
                }
            }

            row.SetTemplates(types, preferred);
            row.IsMissing = true;
            if (!keepSelection) row.IsSelected = true; // VẪN TẠO!
            row.Status = "⚠️ No type with matching size";
        }

        /// <summary>
        /// Tìm type đã có: ưu tiên trùng tên, sau đó trùng kích thước (ưu tiên family của template mặc định).
        /// </summary>
        private ElementType FindExistingType(CreateSectionItem row, List<ElementType> types)
        {
            // Nếu dòng không phải steel/void, loại trừ các type Void_Beam để không nhận nhầm dầm bê tông thành Void!
            var candidateTypes = row.IsSteelOrVoid
                ? types
                : types.Where(t =>
                {
                    string fName = (t.FamilyName ?? "").ToLowerInvariant();
                    string tName = (t.Name ?? "").ToLowerInvariant();
                    return !(fName.Contains("void") || fName.Contains("dummy") || fName.Contains("null")
                          || tName.Contains("void") || tName.Contains("dummy") || tName.Contains("null"));
                }).ToList();

            string target = string.IsNullOrWhiteSpace(row.TargetTypeName) ? row.SectionName : row.TargetTypeName.Trim();
            string cleanTarget = CleanName(target);
            var byName = candidateTypes.FirstOrDefault(t => CleanName(t.Name) == cleanTarget);
            if (byName != null && SizeMatches(row, byName, allowUnknown: true)) return byName;

            var bySize = candidateTypes.Where(t => SizeMatches(row, t, allowUnknown: false)).ToList();
            if (bySize.Count == 0) return null;

            var defaultFamily = GetDefaultTemplate(row.ElementType, candidateTypes)?.FamilyName;
            return bySize.FirstOrDefault(t => defaultFamily != null && t.FamilyName == defaultFamily)
                ?? bySize.OrderBy(t => t.Name).First();
        }

        private bool SizeMatches(CreateSectionItem row, ElementType type, bool allowUnknown)
        {
            var size = _dimReader.Get(type);
            string kind = row.ElementType;

            if (kind == ElementKinds.Slab || kind == ElementKinds.FoundationSlab || kind == ElementKinds.Wall)
            {
                if (size.ThicknessMm <= 0) return allowUnknown;
                return SectionSizeMatcher.ThicknessMatches(row.DepthMm, size.ThicknessMm);
            }

            if (kind == ElementKinds.Column || kind == ElementKinds.Beam)
            {
                if (row.IsCircular)
                {
                    double d = size.DiameterMm > 0 ? size.DiameterMm : size.WidthMm;
                    if (d <= 0) return allowUnknown;
                    return SectionSizeMatcher.CircleMatches(row.DepthMm, d);
                }
                if (size.WidthMm <= 0 || size.HeightMm <= 0) return allowUnknown;
                return SectionSizeMatcher.RectMatches(row.WidthMm, row.DepthMm, size.WidthMm, size.HeightMm,
                    allowSwap: kind == ElementKinds.Column);
            }

            return false;
        }

        private void BindDefaultTemplateCombos()
        {
            BindCombo(CmbDefaultBeamTemplate, _mapper.BeamSymbols.Cast<ElementType>().ToList(), ElementKinds.Beam);
            BindCombo(CmbDefaultColTemplate, _mapper.ColumnSymbols.Cast<ElementType>().ToList(), ElementKinds.Column);
            BindCombo(CmbDefaultSlabTemplate, _mapper.SlabTypes.Cast<ElementType>().ToList(), ElementKinds.Slab);
            BindCombo(CmbDefaultFoundationTemplate, _mapper.FoundationSlabTypes.Cast<ElementType>().ToList(), ElementKinds.FoundationSlab);
            BindCombo(CmbDefaultWallTemplate, _mapper.WallTypes.Cast<ElementType>().ToList(), ElementKinds.Wall);
        }

        private static void BindCombo(System.Windows.Controls.ComboBox cmb, List<ElementType> types, string kind = null)
        {
            // Giữ lựa chọn theo Id: sau Reload các instance ElementType là object mới.
            ElementId prevId = (cmb.SelectedItem as ElementType)?.Id;
            cmb.ItemsSource = types;
            ElementType keep = prevId != null ? types.FirstOrDefault(t => t.Id == prevId) : null;
            if (keep != null)
            {
                cmb.SelectedItem = keep;
                return;
            }

            if (types.Count == 0)
            {
                cmb.SelectedItem = null;
                return;
            }

            // Với Beam và Column: ưu tiên dầm/cột bê tông chữ nhật thật, TRÁNH xa các family Void / Dummy / Null / Steel
            if (kind == ElementKinds.Beam || kind == ElementKinds.Column)
            {
                // Ưu tiên 1: Family chứa "Concrete", "Bê tông", "Rectangular" và KHÔNG chứa "Void", "Dummy", "Null", "Steel"
                var concrete = types.FirstOrDefault(t =>
                {
                    string fName = (t.FamilyName ?? "").ToLowerInvariant();
                    string tName = (t.Name ?? "").ToLowerInvariant();
                    bool isVoidOrSteel = fName.Contains("void") || fName.Contains("dummy") || fName.Contains("null") || fName.Contains("steel")
                                      || tName.Contains("void") || tName.Contains("dummy") || tName.Contains("null") || tName.Contains("steel");
                    if (isVoidOrSteel) return false;

                    return fName.Contains("concrete") || fName.Contains("bê tông") || fName.Contains("rectangular")
                        || tName.Contains("concrete") || tName.Contains("bê tông") || tName.Contains("rectangular");
                });

                if (concrete != null)
                {
                    cmb.SelectedItem = concrete;
                    return;
                }

                // Ưu tiên 2: Bất kỳ type nào KHÔNG chứa "Void", "Dummy", "Null", "Steel"
                var nonVoid = types.FirstOrDefault(t =>
                {
                    string fName = (t.FamilyName ?? "").ToLowerInvariant();
                    string tName = (t.Name ?? "").ToLowerInvariant();
                    return !(fName.Contains("void") || fName.Contains("dummy") || fName.Contains("null") || fName.Contains("steel")
                          || tName.Contains("void") || tName.Contains("dummy") || tName.Contains("null") || tName.Contains("steel"));
                });

                if (nonVoid != null)
                {
                    cmb.SelectedItem = nonVoid;
                    return;
                }
            }

            cmb.SelectedIndex = 0;
        }

        private System.Windows.Controls.ComboBox GetDefaultCombo(string kind)
        {
            switch (kind)
            {
                case ElementKinds.Beam: return CmbDefaultBeamTemplate;
                case ElementKinds.Column: return CmbDefaultColTemplate;
                case ElementKinds.Slab: return CmbDefaultSlabTemplate;
                case ElementKinds.FoundationSlab: return CmbDefaultFoundationTemplate;
                case ElementKinds.Wall: return CmbDefaultWallTemplate;
                default: return null;
            }
        }

        private ElementType GetDefaultTemplate(string kind, List<ElementType> candidates)
        {
            var sel = GetDefaultCombo(kind)?.SelectedItem as ElementType;
            if (sel == null) return null;
            return candidates.FirstOrDefault(t => t.Id == sel.Id);
        }

        private void BtnApplyGlobalTemplate_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgCreateSections);

            foreach (var item in CreateSectionItems)
            {
                // Chỉ áp cho dòng chưa có, để không làm mất hiển thị type đã tạo / đã có.
                if (!item.CanEdit || !item.IsMissing) continue;
                var sel = GetDefaultCombo(item.ElementType)?.SelectedItem as ElementType;
                if (sel != null) item.SelectTemplateById(sel.Id);
            }
        }

        private void BtnSelectAllMissing_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgCreateSections);

            bool anyUnchecked = CreateSectionItems.Any(x => x.IsMissing && x.CanEdit && !x.IsSelected);
            foreach (var item in CreateSectionItems)
            {
                if (item.IsMissing && item.CanEdit) item.IsSelected = anyUnchecked;
            }
        }

        private void BtnCreateSelected_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgCreateSections);

            var selectedItems = CreateSectionItems.Where(x => x.IsSelected && x.CanEdit).ToList();
            if (selectedItems.Count == 0)
            {
                MessageBox.Show(this, "Please select at least one section to create.", "JNN - CSI x Revit",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            RunInRevit(() => CreateSelectedTypes(selectedItems));
        }

        /// <summary>Chạy trong Revit API context (qua ExternalEvent).</summary>
        private void CreateSelectedTypes(List<CreateSectionItem> selectedItems)
        {
            int createdCount = 0;
            int errorCount = 0;
            var createdIds = new Dictionary<CreateSectionItem, ElementId>();
            var errorMessages = new Dictionary<CreateSectionItem, string>();

            using (Transaction tx = new Transaction(_doc, "JNN - Tạo tiết diện từ ETABS"))
            {
                try
                {
                    tx.Start();

                    foreach (var item in selectedItems)
                    {
                        if (item.SelectedTemplate == null || !item.SelectedTemplate.IsValidObject)
                        {
                            errorCount++;
                            errorMessages[item] = "❌ No base type selected";
                            continue;
                        }

                        string targetName = (item.TargetTypeName ?? "").Trim();
                        if (string.IsNullOrEmpty(targetName)) targetName = item.SectionName;

                        try
                        {
                            ElementType created = CreateOrUpdateType(item, targetName);
                            if (created != null)
                            {
                                createdIds[item] = created.Id;
                                createdCount++;
                            }
                            else
                            {
                                errorCount++;
                                errorMessages[item] = "❌ Could not duplicate type";
                            }
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            errorMessages[item] = "❌ Error: " + ex.Message;
                        }
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    if (tx.HasStarted() && !tx.HasEnded()) tx.RollBack();
                    JNNTool.Core.Logging.Logger.Error("CSI x Revit - tạo tiết diện thất bại", ex);
                    TaskDialog.Show("JNN - CSI x Revit", "Could not create sections: " + ex.Message);
                    return;
                }
            }

            // Nạp lại type từ document, rồi gán đúng type vừa tạo cho dòng (Tab 1) và cho mapping (Tab 2).
            RefreshAfterTypeChange(createdIds);

            foreach (var kv in errorMessages)
            {
                kv.Key.Status = kv.Value;
            }

            // Switch to mapping tab
            MainTabControl.SelectedIndex = 1;

            string msg = $"Successfully created {createdCount} section(s) in Revit!";
            if (errorCount > 0) msg += $"\n{errorCount} section(s) failed (see the Status column in Tab 1).";
            msg += "\n\nSwitched to 'Section Mapping' with the new types assigned.";
            TaskDialog.Show("JNN - CSI x Revit", msg);
        }

        private ElementType CreateOrUpdateType(CreateSectionItem item, string targetName)
        {
            if (item.SelectedTemplate is FamilySymbol symbolTemplate)
            {
                FamilySymbol targetSymbol = null;
                var family = symbolTemplate.Family;

                foreach (ElementId id in family.GetFamilySymbolIds())
                {
                    var s = _doc.GetElement(id) as FamilySymbol;
                    if (s != null && s.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                    {
                        targetSymbol = s;
                        break;
                    }
                }

                if (targetSymbol == null)
                {
                    targetSymbol = symbolTemplate.Duplicate(targetName) as FamilySymbol;
                }

                if (targetSymbol != null)
                {
                    SetSymbolDimensionParameters(targetSymbol, item.IsCircular, item.WidthMm, item.DepthMm);
                }
                return targetSymbol;
            }

            if (item.SelectedTemplate is FloorType floorTemplate)
            {
                bool wantFoundation = item.ElementType == ElementKinds.FoundationSlab;
                // IsFoundationSlab read-only: chỉ nhân bản từ type cùng loại mới ra đúng category.
                if (wantFoundation && !floorTemplate.IsFoundationSlab)
                    throw new InvalidOperationException("Base type for a foundation slab (F) must be a Foundation Slab");
                if (!wantFoundation && floorTemplate.IsFoundationSlab)
                    throw new InvalidOperationException("Base type for a slab (S) must be a regular Floor");

                // Collector mới (không dùng cache _mapper) để thấy cả type vừa tạo trong cùng Transaction.
                FloorType targetFloor = new FilteredElementCollector(_doc)
                    .OfClass(typeof(FloorType))
                    .Cast<FloorType>()
                    .FirstOrDefault(f => f.IsFoundationSlab == floorTemplate.IsFoundationSlab &&
                                         f.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                if (targetFloor == null)
                {
                    targetFloor = floorTemplate.Duplicate(targetName) as FloorType;
                }

                if (targetFloor != null)
                {
                    var cs = targetFloor.GetCompoundStructure();
                    if (cs != null && SetTotalThickness(cs, item.DepthMm > 0 ? item.DepthMm : 100))
                    {
                        targetFloor.SetCompoundStructure(cs);
                    }
                }
                return targetFloor;
            }

            if (item.SelectedTemplate is WallType wallTemplate)
            {
                WallType targetWall = new FilteredElementCollector(_doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(w => w.Kind == wallTemplate.Kind &&
                                         w.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));
                if (targetWall == null)
                {
                    targetWall = wallTemplate.Duplicate(targetName) as WallType;
                }

                if (targetWall != null)
                {
                    var cs = targetWall.GetCompoundStructure();
                    if (cs != null && SetTotalThickness(cs, item.DepthMm > 0 ? item.DepthMm : 200))
                    {
                        targetWall.SetCompoundStructure(cs);
                    }
                }
                return targetWall;
            }

            return null;
        }

        /// <summary>
        /// Đặt TỔNG chiều dày bằng cách chỉnh lớp lõi (các lớp hoàn thiện giữ nguyên),
        /// để type tạo ra khớp đúng tiết diện ETABS khi đối soát "đã có" theo kích thước.
        /// </summary>
        private static bool SetTotalThickness(CompoundStructure cs, double totalMm)
        {
            int layerIdx = cs.GetFirstCoreLayerIndex();
            if (layerIdx < 0) layerIdx = 0;
            if (layerIdx >= cs.LayerCount) return false;

            double totalFeet = UnitUtils.ConvertToInternalUnits(totalMm, UnitTypeId.Millimeters);
            double otherFeet = cs.GetWidth() - cs.GetLayerWidth(layerIdx);
            double minFeet = UnitUtils.ConvertToInternalUnits(1.0, UnitTypeId.Millimeters);
            double coreFeet = Math.Max(totalFeet - otherFeet, minFeet);
            cs.SetLayerWidth(layerIdx, coreFeet);
            return true;
        }

        private void RefreshAfterTypeChange(Dictionary<CreateSectionItem, ElementId> createdIds)
        {
            _mapper.Reload();
            _dimReader.Clear();
            BindDefaultTemplateCombos();

            foreach (var row in CreateSectionItems)
            {
                if (createdIds != null && createdIds.TryGetValue(row, out var newId))
                {
                    var types = _mapper.GetTypesForKind(row.ElementType);
                    var created = types.FirstOrDefault(t => t.Id == newId);
                    row.CanEdit = types.Count > 0;
                    if (created != null)
                    {
                        row.SetTemplates(types, created);
                        row.TargetTypeName = created.Name;
                        row.Status = $"✅ Created: {created.Name}";
                    }
                    else
                    {
                        ApplyRowTemplates(row, newId, keepSelection: false);
                    }
                    row.IsMissing = false;
                    row.IsSelected = false;
                    _pinnedTypes[PinKey(row.ElementType, row.SectionName)] = newId;
                }
                else
                {
                    ApplyRowTemplates(row, row.SelectedTemplate?.Id, keepSelection: true);
                }
            }

            RefreshMappingAvailableTypes();
            AutoMatch();
        }

        private static string PinKey(string kind, string sectionName) => (kind ?? "") + "|" + (sectionName ?? "");

        private static void CommitGridEdits(System.Windows.Controls.DataGrid dg)
        {
            // Checkbox/TextBox trong DataGrid có thể còn ở trạng thái edit → commit trước khi đọc dữ liệu.
            try
            {
                dg.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
                dg.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
            }
            catch { }
        }

        #endregion

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
                ElementId prevId = item.SelectedType?.Id;
                var types = _mapper.GetTypesForKind(item.ElementType);
                item.AvailableTypes = types;
                item.SelectedType = prevId != null ? types.FirstOrDefault(t => t.Id == prevId) : null;
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

                _sectionDefs.TryGetValue(item.EtabsSectionName ?? "", out var def);
                bool isSteelOrVoid = IsSteelOrVoidSection(item.EtabsSectionName, def, item.ElementType, null);
                item.IsSteelOrVoid = isSteelOrVoid;

                // 0. Type vừa tạo ở Tab 1 cho chính tiết diện này
                if (_pinnedTypes.TryGetValue(PinKey(item.ElementType, item.EtabsSectionName), out var pinId))
                {
                    var pinned = item.AvailableTypes.FirstOrDefault(t => t.Id == pinId);
                    if (pinned != null)
                    {
                        item.SelectedType = pinned;
                        continue;
                    }
                }

                // 0.5. Nếu là thép I hoặc xà gồ hoặc void, người dùng muốn mặc định là Void_Beam
                if (isSteelOrVoid)
                {
                    var voidMatch = item.AvailableTypes.FirstOrDefault(s =>
                    {
                        string sName = (s.Name ?? "").ToLowerInvariant();
                        string fName = (s.FamilyName ?? "").ToLowerInvariant();
                        return sName.Contains("void") || fName.Contains("void") || sName.Contains("dummy") || fName.Contains("dummy");
                    });

                    if (voidMatch != null)
                    {
                        item.SelectedType = voidMatch;
                        continue;
                    }
                }

                string etabsName = CleanName(item.EtabsSectionName);

                // 1. Exact match (loại trừ Void nếu không phải steel/void)
                var exactMatch = item.AvailableTypes.FirstOrDefault(s =>
                {
                    if (!isSteelOrVoid)
                    {
                        string sName = (s.Name ?? "").ToLowerInvariant();
                        string fName = (s.FamilyName ?? "").ToLowerInvariant();
                        if (sName.Contains("void") || fName.Contains("void")) return false;
                    }
                    return CleanName(s.Name) == etabsName;
                });
                if (exactMatch != null)
                {
                    item.SelectedType = exactMatch;
                    continue;
                }

                // 2. Partial match (loại trừ Void nếu không phải steel/void)
                var partialMatch = item.AvailableTypes.FirstOrDefault(s =>
                {
                    if (!isSteelOrVoid)
                    {
                        string sName = (s.Name ?? "").ToLowerInvariant();
                        string fName = (s.FamilyName ?? "").ToLowerInvariant();
                        if (sName.Contains("void") || fName.Contains("void")) return false;
                    }
                    string sNameClean = CleanName(s.Name);
                    return sNameClean.Contains(etabsName) || etabsName.Contains(sNameClean);
                });

                if (partialMatch != null)
                {
                    item.SelectedType = partialMatch;
                    continue;
                }

                // 3. Smart thickness match for Sàn (S / F) & Vách (Wall)
                if (ElementKinds.IsFloorLike(item.ElementType) || item.ElementType == ElementKinds.Wall)
                {
                    double targetThickMm = 0;
                    if (def != null && def.DepthMm > 0)
                    {
                        targetThickMm = def.DepthMm;
                    }
                    else
                    {
                        var match = Regex.Match(item.EtabsSectionName ?? "", @"\d+");
                        if (match.Success && double.TryParse(match.Value, out double parsed))
                        {
                            targetThickMm = parsed;
                        }
                    }

                    if (targetThickMm > 0)
                    {
                        string thickStr = Math.Round(targetThickMm, 0).ToString();
                        var thickMatch = item.AvailableTypes.FirstOrDefault(s =>
                        {
                            string sName = CleanName(s.Name);
                            return sName.Contains(thickStr);
                        });

                        if (thickMatch != null)
                        {
                            item.SelectedType = thickMatch;
                            continue;
                        }
                    }
                }
                // 4. Smart dimension match for Cột (Column) & Dầm (Beam)
                else if (item.ElementType == ElementKinds.Column || item.ElementType == ElementKinds.Beam)
                {
                    if (def != null && def.WidthMm > 0 && def.DepthMm > 0)
                    {
                        string wStr = Math.Round(def.WidthMm, 0).ToString();
                        string dStr = Math.Round(def.DepthMm, 0).ToString();

                        var dimMatch = item.AvailableTypes.FirstOrDefault(s =>
                        {
                            if (!isSteelOrVoid)
                            {
                                string sName = (s.Name ?? "").ToLowerInvariant();
                                string fName = (s.FamilyName ?? "").ToLowerInvariant();
                                if (sName.Contains("void") || fName.Contains("void")) return false;
                            }
                            string sNameClean = CleanName(s.Name);
                            return sNameClean.Contains(wStr) && sNameClean.Contains(dStr);
                        });

                        if (dimMatch != null)
                        {
                            item.SelectedType = dimMatch;
                            continue;
                        }
                    }
                    else
                    {
                        var match = Regex.Match(item.EtabsSectionName ?? "", @"(\d+)[xX*_-](\d+)");
                        if (match.Success)
                        {
                            string g1 = match.Groups[1].Value;
                            string g2 = match.Groups[2].Value;
                            var dimMatch = item.AvailableTypes.FirstOrDefault(s =>
                            {
                                if (!isSteelOrVoid)
                                {
                                    string sName = (s.Name ?? "").ToLowerInvariant();
                                    string fName = (s.FamilyName ?? "").ToLowerInvariant();
                                    if (sName.Contains("void") || fName.Contains("void")) return false;
                                }
                                string sNameClean = CleanName(s.Name);
                                return sNameClean.Contains(g1) && sNameClean.Contains(g2);
                            });
                            if (dimMatch != null)
                            {
                                item.SelectedType = dimMatch;
                                continue;
                            }
                        }
                    }
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
            CommitGridEdits(DgMapping);
            AutoMatch();
        }

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgCreateSections);
            CommitGridEdits(DgMapping);
            if (DgLevels != null) CommitGridEdits(DgLevels);
            // Đọc lại document cũng đi qua API context cho an toàn (modeless).
            RunInRevit(() => RefreshAfterTypeChange(null));
        }

        private void BtnSelectAllMapping_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgMapping);
            bool anyUnchecked = MappingItems.Any(x => !x.IsSelected);
            foreach (var item in MappingItems) item.IsSelected = anyUnchecked;
        }

        private void BtnDeselectAllMapping_Click(object sender, RoutedEventArgs e)
        {
            CommitGridEdits(DgMapping);
            foreach (var item in MappingItems) item.IsSelected = false;
        }

        private void BtnAddLevelPrefix_Click(object sender, RoutedEventArgs e)
        {
            if (DgLevels != null) CommitGridEdits(DgLevels);
            foreach (var item in LevelItems)
            {
                if (Regex.IsMatch(item.EtabsStoryName, @"^[Tt]?(\d+)$"))
                {
                    var m = Regex.Match(item.EtabsStoryName, @"^[Tt]?(\d+)$");
                    item.TargetLevelName = "Tầng " + m.Groups[1].Value;
                }
                else if (item.EtabsStoryName.Equals("M", StringComparison.OrdinalIgnoreCase) || item.EtabsStoryName.Equals("ROOF", StringComparison.OrdinalIgnoreCase))
                {
                    item.TargetLevelName = "Tầng Mái";
                }
                else if (!item.TargetLevelName.StartsWith("Tầng ", StringComparison.OrdinalIgnoreCase))
                {
                    item.TargetLevelName = "Tầng " + item.TargetLevelName;
                }
            }
        }

        private void BtnResetLevelNames_Click(object sender, RoutedEventArgs e)
        {
            if (DgLevels != null) CommitGridEdits(DgLevels);
            foreach (var item in LevelItems)
            {
                item.TargetLevelName = item.EtabsStoryName;
                item.MappedLevelName = "";
            }
        }

        private void BtnSelectAllLevels_Click(object sender, RoutedEventArgs e)
        {
            if (DgLevels != null) CommitGridEdits(DgLevels);
            bool anyUnchecked = LevelItems.Any(x => !x.IsSelected);
            foreach (var item in LevelItems) item.IsSelected = anyUnchecked;
        }

        private void BtnDeselectAllLevels_Click(object sender, RoutedEventArgs e)
        {
            if (DgLevels != null) CommitGridEdits(DgLevels);
            foreach (var item in LevelItems) item.IsSelected = false;
        }

        private bool IsCategoryEnabled(string kind)
        {
            if (kind == ElementKinds.Column) return ImportColumns;
            if (kind == ElementKinds.Beam) return ImportBeams;
            if (kind == ElementKinds.Wall) return ImportWalls;
            if (kind == ElementKinds.Slab || kind == ElementKinds.FoundationSlab) return ImportFloors;
            return true;
        }

        private void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            if (_hasImported)
            {
                SafeClose();
                return;
            }

            CommitGridEdits(DgMapping);
            if (DgLevels != null) CommitGridEdits(DgLevels);

            // Verify if any active section marked for modeling is unmapped (sàn ảo cố ý bỏ qua nên không cảnh báo)
            var unmapped = MappingItems.Where(x =>
                x.IsSelected &&
                x.SelectedType == null &&
                x.ElementType != ElementKinds.VirtualSlab &&
                IsCategoryEnabled(x.ElementType)
            ).ToList();

            if (unmapped.Count > 0)
            {
                string unmappedNames = string.Join(", ", unmapped.Take(5).Select(x => $"{x.EtabsSectionName} ({x.ElementType})"));
                if (unmapped.Count > 5) unmappedNames += $", ... (+{unmapped.Count - 5})";

                var res = MessageBox.Show(this,
                    $"{unmapped.Count} cấu kiện / tiết diện đã chọn chưa được gán loại Family trong Revit:\n{unmappedNames}\n\nCác cấu kiện này sẽ bị BỎ QUA khi dựng mô hình.\n\n• Bấm 'No' để quay lại gán Family (hoặc sang Tab 1 để tạo tiết diện thiếu).\n• Bỏ tick cột 'Model' nếu bạn không muốn dựng các tiết diện này.\n• Bấm 'Yes' nếu bạn đồng ý tiếp tục và bỏ qua chúng.",
                    "JNN - CSI x Revit", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (res != MessageBoxResult.Yes) return;
            }

            if (_importAction == null)
            {
                SafeClose();
                return;
            }

            RunInRevit(() =>
            {
                bool success = false;
                try
                {
                    success = _importAction?.Invoke() ?? false;
                }
                catch (Exception ex)
                {
                    JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi dựng hình", ex);
                    TaskDialog.Show("JNN - CSI x Revit", $"An error occurred while building the model: {ex.Message}");
                    return;
                }

                if (success)
                {
                    _hasImported = true;
                    SafeClose();
                }
            });
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            SafeClose();
        }

        private Dictionary<string, ElementType> BuildMapping(Func<SectionMappingItem, bool> predicate)
        {
            // GroupBy để không vỡ ToDictionary nếu một tên tiết diện xuất hiện 2 lần.
            return MappingItems
                .Where(x => x.IsSelected && predicate(x) && x.SelectedType != null && !string.IsNullOrEmpty(x.EtabsSectionName))
                .GroupBy(x => x.EtabsSectionName)
                .ToDictionary(g => g.Key, g => g.First().SelectedType);
        }

        public Dictionary<string, ElementType> GetBeamMapping()
        {
            return BuildMapping(x => x.ElementType == ElementKinds.Beam);
        }

        public Dictionary<string, ElementType> GetColumnMapping()
        {
            return BuildMapping(x => x.ElementType == ElementKinds.Column);
        }

        /// <summary>Gồm cả sàn S và sàn móng F; sàn ảo không bao giờ được map.</summary>
        public Dictionary<string, ElementType> GetFloorMapping()
        {
            return BuildMapping(x => x.ElementType == ElementKinds.Slab || x.ElementType == ElementKinds.FoundationSlab);
        }

        public Dictionary<string, ElementType> GetWallMapping()
        {
            return BuildMapping(x => x.ElementType == ElementKinds.Wall);
        }

        public ElementId GetSelectedLevelId()
        {
            if (CmbLevels.SelectedItem is Level level) return level.Id;
            return ElementId.InvalidElementId;
        }
    }

    public class CreateSectionItem : INotifyPropertyChanged
    {
        private const string UnknownFamily = "(Unknown family)";

        private bool _isSelected = true;
        private bool _canEdit = true;
        private bool _isSteelOrVoid;
        private string _status = "Not in Revit yet";
        private string _targetTypeName = "";
        private List<ElementType> _allTemplates = new List<ElementType>();
        private List<string> _availableFamilies = new List<string>();
        private List<ElementType> _availableTemplates = new List<ElementType>();
        private string _selectedFamily;
        private ElementType _selectedTemplate;

        // Khi đổi ItemsSource, ComboBox WPF đẩy null ngược về binding → bỏ qua null trong lúc đang nạp lại.
        private bool _suppressNull;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public bool CanEdit
        {
            get => _canEdit;
            set { _canEdit = value; OnPropertyChanged(nameof(CanEdit)); }
        }

        private bool _isMissing = true;
        public bool IsMissing
        {
            get => _isMissing;
            set
            {
                _isMissing = value;
                OnPropertyChanged(nameof(IsMissing));
                OnPropertyChanged(nameof(TextColor));
            }
        }
        public string SectionName { get; set; } = "";
        public string ElementType { get; set; } = "";
        public string DimensionSummary { get; set; } = "";
        public double WidthMm { get; set; }
        public double DepthMm { get; set; }
        public bool IsCircular { get; set; }

        public bool IsSteelOrVoid
        {
            get
            {
                // Chỉ áp dụng cho Beam và Column. Wall, Slab, FoundationSlab tuyệt đối không phải steel/void!
                if (ElementType != ElementKinds.Beam && ElementType != ElementKinds.Column)
                    return false;

                if (!string.IsNullOrEmpty(_selectedFamily) &&
                    (_selectedFamily.IndexOf("void", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     _selectedFamily.IndexOf("dummy", StringComparison.OrdinalIgnoreCase) >= 0))
                    return true;

                if (_selectedTemplate != null)
                {
                    string tName = (_selectedTemplate.Name ?? "").ToLowerInvariant();
                    string fName = (_selectedTemplate.FamilyName ?? "").ToLowerInvariant();
                    if (tName.Contains("void") || fName.Contains("void") || tName.Contains("dummy") || fName.Contains("dummy"))
                        return true;
                }
                return _isSteelOrVoid;
            }
            set
            {
                _isSteelOrVoid = value;
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public string TextColor
        {
            get
            {
                if (IsSteelOrVoid) return "#DC2626"; // Thép / Void: ĐỎ
                if (IsMissing) return "#0284C7";     // Tạo mới tiết diện: XANH DƯƠNG
                return "#0F172A";                   // Đã có sẵn: ĐEN
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                _status = value ?? "";
                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(StatusLevel));
                OnPropertyChanged(nameof(StatusText));
            }
        }

        // WPF (.NET Framework) vẽ emoji đơn sắc → suy ra mức trạng thái từ tiền tố để XAML tô màu huy hiệu.
        public string StatusLevel
        {
            get
            {
                if (_status.StartsWith("✅", StringComparison.Ordinal)) return "Ok";
                if (_status.StartsWith("⚠", StringComparison.Ordinal)) return "Missing";
                if (_status.StartsWith("⏭", StringComparison.Ordinal)) return "Skip";
                if (_status.StartsWith("❌", StringComparison.Ordinal)) return "Error";
                return "Missing";
            }
        }

        public string StatusText
        {
            get
            {
                string s = _status;
                foreach (var prefix in new[] { "✅", "⚠️", "⚠", "⏭️", "⏭", "❌" })
                {
                    if (s.StartsWith(prefix, StringComparison.Ordinal)) { s = s.Substring(prefix.Length); break; }
                }
                return s.Trim();
            }
        }

        /// <summary>Tên type sẽ tạo trong Revit. Mặc định = tên tiết diện ETABS.</summary>
        public string TargetTypeName
        {
            get => _targetTypeName;
            set { _targetTypeName = value; OnPropertyChanged(nameof(TargetTypeName)); }
        }

        public List<string> AvailableFamilies
        {
            get => _availableFamilies;
            private set { _availableFamilies = value; OnPropertyChanged(nameof(AvailableFamilies)); }
        }

        public List<ElementType> AvailableTemplates
        {
            get => _availableTemplates;
            private set { _availableTemplates = value; OnPropertyChanged(nameof(AvailableTemplates)); }
        }

        public string SelectedFamily
        {
            get => _selectedFamily;
            set
            {
                if (value == null && _suppressNull) return;
                if (string.Equals(value, _selectedFamily, StringComparison.Ordinal)) return;

                _selectedFamily = value;
                OnPropertyChanged(nameof(SelectedFamily));
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));

                var filtered = FilterByFamily(value);
                var keep = _selectedTemplate != null && filtered.Contains(_selectedTemplate) ? _selectedTemplate : filtered.FirstOrDefault();
                UpdateTemplateList(filtered, keep);
            }
        }

        public ElementType SelectedTemplate
        {
            get => _selectedTemplate;
            set
            {
                if (value == null && _suppressNull) return;
                if (ReferenceEquals(value, _selectedTemplate)) return;

                if (value != null)
                {
                    string fam = GetFamilyName(value);
                    if (!string.Equals(fam, _selectedFamily, StringComparison.Ordinal))
                    {
                        // Type thuộc family khác (vd. Áp dụng Template) → đổi luôn family và lọc lại danh sách.
                        _selectedFamily = fam;
                        OnPropertyChanged(nameof(SelectedFamily));
                        OnPropertyChanged(nameof(IsSteelOrVoid));
                        OnPropertyChanged(nameof(TextColor));
                        UpdateTemplateList(FilterByFamily(fam), value);
                        return;
                    }
                }

                _selectedTemplate = value;
                OnPropertyChanged(nameof(SelectedTemplate));
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        /// <summary>Nạp toàn bộ type ứng viên cho dòng, chọn sẵn <paramref name="selected"/>.</summary>
        public void SetTemplates(List<ElementType> all, ElementType selected)
        {
            _allTemplates = all ?? new List<ElementType>();
            var families = _allTemplates.Select(GetFamilyName).Distinct().OrderBy(x => x).ToList();
            string family = selected != null ? GetFamilyName(selected) : families.FirstOrDefault();

            _suppressNull = true;
            try
            {
                _selectedFamily = family;
                AvailableFamilies = families;
                OnPropertyChanged(nameof(SelectedFamily));
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
            finally
            {
                _suppressNull = false;
            }

            var filtered = FilterByFamily(family);
            UpdateTemplateList(filtered, selected ?? filtered.FirstOrDefault());
        }

        public void SelectTemplateById(ElementId id)
        {
            if (id == null) return;
            var t = _allTemplates.FirstOrDefault(x => x.Id == id);
            if (t != null) SelectedTemplate = t;
        }

        private void UpdateTemplateList(List<ElementType> list, ElementType selected)
        {
            _suppressNull = true;
            try
            {
                _selectedTemplate = selected;
                AvailableTemplates = list;
                OnPropertyChanged(nameof(SelectedTemplate));
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
            finally
            {
                _suppressNull = false;
            }
        }

        private List<ElementType> FilterByFamily(string family)
        {
            if (family == null) return new List<ElementType>();
            return _allTemplates
                .Where(t => string.Equals(GetFamilyName(t), family, StringComparison.Ordinal))
                .OrderBy(t => t.Name)
                .ToList();
        }

        private static string GetFamilyName(ElementType t)
        {
            try
            {
                string name = t?.FamilyName;
                return string.IsNullOrEmpty(name) ? UnknownFamily : name;
            }
            catch
            {
                return UnknownFamily;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class SectionMappingItem : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        private ElementType _selectedType;
        private List<ElementType> _availableTypes;
        private bool _isSteelOrVoid;

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public string EtabsSectionName { get; set; }
        public string SectionDetails { get; set; }
        public string ElementType { get; set; }
        public int ObjectCount { get; set; }

        public ElementType SelectedType
        {
            get => _selectedType;
            set
            {
                _selectedType = value;
                OnPropertyChanged(nameof(SelectedType));
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public List<ElementType> AvailableTypes
        {
            get => _availableTypes;
            set { _availableTypes = value; OnPropertyChanged(nameof(AvailableTypes)); }
        }

        public bool IsSteelOrVoid
        {
            get
            {
                // Chỉ áp dụng cho Beam và Column. Wall, Slab, FoundationSlab tuyệt đối không phải steel/void!
                if (ElementType != ElementKinds.Beam && ElementType != ElementKinds.Column)
                    return false;

                if (_selectedType != null)
                {
                    string name = (_selectedType.Name ?? "").ToLowerInvariant();
                    string fam = (_selectedType.FamilyName ?? "").ToLowerInvariant();
                    if (name.Contains("void") || fam.Contains("void") || name.Contains("dummy") || fam.Contains("dummy"))
                        return true;
                }
                return _isSteelOrVoid;
            }
            set
            {
                _isSteelOrVoid = value;
                OnPropertyChanged(nameof(IsSteelOrVoid));
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public string TextColor
        {
            get
            {
                if (IsSteelOrVoid) return "#DC2626";       // Thép / Void: ĐỎ
                if (SelectedType == null) return "#0284C7"; // Chưa khớp: XANH DƯƠNG
                return "#0F172A";                          // Đã khớp: ĐEN
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class FrameAndColumnSelectionFilter : Autodesk.Revit.UI.Selection.ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            if (elem is FamilyInstance fi)
            {
                var cat = fi.Category;
                if (cat != null)
                {
                    return cat.Id == new ElementId(BuiltInCategory.OST_StructuralFraming) 
                        || cat.Id == new ElementId(BuiltInCategory.OST_StructuralColumns);
                }
            }
            return false;
        }

        public bool AllowReference(Reference reference, XYZ position) => false;
    }
}
