using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.CSIxRevit.Models;

using System.Collections.Generic;
using System.Linq;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class ColumnBuilder
    {
        public static void Build(Document doc, ColumnData column, PointData p1, PointData p2, FamilySymbol symbol, List<Level> allLevels)
        {
            if (symbol != null && !symbol.IsActive) symbol.Activate();
            XYZ botPt = new XYZ(p1.Position.X, p1.Position.Y, column.BottomElevation);
            XYZ topPt = new XYZ(p2.Position.X, p2.Position.Y, column.TopElevation);

            Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - botPt.Z)).FirstOrDefault();
            if (level == null) return;

            if (botPt.IsAlmostEqualTo(topPt))
            {
                // Vertical column defined by a single point
                FamilyInstance instance = doc.Create.NewFamilyInstance(botPt, symbol, level, StructuralType.Column);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(botPt.Z - level.Elevation);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(botPt.Z - level.Elevation + 10.0);
            }
            else if (Math.Abs(botPt.X - topPt.X) < 0.001 && Math.Abs(botPt.Y - topPt.Y) < 0.001)
            {
                // Perfectly vertical column between two distinct Z points
                FamilyInstance instance = doc.Create.NewFamilyInstance(botPt, symbol, level, StructuralType.Column);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(botPt.Z - level.Elevation);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topPt.Z - level.Elevation);
            }
            else
            {
                // Slanted column
                Curve curve = Line.CreateBound(botPt, topPt);
                FamilyInstance instance = doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Column);
                
                // Fix Column Elevation
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(botPt.Z - level.Elevation);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topPt.Z - level.Elevation);
            }
        }
    }
}
