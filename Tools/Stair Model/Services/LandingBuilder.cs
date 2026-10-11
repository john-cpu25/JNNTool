#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.StairModel.Engine;

namespace JNNTool.Tools.StairModel.Services
{
    /// <summary>
    /// Chịu trách nhiệm tạo bản chiếu nghỉ (Landing Slab) dạng native Revit Floor phẳng.
    /// </summary>
    public static class LandingBuilder
    {
        public static Floor? Build(Document doc, LandingGeometry landing, ElementId floorTypeId, ElementId levelId)
        {
            if (doc == null || landing == null || landing.ProfilePoints.Count < 3)
                return null;

            // 1. Chuyển đổi các điểm biên dạng sang CurveLoop phẳng
            var profileCurveLoop = new CurveLoop();
            int count = landing.ProfilePoints.Count;

            for (int i = 0; i < count; i++)
            {
                var ptCurrent = landing.ProfilePoints[i];
                var ptNext = landing.ProfilePoints[(i + 1) % count];

                XYZ p1 = new XYZ(
                    UnitUtils.ConvertToInternalUnits(ptCurrent.X, UnitTypeId.Millimeters),
                    UnitUtils.ConvertToInternalUnits(ptCurrent.Y, UnitTypeId.Millimeters),
                    0.0
                );

                XYZ p2 = new XYZ(
                    UnitUtils.ConvertToInternalUnits(ptNext.X, UnitTypeId.Millimeters),
                    UnitUtils.ConvertToInternalUnits(ptNext.Y, UnitTypeId.Millimeters),
                    0.0
                );

                if (p1.DistanceTo(p2) > 1e-5)
                {
                    profileCurveLoop.Append(Line.CreateBound(p1, p2));
                }
            }

            if (!profileCurveLoop.IsOpen())
            {
                var loops = new List<CurveLoop> { profileCurveLoop };
                Floor floor = Floor.Create(doc, loops, floorTypeId, levelId);

                if (floor != null)
                {
                    // Thiết lập cao độ chiếu nghỉ so với Level
                    double elevationFeet = UnitUtils.ConvertToInternalUnits(landing.ElevationOffsetMm, UnitTypeId.Millimeters);
                    Parameter? offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                    if (offsetParam != null && !offsetParam.IsReadOnly)
                    {
                        offsetParam.Set(elevationFeet);
                    }

                    // Đặt Comments
                    Parameter? commentsParam = floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (commentsParam != null && !commentsParam.IsReadOnly)
                    {
                        commentsParam.Set($"JNN_Stair_{landing.Name}");
                    }
                }

                return floor;
            }

            return null;
        }
    }
}
