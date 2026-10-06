using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo cấu kiện dầm kết cấu native trong Revit (Structural Framing) từ ETABSBeam.
    /// </summary>
    public class BeamBuilder
    {
        private readonly Document _doc;
        private readonly TypeAutoCreator _typeCreator;

        public BeamBuilder(Document doc, TypeAutoCreator typeCreator)
        {
            _doc = doc;
            _typeCreator = typeCreator;
        }

        public FamilyInstance? CreateBeam(ETABSBeam beam, Level level)
        {
            if (beam == null || level == null) return null;

            // 1. Chuyển đổi tọa độ mm sang internal Revit (feet)
            var p1 = new XYZ(
                UnitConverter.MmToFeet(beam.StartPoint.X),
                UnitConverter.MmToFeet(beam.StartPoint.Y),
                UnitConverter.MmToFeet(beam.StartPoint.Z)
            );

            var p2 = new XYZ(
                UnitConverter.MmToFeet(beam.EndPoint.X),
                UnitConverter.MmToFeet(beam.EndPoint.Y),
                UnitConverter.MmToFeet(beam.EndPoint.Z)
            );

            if (p1.DistanceTo(p2) < 0.1) return null; // Bỏ qua đoạn quá ngắn tránh lỗi Revit

            // 2. Lấy FamilySymbol phù hợp
            FamilySymbol? symbol = _typeCreator.GetOrCreateBeamSymbol(beam.Section, beam.WidthMm, beam.DepthMm);
            if (symbol == null) return null;

            // 3. Tạo FamilyInstance dầm theo đường Curve
            Curve curve = Line.CreateBound(p1, p2);
            FamilyInstance instance = _doc.Create.NewFamilyInstance(curve, symbol, level, StructuralType.Beam);
            if (instance == null) return null;

            // 4. Thiết lập cao độ Z offset (nếu tâm dầm không nằm đúng cao độ level)
            double levelElevFt = level.Elevation;
            double avgZ = (p1.Z + p2.Z) * 0.5;
            double offsetZ = avgZ - levelElevFt;

            SetParameter(instance, BuiltInParameter.STRUCTURAL_BEAM_END0_ELEVATION, p1.Z - levelElevFt);
            SetParameter(instance, BuiltInParameter.STRUCTURAL_BEAM_END1_ELEVATION, p2.Z - levelElevFt);

            // 5. Gắn thông số JNN Traceability
            AttachParameters(instance, beam);

            return instance;
        }

        public void UpdateBeam(FamilyInstance instance, ETABSBeam beam)
        {
            if (instance == null || beam == null) return;

            // Đổi type nếu tiết diện thay đổi
            FamilySymbol? symbol = _typeCreator.GetOrCreateBeamSymbol(beam.Section, beam.WidthMm, beam.DepthMm);
            if (symbol != null && instance.Symbol.Id != symbol.Id)
            {
                instance.Symbol = symbol;
            }

            AttachParameters(instance, beam);
        }

        private static void AttachParameters(FamilyInstance instance, ETABSBeam beam)
        {
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSource, beam.Source);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamEtabsId, beam.SourceId);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSection, beam.Section);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamMaterial, beam.Material);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamStory, beam.Story);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSourceHash, beam.Hash);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamConversionStatus, beam.Status.ToString());
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }

        private static void SetParameter(Element el, BuiltInParameter bip, double val)
        {
            Parameter p = el.get_Parameter(bip);
            if (p != null && !p.IsReadOnly)
            {
                p.Set(val);
            }
        }
    }
}
