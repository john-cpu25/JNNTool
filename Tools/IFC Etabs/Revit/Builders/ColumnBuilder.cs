using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo cấu kiện cột kết cấu native trong Revit (Structural Column) từ ETABSColumn.
    /// Thiết lập chính xác Base Level, Top Level, Offsets, góc xoay và tham số JNN.
    /// </summary>
    public class ColumnBuilder
    {
        private readonly Document _doc;
        private readonly TypeAutoCreator _typeCreator;

        public ColumnBuilder(Document doc, TypeAutoCreator typeCreator)
        {
            _doc = doc;
            _typeCreator = typeCreator;
        }

        public FamilyInstance? CreateColumn(ETABSColumn col, Level baseLevel, Level topLevel)
        {
            if (col == null || baseLevel == null || topLevel == null) return null;

            // 1. Chuyển đổi tọa độ mm sang feet
            var basePt = new XYZ(
                UnitConverter.MmToFeet(col.BasePoint.X),
                UnitConverter.MmToFeet(col.BasePoint.Y),
                UnitConverter.MmToFeet(col.BasePoint.Z)
            );

            var topPt = new XYZ(
                UnitConverter.MmToFeet(col.TopPoint.X),
                UnitConverter.MmToFeet(col.TopPoint.Y),
                UnitConverter.MmToFeet(col.TopPoint.Z)
            );

            // 2. Lấy FamilySymbol phù hợp
            FamilySymbol? symbol = _typeCreator.GetOrCreateColumnSymbol(col.Section, col.WidthMm, col.DepthMm);
            if (symbol == null) return null;

            // 3. Tạo FamilyInstance cột tại điểm Base
            FamilyInstance instance = _doc.Create.NewFamilyInstance(basePt, symbol, baseLevel, StructuralType.Column);
            if (instance == null) return null;

            // 4. Thiết lập Top Level và Base/Top Offsets
            try
            {
                Parameter topLevelParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_PARAM);
                if (topLevelParam != null && !topLevelParam.IsReadOnly)
                {
                    topLevelParam.Set(topLevel.Id);
                }

                double baseOffsetFt = basePt.Z - baseLevel.Elevation;
                double topOffsetFt = topPt.Z - topLevel.Elevation;

                Parameter baseOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_OFFSET_PARAM);
                if (baseOffsetParam != null && !baseOffsetParam.IsReadOnly)
                {
                    baseOffsetParam.Set(baseOffsetFt);
                }

                Parameter topOffsetParam = instance.get_Parameter(BuiltInParameter.FAMILY_TOP_LEVEL_OFFSET_PARAM);
                if (topOffsetParam != null && !topOffsetParam.IsReadOnly)
                {
                    topOffsetParam.Set(topOffsetFt);
                }
            }
            catch { }

            // 5. Xoay góc tiết diện nếu có
            if (Math.Abs(col.AngleDegrees) > 1e-3)
            {
                try
                {
                    double rad = col.AngleDegrees * Math.PI / 180.0;
                    Line axis = Line.CreateBound(basePt, basePt + XYZ.BasisZ);
                    ElementTransformUtils.RotateElement(_doc, instance.Id, axis, rad);
                }
                catch { }
            }

            // 6. Gán tham số JNN
            AttachParameters(instance, col);

            return instance;
        }

        public void UpdateColumn(FamilyInstance instance, ETABSColumn col)
        {
            if (instance == null || col == null) return;

            // Đổi type nếu tiết diện thay đổi
            FamilySymbol? symbol = _typeCreator.GetOrCreateColumnSymbol(col.Section, col.WidthMm, col.DepthMm);
            if (symbol != null && instance.Symbol.Id != symbol.Id)
            {
                instance.Symbol = symbol;
            }

            AttachParameters(instance, col);
        }

        private static void AttachParameters(FamilyInstance instance, ETABSColumn col)
        {
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSource, col.Source);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamEtabsId, col.SourceId);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSection, col.Section);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamMaterial, col.Material);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamStory, col.Story);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamSourceHash, col.Hash);
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamConversionStatus, col.Status.ToString());
            SharedParameterManager.SetParameterValue(instance, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
