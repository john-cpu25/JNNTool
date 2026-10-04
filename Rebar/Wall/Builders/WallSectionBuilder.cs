using System;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Wall.Models;

namespace JNNTool.RebarSuite.Wall.Builders
{
    /// <summary>
    /// Tiện ích tự động tạo Mặt Cắt Dọc (MCD) cho vách bê tông cốt thép.
    /// </summary>
    public static class WallSectionBuilder
    {
        public static void CreateWallSection(Document doc, WallModel wall, WallRebarSettings settings)
        {
            if (wall == null || !settings.CreateWallSection) return;

            try
            {
                var sectionType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.Section);

                if (sectionType == null) return;

                double lengthFeet = wall.LengthMm / 304.8;
                double heightFeet = wall.HeightMm / 304.8;
                double thickFeet = wall.ThicknessMm / 304.8;

                XYZ midPoint = new XYZ(
                    (wall.StartPoint.X + wall.EndPoint.X) / 2.0 / 304.8,
                    (wall.StartPoint.Y + wall.EndPoint.Y) / 2.0 / 304.8,
                    (wall.BottomElevationMm + wall.TopElevationMm) / 2.0 / 304.8);

                XYZ dirX = new XYZ(wall.Direction.X, wall.Direction.Y, 0).Normalize();
                XYZ up = XYZ.BasisZ;
                XYZ viewDir = new XYZ(wall.Normal.X, wall.Normal.Y, 0).Normalize();

                var t = Transform.Identity;
                t.Origin = midPoint;
                t.BasisX = dirX;
                t.BasisY = up;
                t.BasisZ = viewDir;

                var bbox = new BoundingBoxXYZ();
                bbox.Transform = t;

                double padding = 1.5; // feet
                bbox.Min = new XYZ(-lengthFeet / 2.0 - padding, -heightFeet / 2.0 - padding, -thickFeet - 1.0);
                bbox.Max = new XYZ(lengthFeet / 2.0 + padding, heightFeet / 2.0 + padding, thickFeet + 1.0);

                var section = ViewSection.CreateSection(doc, sectionType.Id, bbox);
                if (section != null)
                {
                    string baseName = $"{settings.ViewNamePrefix}{wall.WallName}";
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
                Logger.Warning($"Không thể tạo mặt cắt vách: {ex.Message}");
            }
        }
    }
}
