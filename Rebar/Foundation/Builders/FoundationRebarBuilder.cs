using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Core.Compat;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Foundation.Models;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Foundation.Builders
{
    /// <summary>
    /// Trích xuất dữ liệu móng từ Revit và tạo cốt thép móng trong Document.
    /// </summary>
    public static class FoundationRebarBuilder
    {
        public static FoundationModel ExtractFoundationModel(Document doc, FamilyInstance footing)
        {
            var model = new FoundationModel
            {
                ElementId = footing.Id.GetIdValue(),
                FoundationName = footing.Name
            };

            var bbox = footing.get_BoundingBox(null);
            if (bbox != null)
            {
                double minX = GeometryHelper.FeetToMm(bbox.Min.X);
                double maxX = GeometryHelper.FeetToMm(bbox.Max.X);
                double minY = GeometryHelper.FeetToMm(bbox.Min.Y);
                double maxY = GeometryHelper.FeetToMm(bbox.Max.Y);
                double minZ = GeometryHelper.FeetToMm(bbox.Min.Z);
                double maxZ = GeometryHelper.FeetToMm(bbox.Max.Z);

                model.LengthXMm = Math.Max(maxX - minX, 200.0);
                model.WidthYMm = Math.Max(maxY - minY, 200.0);
                model.HeightZMm = Math.Max(maxZ - minZ, 200.0);

                model.BottomElevationMm = minZ;
                model.CenterPoint = new Point3D((minX + maxX) / 2.0, (minY + maxY) / 2.0, (minZ + maxZ) / 2.0);
            }

            var level = doc.GetElement(footing.LevelId) as Level;
            if (level != null)
            {
                model.LevelName = level.Name;
            }

            return model;
        }

        public static int BuildFoundationRebar(Document doc, FamilyInstance hostFooting, FoundationLayoutResult layoutResult)
        {
            if (hostFooting == null || layoutResult == null || layoutResult.RebarSets.Count == 0)
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
                    int dia = barInfo?.NominalDiameter ?? 14;
                    var barType = RebarTypeResolver.ResolveBarType(doc, dia);
                    if (barType == null) continue;

                    XYZ normal;
                    if (setInfo.Role == FoundationRebarRole.BottomLayerX || setInfo.Role == FoundationRebarRole.TopLayerX)
                    {
                        normal = new XYZ(0, 1, 0); // rải theo Y
                    }
                    else if (setInfo.Role == FoundationRebarRole.BottomLayerY || setInfo.Role == FoundationRebarRole.TopLayerY)
                    {
                        normal = new XYZ(1, 0, 0); // rải theo X
                    }
                    else
                    {
                        normal = new XYZ(0, 0, 1);
                    }

                    var rebar = JNNTool.Core.Compat.RebarCompat.CreateFromCurves(
                        doc,
                        RebarStyle.Standard,
                        barType,
                        null,
                        null,
                        hostFooting,
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
                    Logger.Warning($"Lỗi tạo RebarSet móng {setInfo.Description}: {ex.Message}");
                }
            }

            return createdCount;
        }
    }
}
