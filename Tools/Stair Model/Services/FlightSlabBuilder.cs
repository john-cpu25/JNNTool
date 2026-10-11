#nullable enable
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using JNNTool.Tools.StairModel.Engine;

namespace JNNTool.Tools.StairModel.Services
{
    /// <summary>
    /// Chịu trách nhiệm tạo bản thang nghiêng (Waist Slab) dạng native Revit Floor có độ dốc.
    /// </summary>
    public static class FlightSlabBuilder
    {
        public static Floor? Build(Document doc, FlightGeometry flight, ElementId floorTypeId, ElementId levelId)
        {
            if (doc == null || flight == null || flight.ProfilePoints.Count < 3)
                return null;

            // 1. Chuyển đổi các điểm biên dạng từ mm sang feet
            var profileCurveLoop = new CurveLoop();
            int count = flight.ProfilePoints.Count;

            for (int i = 0; i < count; i++)
            {
                var ptCurrent = flight.ProfilePoints[i];
                var ptNext = flight.ProfilePoints[(i + 1) % count];

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
                // 2. Tạo đường mũi tên độ dốc (Slope Arrow)
                XYZ arrowStart = new XYZ(
                    UnitUtils.ConvertToInternalUnits(flight.SlopeArrowStart.X, UnitTypeId.Millimeters),
                    UnitUtils.ConvertToInternalUnits(flight.SlopeArrowStart.Y, UnitTypeId.Millimeters),
                    0.0
                );

                XYZ arrowEnd = new XYZ(
                    UnitUtils.ConvertToInternalUnits(flight.SlopeArrowEnd.X, UnitTypeId.Millimeters),
                    UnitUtils.ConvertToInternalUnits(flight.SlopeArrowEnd.Y, UnitTypeId.Millimeters),
                    0.0
                );

                Line? slopeArrow = null;
                if (arrowStart.DistanceTo(arrowEnd) > 1e-4)
                {
                    slopeArrow = Line.CreateBound(arrowStart, arrowEnd);
                }

                // 3. Tạo Floor nghiêng có độ dốc
                Floor? floor = null;
                var loops = new List<CurveLoop> { profileCurveLoop };

                if (slopeArrow != null && Math.Abs(flight.Slope) > 1e-5)
                {
                    floor = Floor.Create(doc, loops, floorTypeId, levelId, true, slopeArrow, flight.Slope);
                }
                else
                {
                    floor = Floor.Create(doc, loops, floorTypeId, levelId);
                }

                if (floor != null)
                {
                    // Thiết lập cao độ đáy vế thang so với Level
                    double baseOffsetFeet = UnitUtils.ConvertToInternalUnits(flight.BaseOffsetMm, UnitTypeId.Millimeters);
                    Parameter? offsetParam = floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM);
                    if (offsetParam != null && !offsetParam.IsReadOnly)
                    {
                        offsetParam.Set(baseOffsetFeet);
                    }

                    // Đặt Comments / Mark để dễ quản lý
                    Parameter? commentsParam = floor.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (commentsParam != null && !commentsParam.IsReadOnly)
                    {
                        commentsParam.Set($"JNN_Stair_{flight.Name}");
                    }
                }

                return floor;
            }

            return null;
        }
    }
}
