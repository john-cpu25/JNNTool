using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class BeamBuilder
    {
        public static void Build(Document doc, BeamData beam, PointData p1, PointData p2, FamilySymbol symbol, List<Level> allLevels)
        {
            Build(doc, beam, p1, p2, symbol, allLevels, null, null, true);
        }

        public static void Build(
            Document doc,
            BeamData beam,
            PointData p1,
            PointData p2,
            FamilySymbol symbol,
            List<Level> allLevels,
            XYZ offset1 = null,
            XYZ offset2 = null,
            bool applyCardinalPoint = true)
        {
            if (doc == null || beam == null || symbol == null) return;
            if (p1?.Position == null || p2?.Position == null) return;
            if (allLevels == null || allLevels.Count == 0) return;

            try
            {
                if (!symbol.IsActive) symbol.Activate();

                double topZ = beam.Elevation;
                Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - topZ)).FirstOrDefault();
                if (level == null) return;

                XYZ pt1 = new XYZ(p1.Position.X, p1.Position.Y, topZ);
                XYZ pt2 = new XYZ(p2.Position.X, p2.Position.Y, topZ);

                if (offset1 != null) pt1 = pt1 + offset1;
                if (offset2 != null) pt2 = pt2 + offset2;

                if (pt1.IsAlmostEqualTo(pt2) || pt1.DistanceTo(pt2) < 0.001) return;

                Curve curve = Line.CreateBound(pt1, pt2);
                FamilyInstance instance = doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Beam);
                if (instance == null) return;

                if (applyCardinalPoint)
                {
                    // CSI Cardinal Points:
                    // 1..3: Bottom, 4..6, 10: Center, 7..9: Top
                    // Revit Z_JUSTIFICATION: 0 = Top, 1 = Center, 2 = Bottom
                    int zJust = 0; // Top
                    if (beam.CardinalPoint >= 1 && beam.CardinalPoint <= 3) zJust = 2;
                    else if ((beam.CardinalPoint >= 4 && beam.CardinalPoint <= 6) || beam.CardinalPoint == 10) zJust = 1;
                    else zJust = 0;

                    // CSI Y Justification:
                    // 1,4,7: Left, 2,5,8: Center, 3,6,9: Right
                    // Revit Y_JUSTIFICATION: 0 = Left, 1 = Center, 2 = Right
                    int yJust = 1; // Center
                    if (beam.CardinalPoint == 1 || beam.CardinalPoint == 4 || beam.CardinalPoint == 7) yJust = 0;
                    else if (beam.CardinalPoint == 3 || beam.CardinalPoint == 6 || beam.CardinalPoint == 9) yJust = 2;
                    else yJust = 1;

                    try
                    {
                        var zJustParam = instance.get_Parameter(BuiltInParameter.Z_JUSTIFICATION);
                        if (zJustParam != null && !zJustParam.IsReadOnly)
                        {
                            zJustParam.Set(zJust);
                        }
                        else if (beam.DepthMm > 0)
                        {
                            // Fallback when Z_JUSTIFICATION is not editable: shift via Z_OFFSET_VALUE
                            double depthFeet = UnitUtils.ConvertToInternalUnits(beam.DepthMm, UnitTypeId.Millimeters);
                            double zOffset = 0;
                            if (zJust == 0) zOffset = -depthFeet / 2.0;
                            else if (zJust == 2) zOffset = depthFeet / 2.0;
                            instance.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)?.Set(zOffset);
                        }

                        var yJustParam = instance.get_Parameter(BuiltInParameter.Y_JUSTIFICATION);
                        if (yJustParam != null && !yJustParam.IsReadOnly)
                        {
                            yJustParam.Set(yJust);
                        }
                    }
                    catch { }
                }
                else
                {
                    try
                    {
                        instance.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)?.Set(0);
                    }
                    catch { }
                }

                try
                {
                    instance.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION)?.Set(0);
                    instance.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION)?.Set(0);
                }
                catch { }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi dựng dầm {beam.Name}: {ex.Message}");
            }
        }
    }
}
