#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.StairModel.Engine;

namespace JNNTool.Tools.StairModel.Services
{
    /// <summary>
    /// Chịu trách nhiệm tạo dầm đỡ kết cấu (Support Beams) cho thang.
    /// </summary>
    public static class StairBeamBuilder
    {
        public static FamilyInstance? Build(Document doc, BeamGeometry beam, ElementId symbolId, Level level)
        {
            if (doc == null || beam == null || symbolId == ElementId.InvalidElementId || level == null)
                return null;

            if (doc.GetElement(symbolId) is not FamilySymbol symbol)
                return null;

            if (!symbol.IsActive)
            {
                symbol.Activate();
                doc.Regenerate();
            }

            // Chuyển tọa độ sang Revit feet
            double x1 = UnitUtils.ConvertToInternalUnits(beam.StartPoint.X, UnitTypeId.Millimeters);
            double y1 = UnitUtils.ConvertToInternalUnits(beam.StartPoint.Y, UnitTypeId.Millimeters);
            double z = UnitUtils.ConvertToInternalUnits(beam.ElevationMm, UnitTypeId.Millimeters);

            double x2 = UnitUtils.ConvertToInternalUnits(beam.EndPoint.X, UnitTypeId.Millimeters);
            double y2 = UnitUtils.ConvertToInternalUnits(beam.EndPoint.Y, UnitTypeId.Millimeters);

            XYZ p1 = new XYZ(x1, y1, z);
            XYZ p2 = new XYZ(x2, y2, z);

            if (p1.DistanceTo(p2) < 1e-4)
                return null;

            Curve beamCurve = Line.CreateBound(p1, p2);
            FamilyInstance instance = doc.Create.NewFamilyInstance(beamCurve, symbol, level, StructuralType.Beam);

            if (instance != null)
            {
                // Canh Top mặt trên dầm trùng cao độ sàn
                Parameter? zJustParam = instance.get_Parameter(BuiltInParameter.Z_JUSTIFICATION);
                if (zJustParam != null && !zJustParam.IsReadOnly)
                {
                    zJustParam.Set((int)ZJustification.Top);
                }

                // Gán Comments
                Parameter? commentsParam = instance.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    commentsParam.Set($"JNN_Stair_{beam.Description}");
                }
            }

            return instance;
        }
    }
}
