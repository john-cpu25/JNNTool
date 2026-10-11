using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.CSIxRevit.Parser;
using JNNTool.Tools.CSIxRevit.Mapping;
using JNNTool.Tools.CSIxRevit.Builder;
using JNNTool.Core.ExternalEvents;

namespace JNNTool.Tools.CSIxRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ImportE2KCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            if (uiapp?.ActiveUIDocument?.Document == null)
            {
                message = "Please open a Revit project before running this command.";
                return Result.Failed;
            }
            var doc = uiapp.ActiveUIDocument.Document;

            // Modeless: nếu cửa sổ đang mở thì chỉ đưa lên trước, không mở thêm.
            if (JNNTool.Tools.CSIxRevit.UI.ImportWindow.Current != null)
            {
                try
                {
                    var current = JNNTool.Tools.CSIxRevit.UI.ImportWindow.Current;
                    if (current.WindowState == System.Windows.WindowState.Minimized)
                        current.WindowState = System.Windows.WindowState.Normal;
                    current.Activate();
                }
                catch { }
                return Result.Succeeded;
            }

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "ETABS files (*.e2k;*.$et;*.txt)|*.e2k;*.$et;*.txt|All files (*.*)|*.*";
                var revitOwner = new RevitWin32Window(uiapp.MainWindowHandle);
                if (ofd.ShowDialog(revitOwner) == DialogResult.OK)
                {
                    try
                    {
                        var reader = new E2KReader();
                        var tables = reader.Read(ofd.FileName);

                        // Debug log
                        try 
                        {
                            var logPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(ofd.FileName), "EtabsParserLog.txt");
                            var sb = new System.Text.StringBuilder();
                            foreach(var kv in tables)
                            {
                                sb.AppendLine($"TABLE: {kv.Key}");
                                foreach(var line in kv.Value.Take(10)) sb.AppendLine(line);
                                sb.AppendLine();
                            }
                            System.IO.File.WriteAllText(logPath, sb.ToString());
                        } catch { }

                        var parser = new E2KParser();
                        parser.Parse(tables, out var points, out var beams, out var columns, out var walls, out var floors, out var sectionProps, out var stories, out var grids, out var sectionDefs);

                        var mapper = new FamilyMapper(doc);
                        
                        var items = new List<JNNTool.Tools.CSIxRevit.UI.SectionMappingItem>();

                        var beamGroups = beams.GroupBy(b => b.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in beamGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.BeamSymbols);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = JNNTool.Tools.CSIxRevit.Models.ElementKinds.Beam, ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var colGroups = columns.GroupBy(c => c.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in colGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.ColumnSymbols);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = JNNTool.Tools.CSIxRevit.Models.ElementKinds.Column, ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var wallGroups = walls.GroupBy(w => w.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in wallGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.WallTypes);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = JNNTool.Tools.CSIxRevit.Models.ElementKinds.Wall, ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var floorGroups = floors.GroupBy(f => f.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in floorGroups)
                        {
                            sectionDefs.TryGetValue(g.Section ?? "", out var floorDef);
                            var slabKind = JNNTool.Tools.CSIxRevit.Models.SlabClassifier.Classify(floorDef, g.Section);
                            string kind = JNNTool.Tools.CSIxRevit.Models.SlabClassifier.ToElementKind(slabKind);
                            var available = mapper.GetTypesForKind(kind);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = kind, ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        // ExternalEvent riêng cho cửa sổ này, tạo trong API context (chạy được cả khi load qua Add-in Manager).
                        var eventHandler = new ActionEventHandler();
                        eventHandler.Initialize();

                        var window = new JNNTool.Tools.CSIxRevit.UI.ImportWindow(
                            doc, 
                            items, 
                            mapper.Levels,
                            mapper,
                            sectionDefs,
                            eventHandler
                        );

                        bool hasImported = false;
                        GridBuildResult gridResult = new GridBuildResult();
                        int slabBuilt = 0, foundationBuilt = 0, virtualSkipped = 0, foundationRejected = 0;
                        ScopeBoxBuildResult scopeResult = null;
                        bool fitScopeBox = false;

                        bool RunImport()
                        {
                            var beamMap = window.GetBeamMapping();
                            var colMap = window.GetColumnMapping();
                            var floorMap = window.GetFloorMapping();
                            var wallMap = window.GetWallMapping();
                            bool applyCardinalPoint = window.ApplyCardinalPoint;
                            bool applyEdgeAlignment = window.ApplyEdgeAlignment;
                            fitScopeBox = window.FitScopeBox;

                            Dictionary<string, XYZ> nodeOffsets = null;
                            if (applyEdgeAlignment)
                            {
                                nodeOffsets = EdgeAlignmentHelper.CalculateNodeOffsets(points, beams, columns, sectionDefs);
                            }
                            
                            var levelId = window.GetSelectedLevelId();
                            var level = doc.GetElement(levelId) as Level;

                            using (Transaction tx = new Transaction(doc, "JNN - Import ETABS E2K"))
                            {
                                FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new ImportFailurePreprocessor());
                                tx.SetFailureHandlingOptions(options);

                                tx.Start();

                                LevelBuilder.Build(doc, stories);

                                // Regenerate to register new levels before creating grids
                                doc.Regenerate();

                                var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                                var gridIdsBefore = new HashSet<ElementId>(new FilteredElementCollector(doc).OfClass(typeof(Grid)).ToElementIds());
                                gridResult = GridBuilder.Build(doc, grids);

                                if (doc.ActiveView is View3D view3D && !view3D.IsTemplate)
                                {
                                    try
                                    {
                                        var targetLevelId = (levelId != null && levelId != ElementId.InvalidElementId) ? levelId : allLevels.FirstOrDefault()?.Id;
                                        if (targetLevelId != null && targetLevelId != ElementId.InvalidElementId)
                                        {
                                            view3D.ShowGridsOnLevel(targetLevelId);
                                        }
                                    }
                                    catch { }
                                }

                                foreach (var b in beams)
                                {
                                    if (beamMap.TryGetValue(b.Section, out var elemType) && elemType is FamilySymbol sym)
                                    {
                                        var p1 = points.FirstOrDefault(p => p.Name == b.PointI);
                                        var p2 = points.FirstOrDefault(p => p.Name == b.PointJ);
                                        if (p1?.Position != null && p2?.Position != null)
                                        {
                                            XYZ offset1 = null;
                                            XYZ offset2 = null;
                                            if (nodeOffsets != null)
                                            {
                                                nodeOffsets.TryGetValue(b.PointI, out offset1);
                                                nodeOffsets.TryGetValue(b.PointJ, out offset2);

                                                if (offset1 != null || offset2 != null)
                                                {
                                                    double off1X = offset1?.X ?? 0;
                                                    double off1Y = offset1?.Y ?? 0;
                                                    double off2X = offset2?.X ?? 0;
                                                    double off2Y = offset2?.Y ?? 0;

                                                    // Anti-skew guarantee:
                                                    // 1. If beam was parallel to X in E2K (abs(dy) < 0.01), ensure Y offsets match!
                                                    if (Math.Abs(p1.Position.Y - p2.Position.Y) < 0.01)
                                                    {
                                                        double commonY = (Math.Abs(off1Y) > 0.001 && Math.Abs(off2Y) > 0.001)
                                                            ? (off1Y + off2Y) / 2.0
                                                            : (Math.Abs(off1Y) > 0.001 ? off1Y : off2Y);
                                                        off1Y = commonY;
                                                        off2Y = commonY;
                                                    }

                                                    // 2. If beam was parallel to Y in E2K (abs(dx) < 0.01), ensure X offsets match!
                                                    if (Math.Abs(p1.Position.X - p2.Position.X) < 0.01)
                                                    {
                                                        double commonX = (Math.Abs(off1X) > 0.001 && Math.Abs(off2X) > 0.001)
                                                            ? (off1X + off2X) / 2.0
                                                            : (Math.Abs(off1X) > 0.001 ? off1X : off2X);
                                                        off1X = commonX;
                                                        off2X = commonX;
                                                    }

                                                    offset1 = new XYZ(off1X, off1Y, 0);
                                                    offset2 = new XYZ(off2X, off2Y, 0);
                                                }
                                            }
                                            BeamBuilder.Build(doc, b, p1, p2, sym, allLevels, offset1, offset2, applyCardinalPoint);
                                        }
                                    }
                                }

                                foreach (var c in columns)
                                {
                                    if (colMap.TryGetValue(c.Section, out var elemType) && elemType is FamilySymbol sym)
                                    {
                                        var p1 = points.FirstOrDefault(p => p.Name == c.PointI);
                                        var p2 = points.FirstOrDefault(p => p.Name == c.PointJ);
                                        if (p1?.Position != null && p2?.Position != null)
                                        {
                                            XYZ offset = null;
                                            if (nodeOffsets != null)
                                            {
                                                nodeOffsets.TryGetValue(c.PointI, out offset);
                                            }
                                            ColumnBuilder.Build(doc, c, p1, p2, sym, allLevels, offset);
                                        }
                                    }
                                }

                                foreach (var w in walls)
                                {
                                    if (wallMap.TryGetValue(w.Section, out var elemType) && elemType is WallType wt)
                                    {
                                        var coords = w.PointNames.Select(pn => points.FirstOrDefault(p => p.Name == pn)?.Position).Where(pos => pos != null).ToList();
                                        if (coords.Count >= 2)
                                            WallBuilder.Build(doc, w, coords, wt, allLevels);
                                    }
                                }

                                var foundationSections = new HashSet<string>(
                                    items.Where(i => i.ElementType == JNNTool.Tools.CSIxRevit.Models.ElementKinds.FoundationSlab).Select(i => i.EtabsSectionName ?? ""),
                                    StringComparer.OrdinalIgnoreCase);
                                var virtualSections = new HashSet<string>(
                                    items.Where(i => i.ElementType == JNNTool.Tools.CSIxRevit.Models.ElementKinds.VirtualSlab).Select(i => i.EtabsSectionName ?? ""),
                                    StringComparer.OrdinalIgnoreCase);
                                slabBuilt = 0; foundationBuilt = 0; virtualSkipped = 0; foundationRejected = 0;

                                foreach (var f in floors)
                                {
                                    if (virtualSections.Contains(f.Section ?? "")) { virtualSkipped++; continue; }

                                    if (floorMap.TryGetValue(f.Section, out var elemType) && elemType is FloorType ft)
                                    {
                                        bool isFoundation = foundationSections.Contains(f.Section ?? "");
                                        // Sàn móng F tuyệt đối không được dựng thành Floor thường (sai category khi thống kê).
                                        if (isFoundation && !ft.IsFoundationSlab) { foundationRejected++; continue; }

                                        var coords = f.PointNames.Select(pn => points.FirstOrDefault(p => p.Name == pn)?.Position).Where(pos => pos != null).ToList();
                                        if (coords.Count >= 3 && FloorBuilder.Build(doc, f, coords, ft, allLevels))
                                        {
                                            if (isFoundation) foundationBuilt++; else slabBuilt++;
                                        }
                                    }
                                }

                                if (fitScopeBox)
                                {
                                    try
                                    {
                                        scopeResult = FitScopeBox(doc, points, beams, columns, walls, floors, grids, stories, allLevels, gridIdsBefore);
                                    }
                                    catch (Exception sbEx)
                                    {
                                        JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi đặt Scope Box", sbEx);
                                        scopeResult = new ScopeBoxBuildResult { Found = true, Error = sbEx.Message };
                                    }
                                }

                                try
                                {
                                    doc.Regenerate();
                                }
                                catch { }

                                tx.Commit();
                            }

                            hasImported = true;
                            return true;
                        }

                        void ShowSummary()
                        {
                            var sb = new System.Text.StringBuilder();
                            sb.AppendLine("ETABS MODEL IMPORTED SUCCESSFULLY!");
                            sb.AppendLine();
                            sb.AppendLine($"• Grids: created {gridResult.CreatedCount}/{grids.Count} (skipped/duplicate: {gridResult.SkippedCount})");
                            if (gridResult.Errors.Count > 0)
                            {
                                sb.AppendLine($"  Note: {gridResult.ErrorSummary}");
                            }
                            sb.AppendLine($"• Levels: created/matched {stories.Count}");
                            sb.AppendLine($"• Beams: processed {beams.Count}");
                            sb.AppendLine($"• Columns: processed {columns.Count}");
                            sb.AppendLine($"• Walls: processed {walls.Count}");
                            sb.AppendLine($"• Floors: processed {floors.Count} → Slab (S): {slabBuilt}, Foundation Slab (F): {foundationBuilt}, Virtual skipped: {virtualSkipped}");
                            if (foundationRejected > 0)
                            {
                                sb.AppendLine($"  ⚠️ {foundationRejected} foundation slab(s) skipped because the selected type is not a Foundation Slab.");
                            }
                            sb.AppendLine();
                            AppendScopeBoxSummary(sb, fitScopeBox, scopeResult);
                            sb.AppendLine();
                            sb.AppendLine("💡 Viewing grids:");
                            sb.AppendLine("- Any Floor Plan: grids are fully visible.");
                            sb.AppendLine("- 3D View: grids were enabled on the selected base level.");

                            Autodesk.Revit.UI.TaskDialog.Show("ETABS Import Result", sb.ToString());
                        }

                        // Hành động này được cửa sổ gọi qua ExternalEvent → luôn chạy trong Revit API context.
                        window.SetImportAction(() =>
                        {
                            if (hasImported) return true;
                            bool ok = RunImport();
                            if (ok) ShowSummary();
                            return ok;
                        });

                        var helper = new System.Windows.Interop.WindowInteropHelper(window);
                        helper.Owner = uiapp.MainWindowHandle;

                        window.Loaded += (s, e) =>
                        {
                            window.Activate();
                            window.Focus();
                        };

                        // Modeless: trả quyền cho Revit ngay, người dùng vẫn thao tác được trên model.
                        window.Show();
                    }
                    catch (Exception ex)
                    {
                        JNNTool.Core.Logging.Logger.Error("CSI x Revit - lỗi mở công cụ Import E2K", ex);
                        Autodesk.Revit.UI.TaskDialog.Show("JNN - CSI x Revit", "Could not open the ETABS file: " + ex.Message + "\n\nDetails have been written to the log.");
                    }
                }
            }

            return Result.Succeeded;
        }

        private const double ScopeBoxMarginMm = 2000;

        /// <summary>Khung công trình = các nút thực sự được dùng + vị trí lưới trục; cao độ theo tầng.</summary>
        private static ScopeBoxBuildResult FitScopeBox(
            Document doc,
            List<JNNTool.Tools.CSIxRevit.Models.PointData> points,
            List<JNNTool.Tools.CSIxRevit.Models.BeamData> beams,
            List<JNNTool.Tools.CSIxRevit.Models.ColumnData> columns,
            List<JNNTool.Tools.CSIxRevit.Models.WallData> walls,
            List<JNNTool.Tools.CSIxRevit.Models.FloorData> floors,
            List<JNNTool.Tools.CSIxRevit.Models.GridData> grids,
            List<JNNTool.Tools.CSIxRevit.Models.StoryData> stories,
            List<Level> levelsBefore,
            HashSet<ElementId> gridIdsBefore)
        {
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in beams) { used.Add(b.PointI ?? ""); used.Add(b.PointJ ?? ""); }
            foreach (var c in columns) { used.Add(c.PointI ?? ""); used.Add(c.PointJ ?? ""); }
            foreach (var w in walls) if (w.PointNames != null) foreach (var n in w.PointNames) used.Add(n ?? "");
            foreach (var f in floors) if (f.PointNames != null) foreach (var n in f.PointNames) used.Add(n ?? "");

            var xs = new List<double>();
            var ys = new List<double>();
            foreach (var p in points)
            {
                if (p?.Position == null || !used.Contains(p.Name ?? "")) continue;
                xs.Add(p.Position.X);
                ys.Add(p.Position.Y);
            }
            foreach (var g in grids)
            {
                if (g.IsGeneral) { xs.Add(g.X1); xs.Add(g.X2); ys.Add(g.Y1); ys.Add(g.Y2); }
                else if (string.Equals(g.Direction, "X", StringComparison.OrdinalIgnoreCase)) xs.Add(g.Coordinate);
                else if (string.Equals(g.Direction, "Y", StringComparison.OrdinalIgnoreCase)) ys.Add(g.Coordinate);
            }

            double minZ = 0, maxZ = 0;
            if (stories.Count > 0)
            {
                minZ = stories.Min(s => s.Elevation);
                maxZ = stories.Max(s => s.Elevation);
            }

            return ScopeBoxBuilder.CenterScopeBox(doc, xs, ys, minZ, maxZ, ScopeBoxMarginMm);
        }

        private static void AppendScopeBoxSummary(System.Text.StringBuilder sb, bool enabled, ScopeBoxBuildResult r)
        {
            if (!enabled) return;
            if (r == null || !r.Found)
            {
                sb.AppendLine($"• Scope Box: ⚠️ No scope box named '{ScopeBoxBuilder.DefaultName}' found in project. Please create one named '{ScopeBoxBuilder.DefaultName}' so the tool can center it for you.");
                return;
            }
            if (!string.IsNullOrEmpty(r.Error))
            {
                sb.AppendLine($"• Scope Box '{r.Name}': ❌ {r.Error}");
                return;
            }
            if (r.Fit == null || !r.Fit.HasBuilding)
            {
                sb.AppendLine($"• Scope Box '{r.Name}': no building extents found, not moved.");
                return;
            }

            sb.AppendLine($"• Scope Box '{r.Name}': ✅ Placed at building center (so you can easily locate and adjust it).");
            sb.AppendLine("  👉 Next steps:");
            sb.AppendLine("     1. Drag the Scope Box handles in Revit to frame the house as desired.");
            sb.AppendLine("     2. Click 'Fit Scope Box' on the ribbon to lock all Grids and Levels to it!");
        }
    }

    public class ImportFailurePreprocessor : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> fmas = failuresAccessor.GetFailureMessages();
            if (fmas.Count == 0)
                return FailureProcessingResult.Continue;

            bool resolvedError = false;

            foreach (FailureMessageAccessor fma in fmas)
            {
                if (fma.GetSeverity() == FailureSeverity.Warning)
                {
                    failuresAccessor.DeleteWarning(fma);
                }
                else if (fma.GetSeverity() == FailureSeverity.Error)
                {
                    if (failuresAccessor.IsFailureResolutionPermitted(fma))
                    {
                        failuresAccessor.ResolveFailure(fma);
                        resolvedError = true;
                    }
                }
            }

            if (resolvedError)
                return FailureProcessingResult.ProceedWithCommit;

            return FailureProcessingResult.Continue;
        }
    }

    public class RevitWin32Window : System.Windows.Forms.IWin32Window
    {
        private readonly IntPtr _hwnd;
        public RevitWin32Window(IntPtr hwnd) => _hwnd = hwnd;
        public IntPtr Handle => _hwnd;
    }
}
