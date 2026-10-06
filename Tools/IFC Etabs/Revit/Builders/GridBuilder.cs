using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo và ánh xạ lưới trục kết cấu (Grid) trong Revit từ ETABS / IFC.
    /// Tránh tạo trùng lặp lưới trục đã có.
    /// </summary>
    public class GridBuilder
    {
        private readonly Document _doc;
        private readonly Dictionary<string, Grid> _gridsByName = new(StringComparer.OrdinalIgnoreCase);

        public GridBuilder(Document doc)
        {
            _doc = doc;
            RefreshGridCache();
        }

        public void RefreshGridCache()
        {
            _gridsByName.Clear();
            var collector = new FilteredElementCollector(_doc)
                .OfClass(typeof(Grid))
                .Cast<Grid>();

            foreach (var grid in collector)
            {
                _gridsByName[grid.Name] = grid;
            }
        }

        public Grid? GetOrCreateGrid(ETABSGrid etabsGrid)
        {
            if (string.IsNullOrWhiteSpace(etabsGrid.Name)) return null;

            // 1. Kiểm tra nếu lưới đã tồn tại theo tên
            if (_gridsByName.TryGetValue(etabsGrid.Name, out var existingGrid))
            {
                AttachParameters(existingGrid, etabsGrid);
                return existingGrid;
            }

            // 2. Tạo Grid mới
            var p1 = new XYZ(
                UnitConverter.MmToFeet(etabsGrid.StartPoint.X),
                UnitConverter.MmToFeet(etabsGrid.StartPoint.Y),
                0
            );
            var p2 = new XYZ(
                UnitConverter.MmToFeet(etabsGrid.EndPoint.X),
                UnitConverter.MmToFeet(etabsGrid.EndPoint.Y),
                0
            );

            if (p1.DistanceTo(p2) < 0.1) return null; // Quá ngắn

            Line line = Line.CreateBound(p1, p2);
            Grid newGrid = Grid.Create(_doc, line);
            if (newGrid != null)
            {
                try
                {
                    newGrid.Name = etabsGrid.Name;
                }
                catch { }

                _gridsByName[etabsGrid.Name] = newGrid;
                AttachParameters(newGrid, etabsGrid);
            }

            return newGrid;
        }

        private static void AttachParameters(Grid grid, ETABSGrid etabsGrid)
        {
            SharedParameterManager.SetParameterValue(grid, SharedParameterManager.ParamSource, etabsGrid.Source);
            SharedParameterManager.SetParameterValue(grid, SharedParameterManager.ParamEtabsId, etabsGrid.SourceId);
            SharedParameterManager.SetParameterValue(grid, SharedParameterManager.ParamSourceHash, etabsGrid.Hash);
            SharedParameterManager.SetParameterValue(grid, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
