using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Stair.Models;

namespace JNNTool.RebarSuite.Stair.Builders
{
    /// <summary>
    /// Trích xuất dữ liệu cầu thang từ Revit và tạo cốt thép bản thang trong Document.
    /// </summary>
    public static class StairRebarBuilder
    {
        public static StairModel ExtractStairModel(Document doc, Autodesk.Revit.DB.Architecture.Stairs stairs)
        {
            var model = new StairModel
            {
                ElementId = stairs.Id.GetIdValue(),
                StairName = stairs.Name
            };

            var runs = stairs.GetStairsRuns();
            int idx = 1;
            foreach (var runId in runs)
            {
                var run = doc.GetElement(runId) as StairsRun;
                if (run == null) continue;

                var bbox = run.get_BoundingBox(null);
                if (bbox == null) continue;

                double minX = GeometryHelper.FeetToMm(bbox.Min.X);
                double maxX = GeometryHelper.FeetToMm(bbox.Max.X);
                double minY = GeometryHelper.FeetToMm(bbox.Min.Y);
                double maxY = GeometryHelper.FeetToMm(bbox.Max.Y);
                double minZ = GeometryHelper.FeetToMm(bbox.Min.Z);
                double maxZ = GeometryHelper.FeetToMm(bbox.Max.Z);

                double width = GeometryHelper.FeetToMm(run.ActualRunWidth);
                if (width <= 0) width = Math.Min(maxX - minX, maxY - minY);

                var flight = new StairFlightModel
                {
                    FlightIndex = idx++,
                    WidthMm = Math.Max(width, 600.0),
                    SlabThicknessMm = 120.0,
                    RiserCount = Math.Max(run.ActualRisersNumber, 2),
                    StartPoint = new Point3D(minX, (minY + maxY) / 2.0, minZ),
                    EndPoint = new Point3D(maxX, (minY + maxY) / 2.0, maxZ),
                    Direction = new Point3D(1, 0, 0),
                    Normal = new Point3D(0, 1, 0)
                };

                model.Flights.Add(flight);
            }

            if (model.Flights.Count == 0)
            {
                // Fallback nếu không đọc được run
                var bbox = stairs.get_BoundingBox(null);
                if (bbox != null)
                {
                    model.Flights.Add(new StairFlightModel
                    {
                        FlightIndex = 1,
                        WidthMm = 1200.0,
                        SlabThicknessMm = 120.0,
                        StartPoint = new Point3D(GeometryHelper.FeetToMm(bbox.Min.X), GeometryHelper.FeetToMm(bbox.Min.Y), GeometryHelper.FeetToMm(bbox.Min.Z)),
                        EndPoint = new Point3D(GeometryHelper.FeetToMm(bbox.Max.X), GeometryHelper.FeetToMm(bbox.Max.Y), GeometryHelper.FeetToMm(bbox.Max.Z))
                    });
                }
            }

            var level = doc.GetElement(stairs.LevelId) as Level;
            if (level != null)
            {
                model.LevelName = level.Name;
            }

            return model;
        }

        public static int BuildStairRebar(Document doc, Autodesk.Revit.DB.Architecture.Stairs hostStairs, StairLayoutResult layoutResult)
        {
            if (hostStairs == null || layoutResult == null || layoutResult.RebarSets.Count == 0)
                return 0;

            int createdCount = 0;

            foreach (var setInfo in layoutResult.RebarSets)
            {
                if (setInfo.CurvePoints.Count < 2) continue;

                try
                {
                    var curves = new List<Curve>();
                    for (int i = 0; i < setInfo.CurvePoints.Count - 1; i++)
                    {
                        var p1 = new XYZ(
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].X),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].Y),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i].Z));

                        var p2 = new XYZ(
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].X),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].Y),
                            GeometryHelper.MmToFeet(setInfo.CurvePoints[i + 1].Z));

                        if (p1.DistanceTo(p2) > 0.001)
                        {
                            curves.Add(Line.CreateBound(p1, p2));
                        }
                    }

                    if (curves.Count == 0) continue;

                    var barInfo = BarCatalog.Parse(setInfo.Diameter);
                    int dia = barInfo?.NominalDiameter ?? 10;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null) continue;

                    XYZ normal = new XYZ(setInfo.DistributionVector.X, setInfo.DistributionVector.Y, setInfo.DistributionVector.Z);
                    if (normal.GetLength() < 0.001) normal = new XYZ(0, 1, 0);

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        null,
                        null,
                        hostStairs,
                        normal,
                        curves,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        JNNTool.Core.Compat.JnnHookOrientation.Right,
                        true,
                        true);

                    if (rebar != null)
                    {
                        if (setInfo.BarCount > 1 && setInfo.SpacingMm > 0)
                        {
                            var accessor = rebar.GetShapeDrivenAccessor();
                            if (accessor != null)
                            {
                                double spacingFeet = GeometryHelper.MmToFeet(setInfo.SpacingMm);
                                accessor.SetLayoutAsNumberWithSpacing(setInfo.BarCount, spacingFeet, true, true, true);
                            }
                        }

                        try
                        {
                            if (doc.ActiveView != null)
                            {
                                rebar.SetUnobscuredInView(doc.ActiveView, true);
                            }
                        }
                        catch { }

                        createdCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warning($"Lỗi tạo RebarSet thang {setInfo.Description}: {ex.Message}");
                }
            }

            return createdCount;
        }
    }
}
