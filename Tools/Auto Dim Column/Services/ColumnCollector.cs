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
    /// Thu thập và lọc các cấu kiện Cột kết cấu (OST_StructuralColumns) và Cột kiến trúc (OST_Columns).
    /// </summary>
    public static class ColumnCollector
    {
        public static List<FamilyInstance> CollectColumns(UIDocument uidoc, Document doc, View activeView, ColumnScopeMode mode)
        {
            var results = new List<FamilyInstance>();
            var seenIds = new HashSet<long>();

            if (mode == ColumnScopeMode.SelectedElements)
            {
                var selIds = uidoc.Selection.GetElementIds();
                foreach (var id in selIds)
                {
                    if (doc.GetElement(id) is FamilyInstance fi && IsValidColumn(fi))
                    {
                        long val = GetIdValue(id);
                        if (seenIds.Add(val))
                        {
                            results.Add(fi);
                        }
                    }
                }
            }
            else
            {
                // Toàn bộ cột hiển thị trong Active View (hỗ trợ cả cột kết cấu và cột kiến trúc)
                var catFilters = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_Columns
                };

                var multicatFilter = new ElementMulticategoryFilter(catFilters);

                var col = new FilteredElementCollector(doc, activeView.Id)
                    .WherePasses(multicatFilter)
                    .WhereElementIsNotElementType();

                foreach (var elem in col)
                {
                    if (elem is FamilyInstance fi && IsValidColumn(fi))
                    {
                        long val = GetIdValue(fi.Id);
                        if (seenIds.Add(val))
                        {
                            results.Add(fi);
                        }
                    }
                }
            }

            return results;
        }

        public static bool IsValidColumn(FamilyInstance fi)
        {
            if (fi.Category == null) return false;
            long catId = GetIdValue(fi.Category.Id);
            return catId == (long)BuiltInCategory.OST_StructuralColumns || catId == (long)BuiltInCategory.OST_Columns;
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
