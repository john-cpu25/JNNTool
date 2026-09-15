using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class GridBuilder
    {
        public static void Build(Document doc, List<GridData> grids)
        {
            if (grids.Count == 0) return;

            var existingGrids = new FilteredElementCollector(doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>()
                .ToList();

            // Calculate bounding box for grid lines
            double minX = 0, maxX = 100, minY = 0, maxY = 100;
            var xGrids = grids.Where(g => g.Direction.Equals("X", StringComparison.OrdinalIgnoreCase)).ToList();
            var yGrids = grids.Where(g => g.Direction.Equals("Y", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (xGrids.Any())
            {
                minX = xGrids.Min(g => g.Coordinate) - 10.0;
                maxX = xGrids.Max(g => g.Coordinate) + 10.0;
            }
            if (yGrids.Any())
            {
                minY = yGrids.Min(g => g.Coordinate) - 10.0;
                maxY = yGrids.Max(g => g.Coordinate) + 10.0;
            }

            foreach (var grid in grids)
            {
                var existing = existingGrids.FirstOrDefault(g => g.Name.Equals(grid.Name, StringComparison.OrdinalIgnoreCase));
                if (existing != null) continue; // Skip existing

                Curve curve = null;
                if (grid.Direction.Equals("X", StringComparison.OrdinalIgnoreCase))
                {
                    // ETABS "X" grid runs along Y axis!
                    XYZ p1 = new XYZ(grid.Coordinate, minY, 0);
                    XYZ p2 = new XYZ(grid.Coordinate, maxY, 0);
                    curve = Line.CreateBound(p1, p2);
                }
                else if (grid.Direction.Equals("Y", StringComparison.OrdinalIgnoreCase))
                {
                    // ETABS "Y" grid runs along X axis!
                    XYZ p1 = new XYZ(minX, grid.Coordinate, 0);
                    XYZ p2 = new XYZ(maxX, grid.Coordinate, 0);
                    curve = Line.CreateBound(p1, p2);
                }

                if (curve != null)
                {
                    try
                    {
                        var newGrid = Grid.Create(doc, Line.CreateBound(curve.GetEndPoint(0), curve.GetEndPoint(1)));
                        newGrid.Name = grid.Name;
                    }
                    catch { }
                }
            }
        }
    }
}
