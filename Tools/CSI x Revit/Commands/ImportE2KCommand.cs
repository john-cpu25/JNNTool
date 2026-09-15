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
                if (ofd.ShowDialog() == DialogResult.OK)
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
                        parser.Parse(tables, out var points, out var beams, out var columns, out var walls, out var floors, out var sectionProps, out var stories, out var grids);

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
                            mapper.Levels
                        );

                        if (window.ShowDialog() == true)
                        {
                            var beamMap = window.GetBeamMapping();
                            var colMap = window.GetColumnMapping();
                            var floorMap = window.GetFloorMapping();
                            var wallMap = window.GetWallMapping();
                            
                            var levelId = window.GetSelectedLevelId();
                            var level = doc.GetElement(levelId) as Level;

                            using (Transaction tx = new Transaction(doc, "Import ETABS E2K"))
                            {
                                FailureHandlingOptions options = tx.GetFailureHandlingOptions();
                                options.SetFailuresPreprocessor(new ImportFailurePreprocessor());
                                tx.SetFailureHandlingOptions(options);

                                tx.Start();

                                LevelBuilder.Build(doc, stories);
                                var allLevels = new FilteredElementCollector(doc).OfClass(typeof(Level)).Cast<Level>().ToList();
                                GridBuilder.Build(doc, grids);

                                foreach (var b in beams)
                                {
                                    if (beamMap.ContainsKey(b.Section) && beamMap[b.Section] != null)
                                    {
                                        var p1 = points.FirstOrDefault(p => p.Name == b.PointI);
                                        var p2 = points.FirstOrDefault(p => p.Name == b.PointJ);
                                        if (p1 != null && p2 != null)
                                            BeamBuilder.Build(doc, b, p1, p2, beamMap[b.Section] as FamilySymbol, allLevels);
                                    }
                                }

                                foreach (var c in columns)
                                {
                                    if (colMap.ContainsKey(c.Section) && colMap[c.Section] != null)
                                    {
                                        var p1 = points.FirstOrDefault(p => p.Name == c.PointI);
                                        var p2 = points.FirstOrDefault(p => p.Name == c.PointJ);
                                        if (p1 != null && p2 != null)
                                            ColumnBuilder.Build(doc, c, p1, p2, colMap[c.Section] as FamilySymbol, allLevels);
                                    }
                                }

                                foreach (var w in walls)
                                {
                                    if (wallMap.ContainsKey(w.Section) && wallMap[w.Section] != null)
                                    {
                                        var coords = w.PointNames.Select(pn => points.FirstOrDefault(p => p.Name == pn)?.Position).Where(pos => pos != null).ToList();
                                        if (coords.Count >= 2)
                                            WallBuilder.Build(doc, w, coords, wallMap[w.Section] as WallType, allLevels);
                                    }
                                }

                                foreach (var f in floors)
                                {
                                    if (floorMap.ContainsKey(f.Section) && floorMap[f.Section] != null)
                                    {
                                        var coords = f.PointNames.Select(pn => points.FirstOrDefault(p => p.Name == pn)?.Position).Where(pos => pos != null).ToList();
                                        if (coords.Count >= 3)
                                            FloorBuilder.Build(doc, f, coords, floorMap[f.Section] as FloorType, allLevels);
                                    }
                                }

                                tx.Commit();
                            }
                            Autodesk.Revit.UI.TaskDialog.Show("Thành công", "Đã import xong mô hình từ ETABS!");
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
}
