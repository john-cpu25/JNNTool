#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.Compat;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.Tools.ElementMark.Engine;
using JNNTool.Tools.ElementMark.Models;

namespace JNNTool.Tools.ElementMark.ViewModels
{
    public class Option<T>
    {
        public T Value { get; }
        public string Label { get; }
        public Option(T value, string label) { Value = value; Label = label; }
    }

    public partial class LevelCodeRow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public string Name { get; }
        public double ElevationMm { get; }
        public string ElevationText => ElevationMm.ToString("+0;-0;0", CultureInfo.InvariantCulture);
        public string Suggested { get; }

        [ObservableProperty] private string _code;

        public LevelCodeRow(string name, double elevationMm, string code)
        {
            Name = name;
            ElevationMm = elevationMm;
            Suggested = LevelCodeResolver.Suggest(name);
            _code = code;
        }
    }

    public partial class MarkPreviewRow : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        public long Id { get; set; }
        public MarkCategory Category { get; set; }
        public string CategoryLabel { get; set; } = "";
        public string LevelName { get; set; } = "";
        public double LevelElevation { get; set; }
        public string Axis { get; set; } = "";
        public int Number { get; set; }
        public int Span { get; set; }
        public string TypeName { get; set; } = "";
        public string NewMark { get; set; } = "";

        [ObservableProperty] private string _oldMark = "";
        [ObservableProperty] private string _status = "";
        [ObservableProperty] private bool _willWrite;
    }

    public partial class ElementMarkViewModel : CommunityToolkit.Mvvm.ComponentModel.ObservableObject
    {
        // ── Chung ─────────────────────────────────────────────
        [ObservableProperty] private MarkScope _scope;
        [ObservableProperty] private OverwriteMode _overwrite;
        [ObservableProperty] private string _targetParameter = "Mark";

        // ── Dầm ───────────────────────────────────────────────
        [ObservableProperty] private bool _beamEnabled;
        [ObservableProperty] private string _beamPrefix = "";
        [ObservableProperty] private string _beamTemplate = "";
        [ObservableProperty] private string _beamSpanSuffix = "";
        [ObservableProperty] private string _beamAxisXCode = "";
        [ObservableProperty] private string _beamAxisYCode = "";
        [ObservableProperty] private int _beamStartNumber;
        [ObservableProperty] private VerticalOrder _beamVertical;
        [ObservableProperty] private HorizontalOrder _beamHorizontal;
        [ObservableProperty] private bool _beamMergeSpans;
        [ObservableProperty] private double _beamChainGapMm;
        [ObservableProperty] private double _beamRowToleranceMm;

        // ── Cột ───────────────────────────────────────────────
        [ObservableProperty] private bool _columnEnabled;
        [ObservableProperty] private string _columnPrefix = "";
        [ObservableProperty] private string _columnTemplate = "";
        [ObservableProperty] private int _columnStartNumber;
        [ObservableProperty] private VerticalOrder _columnVertical;
        [ObservableProperty] private HorizontalOrder _columnHorizontal;
        [ObservableProperty] private ColumnPriority _columnPriority;
        [ObservableProperty] private double _columnStackToleranceMm;
        [ObservableProperty] private double _columnRowToleranceMm;

        // ── Sàn ───────────────────────────────────────────────
        [ObservableProperty] private bool _floorEnabled;
        [ObservableProperty] private string _floorPrefix = "";
        [ObservableProperty] private string _floorTemplate = "";
        [ObservableProperty] private bool _floorStructuralOnly;

        // ── Hiển thị ──────────────────────────────────────────
        [ObservableProperty] private string _beamExample = "";
        [ObservableProperty] private string _columnExample = "";
        [ObservableProperty] private string _floorExample = "";
        [ObservableProperty] private string _statusMessage = "Thiết lập quy tắc rồi bấm Xem trước.";
        [ObservableProperty] private string _previewSummary = "Chưa có dữ liệu xem trước";
        [ObservableProperty] private bool _isBusy;
        [ObservableProperty] private MarkPreviewRow? _selectedPreviewRow;
        [ObservableProperty] private string _previewFilter = "Tất cả";

        public ObservableCollection<LevelCodeRow> LevelCodes { get; } = new ObservableCollection<LevelCodeRow>();
        public ObservableCollection<MarkPreviewRow> PreviewRows { get; } = new ObservableCollection<MarkPreviewRow>();
        public ICollectionView PreviewView { get; }

        public IReadOnlyList<Option<MarkScope>> ScopeOptions { get; } = new[]
        {
            new Option<MarkScope>(MarkScope.ActiveView, "Active View"),
            new Option<MarkScope>(MarkScope.Project, "Entire Project")
        };

        public IReadOnlyList<Option<OverwriteMode>> OverwriteOptions { get; } = new[]
        {
            new Option<OverwriteMode>(OverwriteMode.All, "Overwrite All"),
            new Option<OverwriteMode>(OverwriteMode.EmptyOnly, "Empty Only")
        };

        public IReadOnlyList<Option<VerticalOrder>> VerticalOptions { get; } = new[]
        {
            new Option<VerticalOrder>(VerticalOrder.TopToBottom, "Top → Bottom"),
            new Option<VerticalOrder>(VerticalOrder.BottomToTop, "Bottom → Top")
        };

        public IReadOnlyList<Option<HorizontalOrder>> HorizontalOptions { get; } = new[]
        {
            new Option<HorizontalOrder>(HorizontalOrder.LeftToRight, "Left → Right"),
            new Option<HorizontalOrder>(HorizontalOrder.RightToLeft, "Right → Left")
        };

        public IReadOnlyList<Option<ColumnPriority>> PriorityOptions { get; } = new[]
        {
            new Option<ColumnPriority>(ColumnPriority.RowFirst, "By Row (Horizontal first)"),
            new Option<ColumnPriority>(ColumnPriority.ColumnFirst, "By Column (Vertical first)")
        };

        public IReadOnlyList<string> FilterOptions { get; } = new[] { "All", "Beam", "Column", "Floor", "Will Update" };

        private ElementMarkSettings _settings;
        private bool _suspendExamples;

        private static readonly HashSet<string> NonSettingProps = new HashSet<string>
        {
            nameof(BeamExample), nameof(ColumnExample), nameof(FloorExample),
            nameof(StatusMessage), nameof(PreviewSummary), nameof(IsBusy),
            nameof(SelectedPreviewRow), nameof(PreviewFilter)
        };

        public ElementMarkViewModel(IEnumerable<LevelInfo> levels)
        {
            _settings = SettingsService<ElementMarkSettings>.Load();
            PreviewView = CollectionViewSource.GetDefaultView(PreviewRows);
            PreviewView.Filter = FilterRow;

            _suspendExamples = true;
            ApplySettings(_settings);
            SetLevels(levels);
            _suspendExamples = false;
            UpdateExamples();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Commands
        // ═══════════════════════════════════════════════════════════════

        [RelayCommand]
        private void Preview() => Run(false);

        [RelayCommand]
        private void Apply() => Run(true);

        [RelayCommand]
        private void ResetDefaults()
        {
            var levelOverrides = _settings.LevelCodes;
            _settings = SettingsService<ElementMarkSettings>.Reset();
            _settings.LevelCodes = levelOverrides;
            _suspendExamples = true;
            ApplySettings(_settings);
            _suspendExamples = false;
            UpdateExamples();
            StatusMessage = "Settings reset to defaults (level codes preserved).";
        }

        [RelayCommand]
        private void SuggestLevelCodes()
        {
            _suspendExamples = true;
            foreach (var row in LevelCodes) row.Code = row.Suggested;
            _suspendExamples = false;
            UpdateExamples();
            StatusMessage = "Level codes reset to automatic suggestions.";
        }

        [RelayCommand]
        private void ReloadLevels()
        {
            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var doc = app.ActiveUIDocument?.Document;
                    if (doc == null) { StatusMessage = "No active Revit document."; return; }
                    SaveLevelOverrides();
                    SetLevels(ElementMarkService.ReadLevels(doc));
                    UpdateExamples();
                    StatusMessage = $"Loaded {LevelCodes.Count} levels from '{doc.Title}'.";
                }
                catch (Exception ex)
                {
                    Logger.Error("Element Mark: error loading levels", ex);
                    StatusMessage = "Error loading levels: " + ex.Message;
                }
            });
        }

        partial void OnSelectedPreviewRowChanged(MarkPreviewRow? value)
        {
            if (value == null || IsBusy) return;
            long id = value.Id;
            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var uidoc = app.ActiveUIDocument;
                    if (uidoc == null) return;
                    var eid = ElementIdCompat.Create(id);
                    if (uidoc.Document.GetElement(eid) == null) return;
                    uidoc.Selection.SetElementIds(new List<ElementId> { eid });
                }
                catch (Exception ex)
                {
                    Logger.Error("Element Mark: error selecting element", ex);
                }
            });
        }

        partial void OnPreviewFilterChanged(string value) => PreviewView.Refresh();

        // ═══════════════════════════════════════════════════════════════
        //  Run (Preview / Apply)
        // ═══════════════════════════════════════════════════════════════

        private void Run(bool write)
        {
            if (IsBusy) return;

            var s = BuildSettings();
            if (!s.BeamEnabled && !s.ColumnEnabled && !s.FloorEnabled)
            {
                StatusMessage = "No category selected (Beam / Column / Floor).";
                return;
            }
            SettingsService<ElementMarkSettings>.Save(s);
            _settings = s;

            var codes = CurrentLevelCodes();
            var elevations = LevelCodes.GroupBy(r => r.Name).ToDictionary(g => g.Key, g => g.First().ElevationMm);

            IsBusy = true;
            StatusMessage = write ? "Applying marks..." : "Generating preview...";

            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    var uidoc = app.ActiveUIDocument;
                    if (uidoc == null) { StatusMessage = "No active Revit document."; return; }
                    var doc = uidoc.Document;
                    var view = doc.ActiveView;

                    if (s.Scope == MarkScope.ActiveView && (view == null || view is ViewSheet || view is ViewSchedule))
                    {
                        StatusMessage = "Active view does not support element marking. Please open a plan view or select 'Entire Project'.";
                        TaskDialog.Show("JNN - Element Mark", StatusMessage);
                        return;
                    }

                    var result = ElementMarkService.Run(doc, view!, s, codes, write);
                    FillPreview(result, s, elevations);

                    int nb = result.Data.Beams.Count, nc = result.Data.Columns.Count, nf = result.Data.Floors.Count;
                    string frame = result.Data.UsedViewFrame ? "oriented by active view" : "oriented by project north";

                    if (result.Assignments.Count == 0)
                    {
                        StatusMessage = "No elements found in selected scope.";
                        return;
                    }

                    if (!write)
                    {
                        StatusMessage = $"Preview: {result.Assignments.Count} elements (Beam: {nb}, Column: {nc}, Floor: {nf}), {frame}. Click 'Apply' to execute.";
                        return;
                    }

                    var rep = result.Report!;
                    foreach (var row in PreviewRows.Where(r => r.WillWrite))
                    {
                        row.OldMark = row.NewMark;
                        row.Status = "Applied";
                        row.WillWrite = false;
                    }

                    StatusMessage = $"Updated {rep.Written} marks, unchanged: {rep.Unchanged}, skipped: {rep.SkippedHasMark}, errors: {rep.Failed} ({result.ElapsedMs} ms).";
                    string msg = $"Element marks updated successfully for {rep.Written} elements.\n\n" +
                                 $"• Beam: {nb}   • Column: {nc}   • Floor: {nf}\n" +
                                 $"• Unchanged (Mark matches): {rep.Unchanged}\n" +
                                 $"• Skipped (already has Mark): {rep.SkippedHasMark}\n" +
                                 $"• Errors: {rep.Failed}\n" +
                                 $"• Elapsed: {result.ElapsedMs} ms";
                    if (rep.Failed > 0) msg += $"\n\nFirst error: {rep.FirstError}";
                    TaskDialog.Show("JNN - Element Mark", msg);
                }
                catch (Exception ex)
                {
                    Logger.Error("Element Mark: execution error", ex);
                    StatusMessage = "Error: " + ex.Message;
                    TaskDialog.Show("JNN - Element Mark", $"Unable to mark elements:\n\n{ex.Message}\n\nSee log file for details.");
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                    IsBusy = false;
                }
            });
        }

        private void FillPreview(MarkRunResult result, ElementMarkSettings s, Dictionary<string, double> elevations)
        {
            var rows = new List<MarkPreviewRow>(result.Assignments.Count);
            foreach (var a in result.Assignments)
            {
                result.Data.Info.TryGetValue(a.Id, out var info);
                string old = info?.OldMark ?? "";
                bool keep = s.Overwrite == OverwriteMode.EmptyOnly && old.Trim().Length > 0;
                bool same = old == a.Mark;

                rows.Add(new MarkPreviewRow
                {
                    Id = a.Id,
                    Category = a.Category,
                    CategoryLabel = a.Category == MarkCategory.Beam ? "Beam" : a.Category == MarkCategory.Column ? "Column" : "Floor",
                    LevelName = a.LevelName ?? "",
                    LevelElevation = elevations.TryGetValue(a.LevelName ?? "", out var z) ? z : double.MaxValue,
                    Axis = a.Axis,
                    Number = a.Number,
                    Span = a.Span,
                    TypeName = info?.TypeName ?? "",
                    NewMark = a.Mark,
                    OldMark = old,
                    WillWrite = !keep && !same,
                    Status = keep ? "Skipped" : same ? "Unchanged" : old.Length == 0 ? "New" : "Modified"
                });
            }

            PreviewRows.Clear();
            foreach (var r in rows
                         .OrderBy(r => r.Category)
                         .ThenBy(r => r.Category == MarkCategory.Column ? 0 : r.LevelElevation)
                         .ThenBy(r => r.Axis)
                         .ThenBy(r => r.Number)
                         .ThenBy(r => r.Span)
                         .ThenBy(r => r.Category == MarkCategory.Column ? r.LevelElevation : 0))
            {
                PreviewRows.Add(r);
            }

            int write = rows.Count(r => r.WillWrite);
            PreviewSummary = $"{rows.Count} elements · {write} will update · {rows.Count - write} unchanged / kept";
        }

        private bool FilterRow(object obj)
        {
            if (!(obj is MarkPreviewRow r)) return false;
            switch (PreviewFilter)
            {
                case "Beam": return r.Category == MarkCategory.Beam;
                case "Column": return r.Category == MarkCategory.Column;
                case "Floor": return r.Category == MarkCategory.Floor;
                case "Will Update": return r.WillWrite;
                default: return true;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Examples
        // ═══════════════════════════════════════════════════════════════

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (_suspendExamples || e.PropertyName == null || NonSettingProps.Contains(e.PropertyName)) return;
            UpdateExamples();
        }

        private void OnLevelRowChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (!_suspendExamples) UpdateExamples();
        }

        private void UpdateExamples()
        {
            try
            {
                var s = BuildSettings();
                var codes = CurrentLevelCodes();
                string level = LevelCodes.FirstOrDefault(r => r.Code == "1")?.Code
                               ?? LevelCodes.FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.Code))?.Code
                               ?? "1";

                // Simulation: row 1 has 1-span beam, row 2 has continuous 2-span beam, plus 1 Y beam.
                var sample = new List<BeamItem>
                {
                    new BeamItem(1, "_", 0, 10000, 6000, 10000),
                    new BeamItem(2, "_", 0, 0, 6000, 0),
                    new BeamItem(3, "_", 6000, 0, 12000, 0),
                    new BeamItem(4, "_", 0, 0, 0, 10000)
                };
                var beamMarks = ElementMarkEngine.MarkBeams(sample, s, new Dictionary<string, string> { { "_", level } })
                    .Select(a => a.Mark).ToList();
                BeamExample = string.Join("   ", beamMarks) + Warn(s.BeamTemplate);

                var colMarks = Enumerable.Range(0, 3)
                    .Select(i => MarkTemplateEngine.Format(s.ColumnTemplate, new MarkTokens
                    {
                        Prefix = s.ColumnPrefix,
                        Level = level,
                        Number = s.ColumnStartNumber + i
                    }));
                ColumnExample = string.Join("   ", colMarks) + "   (shared from foundation to roof)" + Warn(s.ColumnTemplate);

                var floorSamples = LevelCodes.Take(4).Select(r =>
                    $"{r.Name} → {MarkTemplateEngine.Format(s.FloorTemplate, new MarkTokens { Prefix = s.FloorPrefix, Level = ElementMarkEngine.ResolveLevelCode(codes, r.Name), Number = 1 })}");
                FloorExample = LevelCodes.Count == 0
                    ? MarkTemplateEngine.Format(s.FloorTemplate, new MarkTokens { Prefix = s.FloorPrefix, Level = level })
                    : string.Join("\n", floorSamples);
            }
            catch (Exception ex)
            {
                Logger.Error("Element Mark: example generation error", ex);
            }
        }

        private static string Warn(string template) =>
            MarkTemplateEngine.HasNumberToken(template) ? "" : "\n⚠ Template lacks {N}: all elements will have identical marks.";

        // ═══════════════════════════════════════════════════════════════
        //  Settings ⇄ ViewModel
        // ═══════════════════════════════════════════════════════════════

        private void ApplySettings(ElementMarkSettings s)
        {
            Scope = s.Scope;
            Overwrite = s.Overwrite;
            TargetParameter = s.TargetParameter;

            BeamEnabled = s.BeamEnabled;
            BeamPrefix = s.BeamPrefix;
            BeamTemplate = s.BeamTemplate;
            BeamSpanSuffix = s.BeamSpanSuffix;
            BeamAxisXCode = s.BeamAxisXCode;
            BeamAxisYCode = s.BeamAxisYCode;
            BeamStartNumber = s.BeamStartNumber;
            BeamVertical = s.BeamVertical;
            BeamHorizontal = s.BeamHorizontal;
            BeamMergeSpans = s.BeamMergeSpans;
            BeamChainGapMm = s.BeamChainGapMm;
            BeamRowToleranceMm = s.BeamRowToleranceMm;

            ColumnEnabled = s.ColumnEnabled;
            ColumnPrefix = s.ColumnPrefix;
            ColumnTemplate = s.ColumnTemplate;
            ColumnStartNumber = s.ColumnStartNumber;
            ColumnVertical = s.ColumnVertical;
            ColumnHorizontal = s.ColumnHorizontal;
            ColumnPriority = s.ColumnPriority;
            ColumnStackToleranceMm = s.ColumnStackToleranceMm;
            ColumnRowToleranceMm = s.ColumnRowToleranceMm;

            FloorEnabled = s.FloorEnabled;
            FloorPrefix = s.FloorPrefix;
            FloorTemplate = s.FloorTemplate;
            FloorStructuralOnly = s.FloorStructuralOnly;
        }

        private ElementMarkSettings BuildSettings()
        {
            var s = new ElementMarkSettings
            {
                Scope = Scope,
                Overwrite = Overwrite,
                TargetParameter = string.IsNullOrWhiteSpace(TargetParameter) ? "Mark" : TargetParameter.Trim(),

                BeamEnabled = BeamEnabled,
                BeamPrefix = BeamPrefix ?? "",
                BeamTemplate = BeamTemplate ?? "",
                BeamSpanSuffix = BeamSpanSuffix ?? "",
                BeamAxisXCode = BeamAxisXCode ?? "",
                BeamAxisYCode = BeamAxisYCode ?? "",
                BeamStartNumber = BeamStartNumber,
                BeamVertical = BeamVertical,
                BeamHorizontal = BeamHorizontal,
                BeamMergeSpans = BeamMergeSpans,
                BeamChainGapMm = Math.Max(0, BeamChainGapMm),
                BeamRowToleranceMm = Math.Max(0, BeamRowToleranceMm),

                ColumnEnabled = ColumnEnabled,
                ColumnPrefix = ColumnPrefix ?? "",
                ColumnTemplate = ColumnTemplate ?? "",
                ColumnStartNumber = ColumnStartNumber,
                ColumnVertical = ColumnVertical,
                ColumnHorizontal = ColumnHorizontal,
                ColumnPriority = ColumnPriority,
                ColumnStackToleranceMm = Math.Max(0, ColumnStackToleranceMm),
                ColumnRowToleranceMm = Math.Max(0, ColumnRowToleranceMm),

                FloorEnabled = FloorEnabled,
                FloorPrefix = FloorPrefix ?? "",
                FloorTemplate = FloorTemplate ?? "",
                FloorStructuralOnly = FloorStructuralOnly
            };

            s.LevelCodes = BuildLevelOverrides();
            return s;
        }

        private void SetLevels(IEnumerable<LevelInfo> levels)
        {
            foreach (var row in LevelCodes) row.PropertyChanged -= OnLevelRowChanged;
            LevelCodes.Clear();

            foreach (var l in levels ?? Enumerable.Empty<LevelInfo>())
            {
                string code = _settings.LevelCodes != null && _settings.LevelCodes.TryGetValue(l.Name, out var saved) && !string.IsNullOrWhiteSpace(saved)
                    ? saved
                    : LevelCodeResolver.Suggest(l.Name);
                var row = new LevelCodeRow(l.Name, l.ElevationMm, code);
                row.PropertyChanged += OnLevelRowChanged;
                LevelCodes.Add(row);
            }
        }

        /// <summary>Chỉ lưu mã khác với gợi ý, giữ lại mã của Level thuộc dự án khác.</summary>
        private Dictionary<string, string> BuildLevelOverrides()
        {
            var dict = _settings.LevelCodes != null
                ? new Dictionary<string, string>(_settings.LevelCodes)
                : new Dictionary<string, string>();
            foreach (var row in LevelCodes)
            {
                string code = (row.Code ?? "").Trim();
                if (code.Length == 0 || code == row.Suggested) dict.Remove(row.Name);
                else dict[row.Name] = code;
            }
            return dict;
        }

        private void SaveLevelOverrides()
        {
            _settings.LevelCodes = BuildLevelOverrides();
            SettingsService<ElementMarkSettings>.Save(_settings);
        }

        private Dictionary<string, string> CurrentLevelCodes()
        {
            var dict = new Dictionary<string, string>();
            foreach (var row in LevelCodes) dict[row.Name] = row.Code ?? "";
            return dict;
        }

        /// <summary>Gọi khi đóng cửa sổ để lưu cấu hình và gỡ event.</summary>
        public void Cleanup()
        {
            try
            {
                SettingsService<ElementMarkSettings>.Save(BuildSettings());
                foreach (var row in LevelCodes) row.PropertyChanged -= OnLevelRowChanged;
            }
            catch (Exception ex)
            {
                Logger.Error("Element Mark: lỗi lưu cấu hình khi đóng", ex);
            }
        }
    }
}
