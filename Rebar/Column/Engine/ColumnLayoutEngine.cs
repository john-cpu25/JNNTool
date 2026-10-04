using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Column.Models;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Column.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán phân bổ cốt thép dọc và cốt đai chuỗi cột theo tầng (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class ColumnLayoutEngine
    {
        public static ColumnLayoutResult Calculate(ColumnStackModel stack, ColumnRebarSettings settings)
        {
            var result = new ColumnLayoutResult(stack, settings);
            if (stack.Stories == null || stack.Stories.Count == 0)
                return result;

            var barMain = BarCatalog.Parse(settings.MainDiameter) ?? new BarInfo(20);
            var barStirrup = BarCatalog.Parse(settings.StirrupDiameter) ?? new BarInfo(8);
            double cover = settings.CoverMm;

            // Duyệt từng tầng trong chuỗi cột
            for (int i = 0; i < stack.Stories.Count; i++)
            {
                var story = stack.Stories[i];
                bool isTopStory = (i == stack.Stories.Count - 1);

                // ==========================================================
                // 1. Thép Dọc Cột (Main Vertical Rebars)
                // ==========================================================
                CalculateVerticalBars(story, stack, i, settings, barMain, cover, result);

                // ==========================================================
                // 2. Cốt Đai Cột (Stirrups & Ties)
                // ==========================================================
                CalculateStirrups(story, settings, barStirrup, cover, result);
            }

            return result;
        }

        /// <summary>
        /// Tính toán tọa độ và đoạn uốn cổ chai của các thanh thép dọc cột.
        /// </summary>
        public static void CalculateVerticalBars(
            ColumnStoryModel story,
            ColumnStackModel stack,
            int storyIndex,
            ColumnRebarSettings settings,
            BarInfo barMain,
            double cover,
            ColumnLayoutResult result)
        {
            int nB = Math.Max(settings.CountB, 2);
            int nH = Math.Max(settings.CountH, 2);
            int totalBars = 2 * (nB + nH - 2);

            double b = story.WidthBMm;
            double h = story.HeightHMm;
            double d = barMain.DiameterMm;

            double innerB = b - 2 * cover - d;
            double innerH = h - 2 * cover - d;

            double stepB = nB > 1 ? innerB / (nB - 1) : 0;
            double stepH = nH > 1 ? innerH / (nH - 1) : 0;

            double zBot = story.BottomElevationMm;
            double zTop = story.TopElevationMm;
            bool isTopStory = (storyIndex == stack.Stories.Count - 1);

            double lapLength = settings.LapLengthMultiplier * d;
            double zBarTop = isTopStory ? zTop - cover : zTop + lapLength;

            // Kiểm tra có bẻ cổ chai không khi chuyển lên tầng tiếp theo (độ lệch mỗi mép <= 75mm)
            bool hasSectionChange = stack.HasSectionChange(storyIndex, out double deltaB, out double deltaH);
            bool cranked = hasSectionChange && settings.EnableCrankedLap && (deltaB > 0 || deltaH > 0) && (deltaB / 2.0 <= 75 && deltaH / 2.0 <= 75);

            var barPositions2D = new List<(double X, double Y)>();

            // Cạnh B dưới (Y = -innerH/2)
            for (int col = 0; col < nB; col++)
                barPositions2D.Add((-innerB / 2.0 + col * stepB, -innerH / 2.0));

            // Cạnh H phải (X = innerB/2)
            for (int row = 1; row < nH - 1; row++)
                barPositions2D.Add((innerB / 2.0, -innerH / 2.0 + row * stepH));

            // Cạnh B trên (Y = innerH/2)
            for (int col = nB - 1; col >= 0; col--)
                barPositions2D.Add((-innerB / 2.0 + col * stepB, innerH / 2.0));

            // Cạnh H trái (X = -innerB/2)
            for (int row = nH - 2; row >= 1; row--)
                barPositions2D.Add((-innerB / 2.0, -innerH / 2.0 + row * stepH));

            // Sinh từng thanh dọc
            for (int barIdx = 0; barIdx < barPositions2D.Count; barIdx++)
            {
                var pos = barPositions2D[barIdx];
                var vertBar = new ColumnRebarSetLayoutInfo
                {
                    Role = cranked ? ColumnRebarRole.VerticalCranked : ColumnRebarRole.VerticalMain,
                    Diameter = barMain.Name,
                    BarCount = 1,
                    Description = $"Thép dọc cột Tầng {story.LevelName} (#{barIdx + 1})"
                };

                // Điểm chân thanh thép
                vertBar.CurvePoints.Add(new Point3D(pos.X, pos.Y, zBot));

                if (cranked && !isTopStory)
                {
                    // Vùng bẻ cổ chai tại cao độ mặt sàn tầng trên
                    double crankSlopeH = Math.Max(deltaB / 2.0, deltaH / 2.0) * 6.0; // Tỉ lệ 1:6
                    double zCrankStart = zTop - story.IntersectingBeamDepthMm;
                    double zCrankEnd = zCrankStart + crankSlopeH;

                    vertBar.CurvePoints.Add(new Point3D(pos.X, pos.Y, zCrankStart));

                    // Điểm uốn thu vào trong
                    double offsetX = (pos.X > 0) ? -deltaB / 2.0 : deltaB / 2.0;
                    double offsetY = (pos.Y > 0) ? -deltaH / 2.0 : deltaH / 2.0;
                    vertBar.CurvePoints.Add(new Point3D(pos.X + offsetX, pos.Y + offsetY, zCrankEnd));
                    vertBar.CurvePoints.Add(new Point3D(pos.X + offsetX, pos.Y + offsetY, zBarTop));
                }
                else
                {
                    vertBar.CurvePoints.Add(new Point3D(pos.X, pos.Y, zBarTop));
                }

                result.RebarSets.Add(vertBar);
            }
        }

        /// <summary>
        /// Tính toán phân bổ cốt đai vùng dày (chân/đầu) và vùng thưa thân cột.
        /// </summary>
        public static void CalculateStirrups(
            ColumnStoryModel story,
            ColumnRebarSettings settings,
            BarInfo barStirrup,
            double cover,
            ColumnLayoutResult result)
        {
            double clearH = story.ClearHeightMm;

            // Chiều cao vùng đai dày theo TCVN: max(H_clear/6, B, H, 500mm)
            double denseH = Math.Max(clearH / 6.0, Math.Max(story.WidthBMm, story.HeightHMm));
            denseH = Math.Max(denseH, 500.0);
            denseH = GeometryHelper.RoundUpToMultiple(denseH, 50);

            double midSparseH = Math.Max(clearH - 2 * denseH, 0);

            // 1. Đai dày chân cột
            GeometryHelper.CalculateSpacing(denseH, 50, 0, settings.StirrupSpacingDense, out int countBase, out double spacingBase);
            var stirrupBase = new ColumnRebarSetLayoutInfo
            {
                Role = ColumnRebarRole.StirrupBaseDense,
                Diameter = barStirrup.Name,
                BarCount = countBase,
                SpacingMm = spacingBase,
                RangeHeightMm = denseH,
                Description = $"Đai dày chân cột Tầng {story.LevelName}"
            };
            AddColumnStirrupPoints(stirrupBase, story.WidthBMm, story.HeightHMm, cover);
            result.RebarSets.Add(stirrupBase);

            // 2. Đai thưa thân cột
            int countMid = 0;
            if (midSparseH > 100)
            {
                GeometryHelper.CalculateSpacing(midSparseH, 0, 0, settings.StirrupSpacingSparse, out countMid, out double spacingMid);
                var stirrupMid = new ColumnRebarSetLayoutInfo
                {
                    Role = ColumnRebarRole.StirrupMidSparse,
                    Diameter = barStirrup.Name,
                    BarCount = countMid,
                    SpacingMm = spacingMid,
                    RangeHeightMm = midSparseH,
                    Description = $"Đai thưa thân cột Tầng {story.LevelName}"
                };
                AddColumnStirrupPoints(stirrupMid, story.WidthBMm, story.HeightHMm, cover);
                result.RebarSets.Add(stirrupMid);
            }

            // 3. Đai dày đầu cột (dưới đáy dầm)
            GeometryHelper.CalculateSpacing(denseH, 0, 50, settings.StirrupSpacingDense, out int countTop, out double spacingTop);
            var stirrupTop = new ColumnRebarSetLayoutInfo
            {
                Role = ColumnRebarRole.StirrupTopDense,
                Diameter = barStirrup.Name,
                BarCount = countTop,
                SpacingMm = spacingTop,
                RangeHeightMm = denseH,
                Description = $"Đai dày đầu cột Tầng {story.LevelName}"
            };
            AddColumnStirrupPoints(stirrupTop, story.WidthBMm, story.HeightHMm, cover);
            result.RebarSets.Add(stirrupTop);

            // 4. Đai lồng trong (kiểu AB) nếu được chọn
            if (settings.StirrupPattern == ColumnStirrupPattern.AB && story.WidthBMm >= 350 && story.HeightHMm >= 350)
            {
                var internalStirrup = new ColumnRebarSetLayoutInfo
                {
                    Role = ColumnRebarRole.InternalStirrupOrTie,
                    Diameter = barStirrup.Name,
                    BarCount = countBase + countMid + countTop,
                    SpacingMm = settings.StirrupSpacingSparse,
                    Description = $"Đai lồng trong cột Tầng {story.LevelName}"
                };
                // Đai lồng hình chữ nhật nhỏ bên trong
                double wIn = (story.WidthBMm - 2 * cover) / 2.0;
                double hIn = (story.HeightHMm - 2 * cover) / 2.0;
                internalStirrup.CurvePoints.Add(new Point3D(-wIn / 2.0, -hIn / 2.0, 0));
                internalStirrup.CurvePoints.Add(new Point3D(wIn / 2.0, -hIn / 2.0, 0));
                internalStirrup.CurvePoints.Add(new Point3D(wIn / 2.0, hIn / 2.0, 0));
                internalStirrup.CurvePoints.Add(new Point3D(-wIn / 2.0, hIn / 2.0, 0));
                internalStirrup.CurvePoints.Add(new Point3D(-wIn / 2.0, -hIn / 2.0, 0));
                result.RebarSets.Add(internalStirrup);
            }
        }

        private static void AddColumnStirrupPoints(ColumnRebarSetLayoutInfo set, double b, double h, double cover)
        {
            double halfW = b / 2.0 - cover;
            double halfH = h / 2.0 - cover;

            // Hình chữ nhật đai kín
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, 0));
            set.CurvePoints.Add(new Point3D(halfW, -halfH, 0));
            set.CurvePoints.Add(new Point3D(halfW, halfH, 0));
            set.CurvePoints.Add(new Point3D(-halfW, halfH, 0));
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, 0));
        }
    }
}
