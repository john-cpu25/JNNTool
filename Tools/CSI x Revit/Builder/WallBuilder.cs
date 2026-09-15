using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

using System.Linq;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class WallBuilder
    {
        public static void Build(Document doc, WallData wall, List<XYZ> coords, WallType wallType, List<Level> allLevels)
        {
            // Simplified Wall generation (bottom line):
            double botZ = wall.BottomElevation;
            double topZ = wall.TopElevation;
            
            Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - botZ)).FirstOrDefault();
            if (level == null) return;
            
            if (coords.Count >= 2)
            {
                XYZ p1 = new XYZ(coords[0].X, coords[0].Y, botZ);
                XYZ p2 = new XYZ(coords[1].X, coords[1].Y, botZ);
                Curve baseCurve = Line.CreateBound(p1, p2);
                Wall newWall = Wall.Create(doc, baseCurve, wallType.Id, level.Id, topZ - botZ, 0, false, false);
                newWall.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET)?.Set(botZ - level.Elevation);
            }
        }
    }
}
