using System;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Column.Models;

namespace JNNTool.RebarSuite.Column.Builders
{
    /// <summary>
    /// Tiện ích tự động tạo Mặt Cắt Dọc (MCD) và Mặt Cắt Ngang (MCN) cho cột.
    /// </summary>
    public static class ColumnSectionBuilder
    {
        public static void CreateSections(Document doc, ColumnStackModel stack, ColumnRebarSettings settings)
        {
            if (stack.Stories == null || stack.Stories.Count == 0 || !settings.CreateColumnSection) return;

            try
            {
                var sectionType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);

                if (sectionType == null) return;

                var firstStory = stack.Stories[0];
                var lastStory = stack.Stories[stack.Stories.Count - 1];

                double botZ = firstStory.BottomElevationMm / 304.8;
                double topZ = lastStory.TopElevationMm / 304.8;
                double midZ = (botZ + topZ) / 2.0;
                double totalH = (topZ - botZ) / 2.0 + 1.0;

                var origin = new XYZ(firstStory.CenterPoint.X / 304.8, firstStory.CenterPoint.Y / 304.8, midZ);
                var dirX = XYZ.BasisX;
                var up = XYZ.BasisZ;
                var viewDir = XYZ.BasisY;

                var bbox = new BoundingBoxXYZ();
                var t = Transform.Identity;
                t.Origin = origin;
                t.BasisX = dirX;
                t.BasisY = up;
                t.BasisZ = viewDir;

                double w = (firstStory.WidthBMm / 304.8) * 2.0;

                bbox.Transform = t;
                bbox.Min = new XYZ(-w, -totalH, -2.0);
                bbox.Max = new XYZ(w, totalH, 2.0);

                var section = ViewSection.CreateSection(doc, sectionType.Id, bbox);
                if (section != null)
                {
                    string baseName = $"{settings.ViewNamePrefix}{stack.GroupName}";
                    string finalName = baseName;
                    int idx = 1;
                    while (new FilteredElementCollector(doc).OfClass(typeof(ViewSection)).Any(v => v.Name == finalName))
                    {
                        finalName = $"{baseName}_{idx++}";
                    }
                    section.Name = finalName;
                }
            }
            catch (Exception ex)
            {
                Logger.Warning($"Không thể tạo mặt cắt cột: {ex.Message}");
            }
        }
    }
}
