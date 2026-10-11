#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Tools.AutoDimColumn.Engine;
using JNNTool.Tools.AutoDimColumn.Models;

namespace JNNTool.Tools.AutoDimColumn.Services
{
    /// <summary>
    /// Phát hiện, lọc và ghép nối các Lưới trục (Grid) phù hợp với từng phương của cột.
    /// </summary>
    public static class GridDetectionService
    {
        public static List<Grid> CollectGridsInView(Document doc, View activeView)
        {
            return new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_Grids)
                .WhereElementIsNotElementType()
                .Cast<Grid>()
                .ToList();
        }

        /// <summary>
        /// Tìm các Grid gần nhất và song song với các mặt của cột.
        /// </summary>
        public static List<GridRelationInfo> FindAssociatedGrids(
            ColumnGeometryInfo colInfo,
            List<Grid> grids,
            AutoDimColumnSettings settings)
        {
            var matched = new List<GridRelationInfo>();
            if (!colInfo.IsSupported || (colInfo.WidthPair == null && colInfo.LengthPair == null))
            {
                return matched;
            }

            double searchRangeFt = AutoDimColumnEngine.MmToFt(settings.GridSearchRangeMm);
            double angleTol = settings.AngleToleranceDeg;

            // Xử lý tìm Grid cho phương U (Width)
            if (colInfo.WidthPair != null)
            {
                var bestU = FindBestGridForDirection(colInfo.Center, colInfo.WidthPair.Normal, grids, 0, searchRangeFt, angleTol);
                if (bestU != null) matched.Add(bestU);
            }

            // Xử lý tìm Grid cho phương V (Length)
            if (colInfo.LengthPair != null)
            {
                var bestV = FindBestGridForDirection(colInfo.Center, colInfo.LengthPair.Normal, grids, 1, searchRangeFt, angleTol);
                if (bestV != null) matched.Add(bestV);
            }

            return matched;
        }

        private static GridRelationInfo? FindBestGridForDirection(
            XYZ colCenter,
            XYZ faceNormal,
            List<Grid> grids,
            int targetDir,
            double searchRangeFt,
            double angleTolDeg)
        {
            var candidates = new List<GridRelationInfo>();

            foreach (var grid in grids)
            {
                Curve? curve = grid.Curve;
                if (curve is not Line line) continue;

                XYZ p0 = line.GetEndPoint(0);
                XYZ p1 = line.GetEndPoint(1);
                XYZ gDir = (p1 - p0).Normalize();
                // Pháp tuyến của Grid trên mặt phẳng (2D)
                XYZ gNormal = new XYZ(-gDir.Y, gDir.X, 0).Normalize();

                // Kiểm tra xem gNormal có song song với faceNormal không
                // (Vì mặt cột song song với đường Grid <=> pháp tuyến của chúng cùng phương)
                if (!AutoDimColumnEngine.IsParallel(faceNormal.X, faceNormal.Y, gNormal.X, gNormal.Y, angleTolDeg))
                {
                    continue;
                }

                // Khoảng cách vuông góc từ tâm cột tới Grid Line
                double distFt = AutoDimColumnEngine.PerpendicularDistance(
                    colCenter.X, colCenter.Y,
                    p0.X, p0.Y,
                    gDir.X, gDir.Y
                );

                if (distFt <= searchRangeFt)
                {
                    double angleDiff = AutoDimColumnEngine.AngleBetweenDeg(faceNormal.X, faceNormal.Y, gNormal.X, gNormal.Y);
                    if (angleDiff > 90.0) angleDiff = Math.Abs(180.0 - angleDiff);

                    candidates.Add(new GridRelationInfo
                    {
                        GridId = grid.Id,
                        GridName = grid.Name,
                        GridLine = line,
                        GridDir = gDir,
                        GridNormal = gNormal,
                        PerpendicularDistanceFt = distFt,
                        AngleDiffDeg = angleDiff,
                        TargetDirection = targetDir,
                        GridReference = new Reference(grid)
                    });
                }
            }

            // Chọn Grid gần nhất
            return candidates
                .OrderBy(c => c.PerpendicularDistanceFt)
                .FirstOrDefault();
        }
    }
}
