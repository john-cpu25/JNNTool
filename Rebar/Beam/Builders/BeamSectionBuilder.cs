using System;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Beam.Models;

namespace JNNTool.RebarSuite.Beam.Builders
{
    /// <summary>
    /// Tiện ích tự động tạo Mặt Cắt Dọc (MCD) và Mặt Cắt Ngang (MCN) cho chuỗi dầm liên tục.
    /// </summary>
    public static class BeamSectionBuilder
    {
        public static void CreateSections(Document doc, ContinuousBeamModel beamModel, BeamRebarSettings settings)
        {
            if (beamModel.Spans == null || beamModel.Spans.Count == 0) return;

            try
            {
                var sectionType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);

                if (sectionType == null) return;

                var firstSpan = beamModel.Spans[0];
                var lastSpan = beamModel.Spans[beamModel.Spans.Count - 1];

                // 1. Tạo Mặt Cắt Dọc (MCD)
                if (settings.CreateLongitudinalSection)
                {
                    string mcdName = $"{settings.ViewNamePrefix}{beamModel.GroupName}";
                    CreateLongitudinalView(doc, sectionType, firstSpan, lastSpan, mcdName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi tự động tạo mặt cắt dầm", ex);
            }
        }

        private static void CreateLongitudinalView(
            Document doc,
            ViewFamilyType sectionType,
            BeamSpanModel firstSpan,
            BeamSpanModel lastSpan,
            string viewName)
        {
            try
            {
                var p0 = new XYZ(firstSpan.StartPoint.X / 304.8, firstSpan.StartPoint.Y / 304.8, firstSpan.StartPoint.Z / 304.8);
                var p1 = new XYZ(lastSpan.EndPoint.X / 304.8, lastSpan.EndPoint.Y / 304.8, lastSpan.EndPoint.Z / 304.8);

                var dir = (p1 - p0).Normalize();
                var up = XYZ.BasisZ;
                var viewDir = dir.CrossProduct(up);

                var bbox = new BoundingBoxXYZ();
                var t = Transform.Identity;
                t.Origin = (p0 + p1) / 2.0;
                t.BasisX = dir;
                t.BasisY = up;
                t.BasisZ = viewDir;

                double len = p0.DistanceTo(p1) / 2.0 + 1.0;
                double h = (firstSpan.HeightMm / 304.8) * 2.0;

                bbox.Transform = t;
                bbox.Min = new XYZ(-len, -h, -2.0);
                bbox.Max = new XYZ(len, h, 2.0);

                var section = ViewSection.CreateSection(doc, sectionType.Id, bbox);
                if (section != null)
                {
                    // Tránh trùng tên view
                    string finalName = viewName;
                    int index = 1;
                    while (new FilteredElementCollector(doc).OfClass(typeof(ViewSection)).Any(v => v.Name == finalName))
                    {
                        finalName = $"{viewName}_{index++}";
                    }
                    section.Name = finalName;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Không thể tạo mặt cắt dọc: {ex.Message}");
            }
        }
    }
}
