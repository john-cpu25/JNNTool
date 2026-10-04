using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Wall.Models;

namespace JNNTool.RebarSuite.Wall.Builders
{
    /// <summary>
    /// Trích xuất dữ liệu vách từ Revit và tạo các Rebar/RebarSet trong Document.
    /// </summary>
    public static class WallRebarBuilder
    {
        public static WallModel ExtractWallModel(Document doc, Autodesk.Revit.DB.Wall wall)
        {
            var model = new WallModel
            {
                ElementId = wall.Id.GetIdValue(),
                WallName = wall.Name
            };

            var locCurve = wall.Location as LocationCurve;
            if (locCurve?.Curve != null)
            {
                var curve = locCurve.Curve;
                XYZ p0 = curve.GetEndPoint(0);
                XYZ p1 = curve.GetEndPoint(1);

                model.StartPoint = new Point3D(
                    GeometryHelper.FeetToMm(p0.X),
                    GeometryHelper.FeetToMm(p0.Y),
                    GeometryHelper.FeetToMm(p0.Z));

                model.EndPoint = new Point3D(
                    GeometryHelper.FeetToMm(p1.X),
                    GeometryHelper.FeetToMm(p1.Y),
                    GeometryHelper.FeetToMm(p1.Z));

                model.LengthMm = GeometryHelper.FeetToMm(curve.Length);

                XYZ dir = (p1 - p0).Normalize();
                model.Direction = new Point3D(dir.X, dir.Y, dir.Z);

                XYZ norm = new XYZ(-dir.Y, dir.X, 0).Normalize();
                model.Normal = new Point3D(norm.X, norm.Y, norm.Z);
            }

            model.ThicknessMm = GeometryHelper.FeetToMm(wall.Width);

            var bbox = wall.get_BoundingBox(null);
            if (bbox != null)
            {
                model.BottomElevationMm = GeometryHelper.FeetToMm(bbox.Min.Z);
                model.TopElevationMm = GeometryHelper.FeetToMm(bbox.Max.Z);
                model.HeightMm = model.TopElevationMm - model.BottomElevationMm;
            }

            var level = doc.GetElement(wall.LevelId) as Level;
            if (level != null)
            {
                model.LevelName = level.Name;
            }

            return model;
        }

        public static int BuildWallRebar(Document doc, Autodesk.Revit.DB.Wall hostWall, WallLayoutResult layoutResult)
        {
            if (hostWall == null || layoutResult == null || layoutResult.RebarSets.Count == 0)
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
                    int dia = barInfo?.NominalDiameter ?? 12;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null) continue;

                    // Xác định normal vector cho Rebar
                    XYZ normal;
                    if (setInfo.Role == WallRebarRole.VerticalLayerOuter || setInfo.Role == WallRebarRole.VerticalLayerInner)
                    {
                        // Thép đứng: Normal là vector vuông góc vách
                        normal = new XYZ(layoutResult.Wall.Normal.X, layoutResult.Wall.Normal.Y, layoutResult.Wall.Normal.Z);
                    }
                    else
                    {
                        // Thép ngang và đai C: Normal theo phương đứng Z
                        normal = new XYZ(0, 0, 1);
                    }

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        null,
                        null,
                        hostWall,
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
                    Logger.Warning($"Lỗi tạo RebarSet {setInfo.Description}: {ex.Message}");
                }
            }

            return createdCount;
        }
    }
}
