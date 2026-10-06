using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public class GridBuildResult
    {
        public int TotalParsed { get; set; }
        public int CreatedCount { get; set; }
        public int SkippedCount { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public string ErrorSummary => Errors.Count > 0 ? string.Join("; ", Errors.Take(3)) : "";
    }

    public static class GridBuilder
    {
        public static GridBuildResult Build(Document doc, List<GridData> grids)
        {
            var result = new GridBuildResult();
            if (doc == null || grids == null || grids.Count == 0) return result;

            result.TotalParsed = grids.Count;

            try
            {
                // 1. Deduplicate grids: avoid overlapping collinear grids
                var distinctGrids = new List<GridData>();
                foreach (var g in grids)
                {
                    if (string.IsNullOrWhiteSpace(g.Name)) continue;

                    bool isDup = distinctGrids.Any(d =>
                        (!g.IsGeneral && !d.IsGeneral && d.Direction.Equals(g.Direction, StringComparison.OrdinalIgnoreCase) && Math.Abs(d.Coordinate - g.Coordinate) < 0.05) ||
                        (g.IsGeneral && d.IsGeneral &&
                         ((Math.Abs(d.X1 - g.X1) < 0.05 && Math.Abs(d.Y1 - g.Y1) < 0.05 && Math.Abs(d.X2 - g.X2) < 0.05 && Math.Abs(d.Y2 - g.Y2) < 0.05) ||
                          (Math.Abs(d.X1 - g.X2) < 0.05 && Math.Abs(d.Y1 - g.Y2) < 0.05 && Math.Abs(d.X2 - g.X1) < 0.05 && Math.Abs(d.Y2 - g.Y1) < 0.05)))
                    );

                    if (!isDup)
                    {
                        distinctGrids.Add(g);
                    }
                    else
                    {
                        result.SkippedCount++;
                    }
                }

                if (distinctGrids.Count == 0) return result;

                // 2. Existing grids in Revit project
                var existingNames = new HashSet<string>(
                    new FilteredElementCollector(doc)
                        .OfClass(typeof(Grid))
                        .Cast<Grid>()
                        .Select(g => g.Name)
                        .Where(n => !string.IsNullOrEmpty(n)),
                    StringComparer.OrdinalIgnoreCase);

                // 3. Compute overall bounding box for Cartesian grids
                var allX = new List<double>();
                var allY = new List<double>();

                foreach (var g in distinctGrids)
                {
                    if (g.IsGeneral)
                    {
                        allX.Add(g.X1);
                        allX.Add(g.X2);
                        allY.Add(g.Y1);
                        allY.Add(g.Y2);
                    }
                    else if (g.Direction.Equals("X", StringComparison.OrdinalIgnoreCase))
                    {
                        allX.Add(g.Coordinate);
                    }
                    else if (g.Direction.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        allY.Add(g.Coordinate);
                    }
                }

                double minX = allX.Any() ? allX.Min() - 15.0 : -15.0;
                double maxX = allX.Any() ? allX.Max() + 15.0 : 150.0;
                double minY = allY.Any() ? allY.Min() - 15.0 : -15.0;
                double maxY = allY.Any() ? allY.Max() + 15.0 : 150.0;

                if (Math.Abs(maxX - minX) < 1.0) { minX -= 20.0; maxX += 20.0; }
                if (Math.Abs(maxY - minY) < 1.0) { minY -= 20.0; maxY += 20.0; }

                // 4. Create each grid
                foreach (var grid in distinctGrids)
                {
                    try
                    {
                        Line line = null;

                        if (grid.IsGeneral)
                        {
                            XYZ p1 = new XYZ(grid.X1, grid.Y1, 0);
                            XYZ p2 = new XYZ(grid.X2, grid.Y2, 0);
                            if (p1.DistanceTo(p2) >= 0.1)
                            {
                                line = Line.CreateBound(p1, p2);
                            }
                        }
                        else if (grid.Direction.Equals("X", StringComparison.OrdinalIgnoreCase))
                        {
                            // ETABS "X" grid is at constant X, line runs along Y axis from minY to maxY
                            XYZ p1 = new XYZ(grid.Coordinate, minY, 0);
                            XYZ p2 = new XYZ(grid.Coordinate, maxY, 0);
                            line = Line.CreateBound(p1, p2);
                        }
                        else if (grid.Direction.Equals("Y", StringComparison.OrdinalIgnoreCase))
                        {
                            // ETABS "Y" grid is at constant Y, line runs along X axis from minX to maxX
                            XYZ p1 = new XYZ(minX, grid.Coordinate, 0);
                            XYZ p2 = new XYZ(maxX, grid.Coordinate, 0);
                            line = Line.CreateBound(p1, p2);
                        }

                        if (line == null)
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        Grid newGrid = null;
                        try
                        {
                            newGrid = Grid.Create(doc, line);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Tạo trục '{grid.Name}' ({grid.Direction}) lỗi: {ex.Message}");
                            continue;
                        }

                        if (newGrid == null)
                        {
                            result.SkippedCount++;
                            continue;
                        }

                        result.CreatedCount++;

                        // Safe naming
                        string targetName = grid.Name;
                        if (existingNames.Contains(targetName))
                        {
                            int suffix = 1;
                            while (existingNames.Contains($"{targetName}_{suffix}"))
                            {
                                suffix++;
                            }
                            targetName = $"{targetName}_{suffix}";
                        }

                        try
                        {
                            newGrid.Name = targetName;
                            existingNames.Add(targetName);
                        }
                        catch
                        {
                            existingNames.Add(newGrid.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Lỗi xử lý trục '{grid.Name}': {ex.Message}");
                    }
                }

                // Log result to AppData file for transparency
                try
                {
                    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    var logDir = Path.Combine(appData, "JNNTool", "Logs");
                    Directory.CreateDirectory(logDir);
                    var logFile = Path.Combine(logDir, "CSIxRevit_Import.log");
                    var logText = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GridBuilder: Tổng nhận={grids.Count}, Đã tạo={result.CreatedCount}, Bỏ qua={result.SkippedCount}, Lỗi={result.Errors.Count}\n";
                    if (result.Errors.Count > 0)
                    {
                        foreach (var err in result.Errors)
                        {
                            logText += $"   - {err}\n";
                        }
                    }
                    File.AppendAllText(logFile, logText);
                }
                catch { }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Lỗi GridBuilder: {ex.Message}");
            }

            return result;
        }
    }
}
