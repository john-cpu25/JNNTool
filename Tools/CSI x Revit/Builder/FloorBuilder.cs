using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

using System.Linq;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class FloorBuilder
    {
        public static void Build(Document doc, FloorData floor, List<XYZ> coords, FloorType floorType, List<Level> allLevels)
        {
            double z = floor.Elevation;
            Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - z)).FirstOrDefault();
            if (level == null) return;
            List<XYZ> cleaned = new List<XYZ> { coords[0] };
            for (int i = 1; i < coords.Count; i++)
            {
                if (coords[i].DistanceTo(cleaned[cleaned.Count - 1]) > 0.01)
                    cleaned.Add(coords[i]);
            }
            if (cleaned.Count > 1 && cleaned[cleaned.Count - 1].DistanceTo(cleaned[0]) <= 0.01)
            {
                cleaned.RemoveAt(cleaned.Count - 1);
            }

            CurveLoop profile = new CurveLoop();
            for (int i = 0; i < cleaned.Count; i++)
            {
                XYZ p1 = new XYZ(cleaned[i].X, cleaned[i].Y, z);
                XYZ p2 = new XYZ(cleaned[(i + 1) % cleaned.Count].X, cleaned[(i + 1) % cleaned.Count].Y, z);
                try
                {
                    profile.Append(Line.CreateBound(p1, p2));
                }
                catch { }
            }

            if (profile.NumberOfCurves() >= 3 && !profile.IsOpen())
            {
                try
                {
                    Floor newFloor = Floor.Create(doc, new List<CurveLoop> { profile }, floorType.Id, level.Id);
                    newFloor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)?.Set(z - level.Elevation);
                }
                catch (Exception ex)
                {
                    // Ignore invalid floor boundaries (e.g. self-intersecting)
                    System.Diagnostics.Debug.WriteLine("Floor failed: " + ex.Message);
                }
            }
        }
    }
}
