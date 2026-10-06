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

namespace JNNTool.Tools.CSIxRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class ImportE2KCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var doc = uiapp.ActiveUIDocument.Document;

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
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = "Dầm (Beam)", ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var colGroups = columns.GroupBy(c => c.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in colGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.ColumnSymbols);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = "Cột (Column)", ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var wallGroups = walls.GroupBy(w => w.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in wallGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.WallTypes);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = "Vách (Wall)", ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var floorGroups = floors.GroupBy(f => f.Section).Select(g => new { Section = g.Key, Count = g.Count() });
                        foreach (var g in floorGroups)
                        {
                            var available = new List<ElementType>();
                            available.AddRange(mapper.FloorTypes);
                            items.Add(new JNNTool.Tools.CSIxRevit.UI.SectionMappingItem { EtabsSectionName = g.Section, ElementType = "Sàn (Floor)", ObjectCount = g.Count, AvailableTypes = available, SectionDetails = sectionProps.ContainsKey(g.Section) ? sectionProps[g.Section] : "" });
                        }

                        var window = new JNNTool.Tools.CSIxRevit.UI.ImportWindow(
                            doc, 
                            items, 
                            mapper.Levels,
                            mapper,
                            sectionDefs
                        );

                        bool hasImported = false;
                        GridBuildResult gridResult = new GridBuildResult();

                        bool RunImport()
                        {
                            var beamMap = window.GetBeamMapping();
                            var colMap = window.GetColumnMapping();
                            var floorMap = window.GetFloorMapping();
                            var wallMap = window.GetWallMapping();
                            bool applyCardinalPoint = window.ApplyCardinalPoint;
                            bool applyEdgeAlignment = window.ApplyEdgeAlignment;

                            Dictionary<string, XYZ> nodeOffsets = null;
                            if (applyEdgeAlignment)
                            {
                                nodeOffsets = EdgeAlignmentHelper.CalculateNodeOffsets(points, beams, columns, sectionDefs);
                            }
                            
                            var levelId = window.GetSelectedLevelId();
                            var level = doc.GetElement(levelId) as Level;

                            using (Transaction tx = new Transaction(doc, "Import ETABS E2K"))
                            {
                                FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new ImportFailurePreprocessor());
                                tx.SetFailureHandlingOptions(options);

                                tx.Start();

                                LevelBuilder.Build(doc, stories);

                                // Regenerate to register new levels before creating grids
                                doc.Regenerate();

                                var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
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

                                foreach (var f in floors)
                                {
                                    if (floorMap.TryGetValue(f.Section, out var elemType) && elemType is FloorType ft)
                                    {
                                        var coords = f.PointNames.Select(pn => points.FirstOrDefault(p => p.Name == pn)?.Position).Where(pos => pos != null).ToList();
                                        if (coords.Count >= 3)
                                            FloorBuilder.Build(doc, f, coords, ft, allLevels);
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

                        window.SetImportAction(RunImport);

                        var helper = new System.Windows.Interop.WindowInteropHelper(window);
                        helper.Owner = uiapp.MainWindowHandle;

                        window.Loaded += (s, e) =>
                        {
                            window.Activate();
                            window.Focus();
                        };

                        if (window.ShowDialog() == true && !hasImported)
                        {
                            RunImport();
                        }

                        if (hasImported)
                        {

                            var sb = new System.Text.StringBuilder();
                            sb.AppendLine("ĐÃ IMPORT XONG MÔ HÌNH TỪ ETABS!");
                            sb.AppendLine();
                            sb.AppendLine($"• Lưới trục (Grids): Đã tạo {gridResult.CreatedCount}/{grids.Count} trục (Bỏ qua/trùng: {gridResult.SkippedCount})");
                            if (gridResult.Errors.Count > 0)
                            {
                                sb.AppendLine($"  Ghi chú: {gridResult.ErrorSummary}");
                            }
                            sb.AppendLine($"• Tầng (Levels): Đã tạo/khớp {stories.Count} tầng");
                            sb.AppendLine($"• Dầm (Beams): Đã xử lý {beams.Count} dầm");
                            sb.AppendLine($"• Cột (Columns): Đã xử lý {columns.Count} cột");
                            sb.AppendLine($"• Vách (Walls): Đã xử lý {walls.Count} vách");
                            sb.AppendLine($"• Sàn (Floors): Đã xử lý {floors.Count} sàn");
                            sb.AppendLine();
                            sb.AppendLine("💡 Ghi chú xem Lưới trục (Grid):");
                            sb.AppendLine("- Mở bất kỳ Mặt bằng tầng nào (Floor Plan): Lưới trục hiển thị đầy đủ.");
                            sb.AppendLine("- Trên 3D View: Đã tự động kích hoạt hiển thị lưới trục trên tầng đã chọn.");

                            Autodesk.Revit.UI.TaskDialog.Show("Kết quả Import ETABS", sb.ToString());
                        }
                    }
                    catch (Exception ex)
                    {
                        Autodesk.Revit.UI.TaskDialog.Show("Error", ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }

            return Result.Succeeded;
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
