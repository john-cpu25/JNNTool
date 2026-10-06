using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

using System.Linq;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class WallBuilder
    {
        public static void Build(Document doc, WallData wall, List<Point3D> coords, WallType wallType, List<Level> allLevels)
        {
            if (doc == null || wall == null || wallType == null || coords == null || coords.Count < 2) return;
            if (allLevels == null || allLevels.Count == 0) return;

            try
            {
                double botZ = wall.BottomElevation;
                double topZ = wall.TopElevation;
                
                Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - botZ)).FirstOrDefault();
                if (level == null) return;
                
                XYZ p1 = new XYZ(coords[0].X, coords[0].Y, botZ);
                XYZ p2 = new XYZ(coords[1].X, coords[1].Y, botZ);
                if (p1.DistanceTo(p2) < 0.001) return;

                Curve baseCurve = Line.CreateBound(p1, p2);
                double height = topZ - botZ;
                if (height <= 0.001) height = 10.0;

                Wall newWall = Wall.Create(doc, baseCurve, wallType.Id, level.Id, height, 0, false, false);
                newWall?.get_Parameter(BuiltInParameter.WALL_BASE_OFFSET)?.Set(botZ - level.Elevation);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi dựng vách {wall.Name}: {ex.Message}");
            }
        }
    }
}
