using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.CSIxRevit.Models;

using System.Collections.Generic;
using System.Linq;

namespace JNNTool.Tools.CSIxRevit.Builder
{
    public static class BeamBuilder
    {
        public static void Build(Document doc, BeamData beam, PointData p1, PointData p2, FamilySymbol symbol, List<Level> allLevels)
        {
            if (symbol != null && !symbol.IsActive) symbol.Activate();

            double topZ = beam.Elevation;
            Level level = allLevels.OrderBy(l => Math.Abs(l.Elevation - topZ)).FirstOrDefault();
            if (level == null) return;

            XYZ pt1 = new XYZ(p1.Position.X, p1.Position.Y, topZ);
            XYZ pt2 = new XYZ(p2.Position.X, p2.Position.Y, topZ);
            Curve curve = Line.CreateBound(pt1, pt2);
            FamilyInstance instance = doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Beam);
            instance.get_Parameter(BuiltInParameter.Z_OFFSET_VALUE)?.Set(0);
            instance.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION)?.Set(0);
            instance.get_Parameter(BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION)?.Set(0);
        }
    }
}
