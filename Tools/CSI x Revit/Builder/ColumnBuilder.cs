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
            if (doc == null || column == null || symbol == null) return;
            if (p1?.Position == null || p2?.Position == null) return;
            if (allLevels == null || allLevels.Count == 0) return;

            try
            {
                if (!symbol.IsActive) symbol.Activate();

                XYZ botPt = new XYZ(p1.Position.X, p1.Position.Y, column.BottomElevation);
                XYZ topPt = new XYZ(p2.Position.X, p2.Position.Y, column.TopElevation);

                if (offset != null)
                {
                    botPt = botPt + offset;
                    topPt = topPt + offset;
                }

                // 1. Xác định Base Level và Top Level theo cao độ Z
                Level baseLevel = allLevels.OrderBy(l => Math.Abs(l.Elevation - botPt.Z)).FirstOrDefault();
                Level topLevel = allLevels.OrderBy(l => Math.Abs(l.Elevation - topPt.Z)).FirstOrDefault();
                if (baseLevel == null) return;

                if (topLevel == null || topLevel.Elevation < baseLevel.Elevation)
                {
                    topLevel = allLevels.Where(l => l.Elevation >= baseLevel.Elevation)
                                        .OrderBy(l => l.Elevation)
                                        .FirstOrDefault() ?? baseLevel;
                }

                // 2. Base Offset tính từ Base Level, Top Offset tính từ Top Level (KHÔNG tính từ Base Level)
                double baseOffset = botPt.Z - baseLevel.Elevation;
                double topOffset = (topLevel.Id != baseLevel.Id)
                    ? (topPt.Z - topLevel.Elevation)
                    : (topPt.Z - baseLevel.Elevation);

                FamilyInstance instance = null;

                if (Math.Abs(botPt.X - topPt.X) < 0.001 && Math.Abs(botPt.Y - topPt.Y) < 0.001)
                {
                    // Cột thẳng đứng (Vertical column)
                    instance = doc.Create.NewFamilyInstance(botPt, symbol, baseLevel, StructuralType.Column);
                    if (instance != null)
                    {
                        try
                        {
                            var topLvlParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                            if (topLvlParam != null && !topLvlParam.IsReadOnly && topLevel.Id != baseLevel.Id)
                            {
                                topLvlParam.Set(topLevel.Id);
                            }

                            var baseOffParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                            if (baseOffParam != null && !baseOffParam.IsReadOnly)
                            {
                                baseOffParam.Set(baseOffset);
                            }

                            var topOffParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                            if (topOffParam != null && !topOffParam.IsReadOnly)
                            {
                                topOffParam.Set(topOffset);
                            }
                        }
                        catch
                        {
                            try
                            {
                                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM)?.Set(topOffset);
                                instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM)?.Set(topLevel.Id);
                                instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM)?.Set(baseOffset);
                            }
                            catch { }
                        }
                    }
                }
                else
                {
                    // Cột xiên (Slanted column)
                    if (botPt.DistanceTo(topPt) < 0.001) return;
                    Curve curve = Line.CreateBound(botPt, topPt);
                    instance = doc.Create.NewFamilyInstance(curve, symbol, baseLevel, StructuralType.Column);
                }

                if (instance != null)
                {

                    // Determine effective rotation angle
                    double etabsW = column.WidthMm;
                    double etabsD = column.DepthMm;
                    double etabsAngle = column.Angle;
                    double targetAngleDeg = etabsAngle;

                    // Extract actual placed Revit dimensions (from instance, symbol, or BoundingBox)
                    double revitB = 0;
                    double revitH = 0;

                    var pB = instance.LookupParameter("b") ?? symbol.LookupParameter("b")
                          ?? instance.LookupParameter("Width") ?? symbol.LookupParameter("Width")
                          ?? instance.LookupParameter("B") ?? symbol.LookupParameter("B")
                          ?? instance.LookupParameter("b0") ?? symbol.LookupParameter("b0")
                          ?? instance.LookupParameter("bf") ?? symbol.LookupParameter("bf")
                          ?? instance.LookupParameter("Chiều rộng") ?? symbol.LookupParameter("Chiều rộng")
                          ?? instance.LookupParameter("b (Width)") ?? symbol.LookupParameter("b (Width)");

                    var pH = instance.LookupParameter("h") ?? symbol.LookupParameter("h")
                          ?? instance.LookupParameter("Depth") ?? symbol.LookupParameter("Depth")
                          ?? instance.LookupParameter("Height") ?? symbol.LookupParameter("Height")
                          ?? instance.LookupParameter("H") ?? symbol.LookupParameter("H")
                          ?? instance.LookupParameter("h0") ?? symbol.LookupParameter("h0")
                          ?? instance.LookupParameter("d") ?? symbol.LookupParameter("d")
                          ?? instance.LookupParameter("Chiều cao") ?? symbol.LookupParameter("Chiều cao")
                          ?? instance.LookupParameter("h (Height)") ?? symbol.LookupParameter("h (Height)");

                    if (pB != null && pH != null)
                    {
                        revitB = pB.AsDouble();
                        revitH = pH.AsDouble();
                    }

                    if (revitB <= 0 || revitH <= 0)
                    {
                        var bbox = instance.get_BoundingBox(null);
                        if (bbox != null)
                        {
                            double dx = Math.Abs(bbox.Max.X - bbox.Min.X);
                            double dy = Math.Abs(bbox.Max.Y - bbox.Min.Y);
                            if (dx > 0.05 && dy > 0.05)
                            {
                                if (revitB <= 0) revitB = dx;
                                if (revitH <= 0) revitH = dy;
                            }
                        }
                    }

                    // Calculate accurate rotation angle using ColumnRotationHelper
                    double revitBMm = revitB > 0 ? revitB * 304.8 : 0;
                    double revitHMm = revitH > 0 ? revitH * 304.8 : 0;
                    double normalizedAngle = ColumnRotationHelper.CalculateRotationAngle(etabsD, etabsW, etabsAngle, revitBMm, revitHMm);

                    if (normalizedAngle > 0.5 && normalizedAngle < 179.5)
                    {
                        try
                        {
                            double angleRad = normalizedAngle * (Math.PI / 180.0);
                            bool isVerticalCol = Math.Abs(botPt.X - topPt.X) < 0.001 && Math.Abs(botPt.Y - topPt.Y) < 0.001;
                            Line axis = isVerticalCol
                                ? Line.CreateBound(botPt, botPt + XYZ.BasisZ * 10.0)
                                : Line.CreateBound(botPt, topPt);

                            bool rotated = false;
                            if (instance.Location is LocationPoint locPt)
                            {
                                try
                                {
                                    rotated = locPt.Rotate(axis, angleRad);
                                }
                                catch { }
                            }

                            if (!rotated)
                            {
                                ElementTransformUtils.RotateElement(doc, instance.Id, axis, angleRad);
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi xoay cột {column.Name}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi dựng cột {column.Name}: {ex.Message}");
            }
        }
    }
}
