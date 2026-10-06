using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.CSIxRevit.Models;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class EdgeAlignmentHelper
    {
        public static Dictionary<string, XYZ> CalculateNodeOffsets(
            List<PointData> points,
            List<BeamData> beams,
            List<ColumnData> columns,
            Dictionary<string, SectionDefinition> sectionDefs)
        {
            var offsets = new Dictionary<string, XYZ>(StringComparer.OrdinalIgnoreCase);
            var ptMap = points.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var pt in points)
            {
                var col = columns.FirstOrDefault(c => 
                    c.PointI.Equals(pt.Name, StringComparison.OrdinalIgnoreCase) || 
                    c.PointJ.Equals(pt.Name, StringComparison.OrdinalIgnoreCase));

                if (col == null)
                {
                    offsets[pt.Name] = XYZ.Zero;
                    continue;
                }

                double colWidthMm = col.WidthMm;
                double colDepthMm = col.DepthMm;

                if ((colWidthMm <= 0 || colDepthMm <= 0) && sectionDefs != null)
                {
                    if (sectionDefs.TryGetValue(col.Section, out var secDef))
                    {
                        if (colWidthMm <= 0) colWidthMm = secDef.WidthMm;
                        if (colDepthMm <= 0) colDepthMm = secDef.DepthMm;
                    }
                }

                if (colWidthMm <= 0) colWidthMm = 400;
                if (colDepthMm <= 0) colDepthMm = colWidthMm;

                // In ETABS rectangular sections: D is along Global X (angle 0) and W is along Global Y
                double effectiveHalfExtentX_mm = colDepthMm / 2.0;
                double effectiveHalfExtentY_mm = colWidthMm / 2.0;

                double normAngle = ((col.Angle % 180.0) + 180.0) % 180.0;
                if (Math.Abs(normAngle - 90.0) < 15.0)
                {
                    effectiveHalfExtentX_mm = colWidthMm / 2.0;
                    effectiveHalfExtentY_mm = colDepthMm / 2.0;
                }

                double halfWidthFeet = UnitUtils.ConvertToInternalUnits(effectiveHalfExtentX_mm, UnitTypeId.Millimeters);
                double halfDepthFeet = UnitUtils.ConvertToInternalUnits(effectiveHalfExtentY_mm, UnitTypeId.Millimeters);

                var connectedBeams = beams.Where(b => 
                    b.PointI.Equals(pt.Name, StringComparison.OrdinalIgnoreCase) || 
                    b.PointJ.Equals(pt.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                if (connectedBeams.Count == 0)
                {
                    offsets[pt.Name] = XYZ.Zero;
                    continue;
                }

                bool hasPosX = false, hasNegX = false;
                bool hasPosY = false, hasNegY = false;

                foreach (var b in connectedBeams)
                {
                    string otherPtName = b.PointI.Equals(pt.Name, StringComparison.OrdinalIgnoreCase) ? b.PointJ : b.PointI;
                    if (ptMap.TryGetValue(otherPtName, out var otherPt))
                    {
                        double dx = otherPt.Position.X - pt.Position.X;
                        double dy = otherPt.Position.Y - pt.Position.Y;

                        if (dx > 0.3) hasPosX = true;
                        if (dx < -0.3) hasNegX = true;
                        if (dy > 0.3) hasPosY = true;
                        if (dy < -0.3) hasNegY = true;
                    }
                }

                double shiftX = 0;
                double shiftY = 0;

                // Edge column shift inward
                if (hasPosX && !hasNegX) shiftX = halfWidthFeet;
                else if (hasNegX && !hasPosX) shiftX = -halfWidthFeet;

                if (hasPosY && !hasNegY) shiftY = halfDepthFeet;
                else if (hasNegY && !hasPosY) shiftY = -halfDepthFeet;

                offsets[pt.Name] = new XYZ(shiftX, shiftY, 0);
            }

            // 2. Second pass: propagate offsets to intermediate beam joints (points without columns)
            // If an intermediate point lies on a continuous edge beam along X, inherit the neighbor's shiftY.
            // If it lies on a continuous edge beam along Y, inherit the neighbor's shiftX.
            foreach (var pt in points)
            {
                if (offsets.TryGetValue(pt.Name, out var curOffset) && curOffset.IsAlmostEqualTo(XYZ.Zero))
                {
                    var connectedBeams = beams.Where(b => 
                        b.PointI.Equals(pt.Name, StringComparison.OrdinalIgnoreCase) || 
                        b.PointJ.Equals(pt.Name, StringComparison.OrdinalIgnoreCase)).ToList();

                    double propShiftX = 0;
                    double propShiftY = 0;

                    foreach (var b in connectedBeams)
                    {
                        string otherPtName = b.PointI.Equals(pt.Name, StringComparison.OrdinalIgnoreCase) ? b.PointJ : b.PointI;
                        if (offsets.TryGetValue(otherPtName, out var otherOffset) && !otherOffset.IsAlmostEqualTo(XYZ.Zero))
                        {
                            if (ptMap.TryGetValue(otherPtName, out var otherPt))
                            {
                                double dx = Math.Abs(otherPt.Position.X - pt.Position.X);
                                double dy = Math.Abs(otherPt.Position.Y - pt.Position.Y);

                                // If collinear along X (same Y), inherit shiftY from the edge beam to avoid ziczac teeth
                                if (dy < 0.05 && dx > 0.05 && Math.Abs(otherOffset.Y) > 0.001)
                                {
                                    propShiftY = otherOffset.Y;
                                }
                                // If collinear along Y (same X), inherit shiftX from the edge beam to avoid ziczac teeth
                                if (dx < 0.05 && dy > 0.05 && Math.Abs(otherOffset.X) > 0.001)
                                {
                                    propShiftX = otherOffset.X;
                                }
                            }
                        }
                    }

                    if (Math.Abs(propShiftX) > 0.001 || Math.Abs(propShiftY) > 0.001)
                    {
                        offsets[pt.Name] = new XYZ(propShiftX, propShiftY, 0);
                    }
                }
            }

            return offsets;
        }
    }
}
