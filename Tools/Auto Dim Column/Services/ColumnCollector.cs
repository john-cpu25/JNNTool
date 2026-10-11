#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using JNNTool.Tools.AutoDimColumn.Models;

namespace JNNTool.Tools.AutoDimColumn.Services
{
    /// <summary>
    /// Thu thập và lọc các cấu kiện Cột kết cấu (OST_StructuralColumns).
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
                    if (doc.GetElement(id) is FamilyInstance fi && IsValidStructuralColumn(fi))
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
                // Toàn bộ cột hiển thị trong Active View
                var col = new FilteredElementCollector(doc, activeView.Id)
                    .OfCategory(BuiltInCategory.OST_StructuralColumns)
                    .WhereElementIsNotElementType();

                foreach (var elem in col)
                {
                    if (elem is FamilyInstance fi && IsValidStructuralColumn(fi))
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

        public static bool IsValidStructuralColumn(FamilyInstance fi)
        {
            if (fi.Category == null) return false;
#if NET48
            if (fi.Category.Id.IntegerValue != (int)BuiltInCategory.OST_StructuralColumns) return false;
#else
            if (fi.Category.Id.Value != (long)BuiltInCategory.OST_StructuralColumns) return false;
#endif
            // Bỏ qua cột đang bị ẩn trong view
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
