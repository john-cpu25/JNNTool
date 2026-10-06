using System;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Core.Units;
using JNNTool.Tools.IFCEtabs.Revit.Parameters;

namespace JNNTool.Tools.IFCEtabs.Revit.Builders
{
    /// <summary>
    /// Tạo cấu kiện vách kết cấu native trong Revit (Wall) từ ETABSWall.
    /// </summary>
    public class WallBuilder
    {
        private readonly Document _doc;
        private WallType? _defaultWallType;

        public WallBuilder(Document doc)
        {
            _doc = doc;
            ResolveWallType();
        }

        private void ResolveWallType()
        {
            // Tìm WallType kết cấu bê tông phù hợp hoặc lấy loại cơ bản mặc định
            var wallTypes = new FilteredElementCollector(_doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wt => wt.Kind == WallKind.Basic)
                .ToList();

            _defaultWallType = wallTypes.FirstOrDefault(wt => wt.Name.Contains("200") || wt.Name.Contains("Concrete"))
                               ?? wallTypes.FirstOrDefault();
        }

        public Wall? CreateWall(ETABSWall wall, Level level, double defaultHeightMm = 3000.0)
        {
            if (wall == null || level == null) return null;

            if (_defaultWallType == null)
            {
                ResolveWallType();
                if (_defaultWallType == null) return null;
            }

            // 1. Xác định 2 điểm chân vách (Bottom line)
            XYZ p1, p2;
            if (wall.BoundaryPoints.Count >= 2)
            {
                // Sắp xếp điểm theo cao độ Z để lấy 2 điểm thấp nhất làm chân vách
                var sorted = wall.BoundaryPoints.OrderBy(p => p.Z).ToList();
                var pt1 = sorted[0];
                var pt2 = sorted[1];

                p1 = new XYZ(UnitConverter.MmToFeet(pt1.X), UnitConverter.MmToFeet(pt1.Y), UnitConverter.MmToFeet(pt1.Z));
                p2 = new XYZ(UnitConverter.MmToFeet(pt2.X), UnitConverter.MmToFeet(pt2.Y), UnitConverter.MmToFeet(pt2.Z));
            }
            else
            {
                p1 = new XYZ(UnitConverter.MmToFeet(wall.BasePointStart.X), UnitConverter.MmToFeet(wall.BasePointStart.Y), UnitConverter.MmToFeet(wall.BasePointStart.Z));
                p2 = new XYZ(UnitConverter.MmToFeet(wall.BasePointEnd.X), UnitConverter.MmToFeet(wall.BasePointEnd.Y), UnitConverter.MmToFeet(wall.BasePointEnd.Z));
            }

            if (p1.DistanceTo(p2) < 0.2) return null; // Quá ngắn

            // Đưa điểm về cùng mặt phẳng ngang của level nếu chênh lệch Z rất nhỏ
            p1 = new XYZ(p1.X, p1.Y, level.Elevation);
            p2 = new XYZ(p2.X, p2.Y, level.Elevation);

            Line line = Line.CreateBound(p1, p2);

            // 2. Chiều cao vách
            double heightMm = wall.HeightMm > 100.0 ? wall.HeightMm : defaultHeightMm;
            double heightFeet = UnitConverter.MmToFeet(heightMm);

            // 3. Tạo Native Wall bằng Revit API
            Wall newWall = Wall.Create(_doc, line, _defaultWallType.Id, level.Id, heightFeet, 0.0, false, true);
            if (newWall == null) return null;

            // 4. Gắn tham số JNN
            AttachParameters(newWall, wall);

            return newWall;
        }

        private static void AttachParameters(Wall wallInstance, ETABSWall wall)
        {
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamSource, wall.Source);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamEtabsId, wall.SourceId);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamSection, wall.Section);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamMaterial, wall.Material);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamStory, wall.Story);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamSourceHash, wall.Hash);
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamConversionStatus, wall.Status.ToString());
            SharedParameterManager.SetParameterValue(wallInstance, SharedParameterManager.ParamConversionDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
        }
    }
}
