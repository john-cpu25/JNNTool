using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Foundation.Models;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Foundation.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán cốt thép móng đơn, móng băng và đài cọc theo TCVN 5574:2018 (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class FoundationLayoutEngine
    {
        public static FoundationLayoutResult Calculate(FoundationModel foundation, FoundationRebarSettings settings)
        {
            var result = new FoundationLayoutResult(foundation, settings);
            if (foundation == null || foundation.LengthXMm <= 100 || foundation.WidthYMm <= 100 || foundation.HeightZMm <= 100)
                return result;

            var barBotX = BarCatalog.Parse(settings.BottomDiameterX) ?? new BarInfo(14);
            var barBotY = BarCatalog.Parse(settings.BottomDiameterY) ?? new BarInfo(14);

            double lx = foundation.LengthXMm;
            double ly = foundation.WidthYMm;
            double hz = foundation.HeightZMm;

            double cx = foundation.CenterPoint.X;
            double cy = foundation.CenterPoint.Y;

            double xMin = cx - lx / 2.0;
            double xMax = cx + lx / 2.0;
            double yMin = cy - ly / 2.0;
            double yMax = cy + ly / 2.0;

            double zBot = foundation.BottomElevationMm;
            double zTop = foundation.TopElevationMm;

            double cBot = settings.CoverBottomMm;
            double cSide = settings.CoverSideMm;
            double cTop = settings.CoverTopMm;

            // ==============================================================
            // 1. Thép Lưới Đáy Phương X (Chạy dọc X, rải theo Y)
            // ==============================================================
            double zBarBotX = zBot + cBot + barBotX.DiameterMm / 2.0;
            double hookHeightX = settings.HookBottomBarsUp 
                ? Math.Min(settings.BottomHookHeightMm, hz - cBot - cTop) 
                : 0.0;

            double rangeY = (yMax - cSide) - (yMin + cSide);
            if (rangeY < 50.0) rangeY = 50.0;
            int countX = Math.Max(2, (int)Math.Ceiling(rangeY / settings.BottomSpacingX) + 1);
            double actualSpacingX = rangeY / (countX - 1);

            Point3D px1 = new Point3D(xMin + cSide, yMin + cSide, zBarBotX + hookHeightX);
            Point3D px2 = new Point3D(xMin + cSide, yMin + cSide, zBarBotX);
            Point3D px3 = new Point3D(xMax - cSide, yMin + cSide, zBarBotX);
            Point3D px4 = new Point3D(xMax - cSide, yMin + cSide, zBarBotX + hookHeightX);

            var curvePtsX = new List<Point3D>();
            if (hookHeightX > 10.0) curvePtsX.Add(px1);
            curvePtsX.Add(px2);
            curvePtsX.Add(px3);
            if (hookHeightX > 10.0) curvePtsX.Add(px4);

            result.RebarSets.Add(new FoundationRebarSetLayoutInfo
            {
                Role = FoundationRebarRole.BottomLayerX,
                Diameter = settings.BottomDiameterX,
                BarCount = countX,
                SpacingMm = actualSpacingX,
                RangeLengthMm = rangeY,
                CurvePoints = curvePtsX,
                DistributionVector = new Point3D(0, 1, 0),
                Description = $"Thép đáy móng phương X ({settings.BottomDiameterX} a{actualSpacingX:F0}, {countX} thanh)"
            });

            // ==============================================================
            // 2. Thép Lưới Đáy Phương Y (Chạy dọc Y, rải theo X)
            // ==============================================================
            double zBarBotY = zBarBotX + barBotX.DiameterMm / 2.0 + barBotY.DiameterMm / 2.0;
            double hookHeightY = settings.HookBottomBarsUp 
                ? Math.Min(settings.BottomHookHeightMm, hz - cBot - cTop) 
                : 0.0;

            double rangeX = (xMax - cSide) - (xMin + cSide);
            if (rangeX < 50.0) rangeX = 50.0;
            int countY = Math.Max(2, (int)Math.Ceiling(rangeX / settings.BottomSpacingY) + 1);
            double actualSpacingY = rangeX / (countY - 1);

            Point3D py1 = new Point3D(xMin + cSide, yMin + cSide, zBarBotY + hookHeightY);
            Point3D py2 = new Point3D(xMin + cSide, yMin + cSide, zBarBotY);
            Point3D py3 = new Point3D(xMin + cSide, yMax - cSide, zBarBotY);
            Point3D py4 = new Point3D(xMin + cSide, yMax - cSide, zBarBotY + hookHeightY);

            var curvePtsY = new List<Point3D>();
            if (hookHeightY > 10.0) curvePtsY.Add(py1);
            curvePtsY.Add(py2);
            curvePtsY.Add(py3);
            if (hookHeightY > 10.0) curvePtsY.Add(py4);

            result.RebarSets.Add(new FoundationRebarSetLayoutInfo
            {
                Role = FoundationRebarRole.BottomLayerY,
                Diameter = settings.BottomDiameterY,
                BarCount = countY,
                SpacingMm = actualSpacingY,
                RangeLengthMm = rangeX,
                CurvePoints = curvePtsY,
                DistributionVector = new Point3D(1, 0, 0),
                Description = $"Thép đáy móng phương Y ({settings.BottomDiameterY} a{actualSpacingY:F0}, {countY} thanh)"
            });

            // ==============================================================
            // 3. Thép Lưới Mặt Trên (Top Grid Rebar - đài cọc / móng dày)
            // ==============================================================
            if (settings.CreateTopGrid)
            {
                CalculateTopGrid(foundation, settings, cSide, cTop, xMin, xMax, yMin, yMax, zTop, result);
            }

            // ==============================================================
            // 4. Thép Chờ Cột & Đai Cổ Móng (Starter Dowels & Ties)
            // ==============================================================
            if (settings.CreateColumnDowels)
            {
                CalculateColumnDowels(foundation, settings, cx, cy, zBarBotY, zTop, cSide, result);
            }

            return result;
        }

        private static void CalculateTopGrid(
            FoundationModel f,
            FoundationRebarSettings s,
            double cSide,
            double cTop,
            double xMin,
            double xMax,
            double yMin,
            double yMax,
            double zTop,
            FoundationLayoutResult result)
        {
            var barTopX = BarCatalog.Parse(s.TopDiameterX) ?? new BarInfo(12);
            var barTopY = BarCatalog.Parse(s.TopDiameterY) ?? new BarInfo(12);

            double zBarTopY = zTop - cTop - barTopY.DiameterMm / 2.0;
            double zBarTopX = zBarTopY - barTopY.DiameterMm / 2.0 - barTopX.DiameterMm / 2.0;
            double hookTop = s.TopHookHeightMm;

            // Thép trên phương X
            double rangeY = (yMax - cSide) - (yMin + cSide);
            int countX = Math.Max(2, (int)Math.Ceiling(rangeY / s.TopSpacingX) + 1);
            double spacingX = rangeY / (countX - 1);

            Point3D px1 = new Point3D(xMin + cSide, yMin + cSide, zBarTopX - hookTop);
            Point3D px2 = new Point3D(xMin + cSide, yMin + cSide, zBarTopX);
            Point3D px3 = new Point3D(xMax - cSide, yMin + cSide, zBarTopX);
            Point3D px4 = new Point3D(xMax - cSide, yMin + cSide, zBarTopX - hookTop);

            result.RebarSets.Add(new FoundationRebarSetLayoutInfo
            {
                Role = FoundationRebarRole.TopLayerX,
                Diameter = s.TopDiameterX,
                BarCount = countX,
                SpacingMm = spacingX,
                RangeLengthMm = rangeY,
                CurvePoints = new List<Point3D> { px1, px2, px3, px4 },
                DistributionVector = new Point3D(0, 1, 0),
                Description = $"Thép mặt trên phương X ({s.TopDiameterX} a{spacingX:F0}, {countX} thanh)"
            });

            // Thép trên phương Y
            double rangeX = (xMax - cSide) - (xMin + cSide);
            int countY = Math.Max(2, (int)Math.Ceiling(rangeX / s.TopSpacingY) + 1);
            double spacingY = rangeX / (countY - 1);

            Point3D py1 = new Point3D(xMin + cSide, yMin + cSide, zBarTopY - hookTop);
            Point3D py2 = new Point3D(xMin + cSide, yMin + cSide, zBarTopY);
            Point3D py3 = new Point3D(xMin + cSide, yMax - cSide, zBarTopY);
            Point3D py4 = new Point3D(xMin + cSide, yMax - cSide, zBarTopY - hookTop);

            result.RebarSets.Add(new FoundationRebarSetLayoutInfo
            {
                Role = FoundationRebarRole.TopLayerY,
                Diameter = s.TopDiameterY,
                BarCount = countY,
                SpacingMm = spacingY,
                RangeLengthMm = rangeX,
                CurvePoints = new List<Point3D> { py1, py2, py3, py4 },
                DistributionVector = new Point3D(1, 0, 0),
                Description = $"Thép mặt trên phương Y ({s.TopDiameterY} a{spacingY:F0}, {countY} thanh)"
            });
        }

        private static void CalculateColumnDowels(
            FoundationModel f,
            FoundationRebarSettings s,
            double cx,
            double cy,
            double zFootLevel,
            double zTop,
            double cSide,
            FoundationLayoutResult result)
        {
            var barDowel = BarCatalog.Parse(s.DowelDiameter) ?? new BarInfo(20);
            double colB = f.ColumnWidthBMm;
            double colH = f.ColumnHeightHMm;
            double dFoot = s.DowelFootLengthMm;
            double zDowelTop = zTop + s.DowelExtendHeightMm;

            int nB = Math.Max(2, s.DowelsCountB);
            int nH = Math.Max(2, s.DowelsCountH);

            double innerB = colB - 2 * 30.0; // Giả định lớp bảo vệ cột = 30mm
            double innerH = colH - 2 * 30.0;

            double stepB = innerB / (nB - 1);
            double stepH = innerH / (nH - 1);

            var dowelPositions = new List<(double X, double Y, Point3D FootDir)>();

            // Cạnh B dưới (uốn chân vịt hướng ra ngoài: Y âm)
            for (int i = 0; i < nB; i++)
                dowelPositions.Add((cx - innerB / 2.0 + i * stepB, cy - innerH / 2.0, new Point3D(0, -1, 0)));

            // Cạnh H phải (uốn chân vịt hướng ra ngoài: X dương)
            for (int i = 1; i < nH - 1; i++)
                dowelPositions.Add((cx + innerB / 2.0, cy - innerH / 2.0 + i * stepH, new Point3D(1, 0, 0)));

            // Cạnh B trên (uốn chân vịt hướng ra ngoài: Y dương)
            for (int i = nB - 1; i >= 0; i--)
                dowelPositions.Add((cx - innerB / 2.0 + i * stepB, cy + innerH / 2.0, new Point3D(0, 1, 0)));

            // Cạnh H trái (uốn chân vịt hướng ra ngoài: X âm)
            for (int i = nH - 2; i >= 1; i--)
                dowelPositions.Add((cx - innerB / 2.0, cy - innerH / 2.0 + i * stepH, new Point3D(-1, 0, 0)));

            // Tạo từng thanh thép chờ chân vịt
            foreach (var pos in dowelPositions)
            {
                Point3D pFootEnd = new Point3D(pos.X + pos.FootDir.X * dFoot, pos.Y + pos.FootDir.Y * dFoot, zFootLevel);
                Point3D pCorner = new Point3D(pos.X, pos.Y, zFootLevel);
                Point3D pTop = new Point3D(pos.X, pos.Y, zDowelTop);

                result.RebarSets.Add(new FoundationRebarSetLayoutInfo
                {
                    Role = FoundationRebarRole.ColumnDowel,
                    Diameter = s.DowelDiameter,
                    BarCount = 1,
                    SpacingMm = 0,
                    RangeLengthMm = 0,
                    CurvePoints = new List<Point3D> { pFootEnd, pCorner, pTop },
                    DistributionVector = new Point3D(0, 0, 0),
                    Description = $"Thép chờ cột chân vịt ({s.DowelDiameter})"
                });
            }

            // Đai cổ cột định vị thép chờ
            if (s.DowelTieCount > 0)
            {
                double zTieStart = zFootLevel + 100.0;
                double halfB = innerB / 2.0 + 10.0;
                double halfH = innerH / 2.0 + 10.0;

                Point3D t1 = new Point3D(cx - halfB, cy - halfH, zTieStart);
                Point3D t2 = new Point3D(cx + halfB, cy - halfH, zTieStart);
                Point3D t3 = new Point3D(cx + halfB, cy + halfH, zTieStart);
                Point3D t4 = new Point3D(cx - halfB, cy + halfH, zTieStart);
                Point3D t5 = new Point3D(cx - halfB, cy - halfH, zTieStart);

                result.RebarSets.Add(new FoundationRebarSetLayoutInfo
                {
                    Role = FoundationRebarRole.ColumnDowelTie,
                    Diameter = s.DowelTieDiameter,
                    BarCount = s.DowelTieCount,
                    SpacingMm = s.DowelTieSpacing,
                    RangeLengthMm = s.DowelTieSpacing * (s.DowelTieCount - 1),
                    CurvePoints = new List<Point3D> { t1, t2, t3, t4, t5 },
                    DistributionVector = new Point3D(0, 0, 1),
                    Description = $"Đai cổ cột định vị thép chờ ({s.DowelTieDiameter} a{s.DowelTieSpacing:F0}, {s.DowelTieCount} đai)"
                });
            }
        }
    }
}
