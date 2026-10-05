using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class ColumnBuilder
    {
        public static void Build(Document doc, ColumnData column, PointData p1, PointData p2, FamilySymbol symbol, List<Level> allLevels)
        {
            Build(doc, column, p1, p2, symbol, allLevels, null);
        }

        public static void Build(Document doc, ColumnData column, PointData p1, PointData p2, FamilySymbol symbol, List<Level> allLevels, XYZ offset = null)
        {
            if (symbol != null && !symbol.IsActive) symbol.Activate();

            XYZ botPt = new XYZ(p1.Position.X, p1.Position.Y, column.BottomElevation);
            XYZ topPt = new XYZ(p2.Position.X, p2.Position.Y, column.TopElevation);

            if (offset != null)
            {
                botPt = botPt + offset;
                topPt = topPt + offset;
            }

            Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - botPt.Z)).FirstOrDefault();
            if (level == null) return;

            double baseOffset = botPt.Z - level.Elevation;
            double topOffset = (column.TopElevation > column.BottomElevation)
                ? (column.TopElevation - level.Elevation)
                : (baseOffset + 10.0);

            if (botPt.IsAlmostEqualTo(topPt))
            {
                // Vertical column defined by a single point
                FamilyInstance instance = doc.Create.NewFamilyInstance(botPt, symbol, level, StructuralType.Column);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffset);
            }
            else if (Math.Abs(botPt.X - topPt.X) < 0.001 && Math.Abs(botPt.Y - topPt.Y) < 0.001)
            {
                // Perfectly vertical column between two distinct Z points
                FamilyInstance instance = doc.Create.NewFamilyInstance(botPt, symbol, level, StructuralType.Column);
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topPt.Z - level.Elevation);
            }
            else
            {
                // Slanted column
                if (botPt.DistanceTo(topPt) < 0.001) return;
                Curve curve = Line.CreateBound(botPt, topPt);
                FamilyInstance instance = doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Column);
                
                // Fix Column Elevation
                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topPt.Z - level.Elevation);
            }
        }
    }
}
