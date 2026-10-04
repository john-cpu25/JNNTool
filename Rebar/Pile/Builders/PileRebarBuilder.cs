using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Pile.Models;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Pile.Builders
{
    /// <summary>
    /// Trích xuất dữ liệu cọc từ Revit và tạo cốt thép cọc trong Document.
    /// </summary>
    public static class PileRebarBuilder
    {
        public static PileModel ExtractPileModel(Document doc, FamilyInstance pileInstance)
        {
            var model = new PileModel
            {
                ElementId = pileInstance.Id.GetIdValue(),
                PileName = pileInstance.Name
            };

            var bbox = pileInstance.get_BoundingBox(null);
            if (bbox != null)
            {
                double minX = GeometryHelper.FeetToMm(bbox.Min.X);
                double maxX = GeometryHelper.FeetToMm(bbox.Max.X);
                double minY = GeometryHelper.FeetToMm(bbox.Min.Y);
                double maxY = GeometryHelper.FeetToMm(bbox.Max.Y);
                double minZ = GeometryHelper.FeetToMm(bbox.Min.Z);
                double maxZ = GeometryHelper.FeetToMm(bbox.Max.Z);

                double dx = maxX - minX;
                double dy = maxY - minY;
                double dz = maxZ - minZ;

                model.LengthMm = Math.Max(dz, 1000.0);
                model.TopElevationMm = maxZ;
                model.CenterPoint = new Point3D((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);

                // Kiểm tra cọc tròn hay cọc vuông
                if (Math.Abs(dx - dy) < 20.0)
                {
                    model.DiameterMm = dx;
                    model.WidthBMm = dx;
                    model.HeightHMm = dy;
                }
                else
                {
                    model.WidthBMm = dx;
                    model.HeightHMm = dy;
                }
            }

            var level = doc.GetElement(pileInstance.LevelId) as Level;
            if (level != null)
            {
                model.LevelName = level.Name;
            }

            return model;
        }

        public static int BuildPileRebar(Document doc, FamilyInstance hostPile, PileLayoutResult layoutResult)
        {
            if (hostPile == null || layoutResult == null || layoutResult.RebarSets.Count == 0)
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
                    int dia = barInfo?.NominalDiameter ?? 16;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null) continue;

                    XYZ normal = (setInfo.Role == PileRebarRole.MainLongitudinal)
                        ? new XYZ(1, 0, 0)
                        : new XYZ(0, 0, 1);

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        null,
                        null,
                        hostPile,
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
                    Logger.Warning($"Lỗi tạo RebarSet cọc {setInfo.Description}: {ex.Message}");
                }
            }

            return createdCount;
        }
    }
}
