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

                double halfWidthFeet = UnitUtils.ConvertToInternalUnits(colWidthMm / 2.0, UnitTypeId.Millimeters);
                double halfDepthFeet = UnitUtils.ConvertToInternalUnits(colDepthMm / 2.0, UnitTypeId.Millimeters);

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

            return offsets;
        }
    }
}
