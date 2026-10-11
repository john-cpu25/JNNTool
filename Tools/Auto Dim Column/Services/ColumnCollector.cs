#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Tools.AutoDimColumn.Models;

namespace JNNTool.Tools.AutoDimColumn.Services
{
    /// <summary>
    /// Thu thập và lọc các cấu kiện Cột (OST_StructuralColumns, OST_Columns) và Vách (OST_Walls).
    /// </summary>
    public static class ColumnCollector
    {
        public static List<Element> CollectElements(UIDocument uidoc, Document doc, View activeView, AutoDimColumnSettings settings)
        {
            var results = new List<Element>();
            var seenIds = new HashSet<long>();

            if (settings.ScopeMode == ColumnScopeMode.SelectedElements)
            {
                var selIds = uidoc.Selection.GetElementIds();
                foreach (var id in selIds)
                {
                    var elem = doc.GetElement(id);
                    if (elem is FamilyInstance fi && settings.IncludeColumns && IsValidColumn(fi))
                    {
                        long val = GetIdValue(id);
                        if (seenIds.Add(val))
                        {
                            results.Add(fi);
                        }
                    }
                    else if (elem is Wall wall && settings.IncludeWalls && IsValidWall(wall))
                    {
                        long val = GetIdValue(id);
                        if (seenIds.Add(val))
                        {
                            results.Add(wall);
                        }
                    }
                }
            }
            else
            {
                var catFilters = new List<BuiltInCategory>();
                if (settings.IncludeColumns)
                {
                    catFilters.Add(BuiltInCategory.OST_StructuralColumns);
                    catFilters.Add(BuiltInCategory.OST_Columns);
                }
                if (settings.IncludeWalls)
                {
                    catFilters.Add(BuiltInCategory.OST_Walls);
                }

                if (catFilters.Count == 0) return results;

                var multicatFilter = new ElementMulticategoryFilter(catFilters);

                var col = new FilteredElementCollector(doc, activeView.Id)
                    .WherePasses(multicatFilter)
                    .WhereElementIsNotElementType();

                foreach (var elem in col)
                {
                    if (elem is FamilyInstance fi && settings.IncludeColumns && IsValidColumn(fi))
                    {
                        long val = GetIdValue(fi.Id);
                        if (seenIds.Add(val))
                        {
                            results.Add(fi);
                        }
                    }
                    else if (elem is Wall wall && settings.IncludeWalls && IsValidWall(wall))
                    {
                        long val = GetIdValue(wall.Id);
                        if (seenIds.Add(val))
                        {
                            results.Add(wall);
                        }
                    }
                }
            }

            return results;
        }

        public static List<FamilyInstance> CollectColumns(UIDocument uidoc, Document doc, View activeView, ColumnScopeMode mode)
        {
            var settings = new AutoDimColumnSettings { ScopeMode = mode, IncludeColumns = true, IncludeWalls = false };
            return CollectElements(uidoc, doc, activeView, settings).OfType<FamilyInstance>().ToList();
        }

        public static bool IsValidColumn(FamilyInstance fi)
        {
            if (fi.Category == null) return false;
            long catId = GetIdValue(fi.Category.Id);
            return catId == (long)BuiltInCategory.OST_StructuralColumns || catId == (long)BuiltInCategory.OST_Columns;
        }

        public static bool IsValidWall(Wall wall)
        {
            if (wall.WallType?.Kind == WallKind.Curtain) return false;
            if (wall.Location is LocationCurve lc && lc.Curve.Length < 0.15) return false; // Bỏ qua đoạn vách quá ngắn (< 50mm)
            return true;
        }

        public static long GetIdValue(ElementId id)
        {
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }
}
