using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Common;

namespace JNNTool.RebarSuite.Slab.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán và sinh tọa độ rải thép sàn (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class SlabLayoutEngine
    {
        public static SlabLayoutResult Calculate(SlabModel slab, SlabRebarSettings settings)
        {
            var result = new SlabLayoutResult(slab, settings);

            var bounds = slab.GetBounds();
            double minX = bounds.MinX;
            double minY = bounds.MinY;
            double maxX = bounds.MaxX;
            double maxY = bounds.MaxY;

            double spanX = maxX - minX;
            double spanY = maxY - minY;

            if (spanX <= 0 || spanY <= 0)
                return result;

            double topElev = slab.TopElevationMm;
            double botElev = topElev - slab.ThicknessMm;
            double coverBot = settings.CoverBottom;
            double coverTop = settings.CoverTop;
            double coverSide = settings.CoverSide;

            var barBottomX = BarCatalog.Parse(settings.BottomXDiameter) ?? new BarInfo(10);
            var barBottomY = BarCatalog.Parse(settings.BottomYDiameter) ?? new BarInfo(10);

            // ==============================================================
            // 1. Thép Lớp Dưới Phương X (Bottom X)
            // ==============================================================
            double startX = minX + coverSide;
            double endX = maxX - coverSide;
            double zBottomX = botElev + coverBot + barBottomX.DiameterMm / 2.0;

            if (endX > startX)
            {
                GeometryHelper.CalculateSpacing(
                    rangeLengthMm: spanY,
                    coverStartMm: coverSide,
                    coverEndMm: coverSide,
                    targetSpacingMm: settings.BottomXSpacing,
                    out int countX,
                    out double actualSpacingY);

                var startPt = new Point3D(startX, minY + coverSide, zBottomX);
                var endPt = new Point3D(endX, minY + coverSide, zBottomX);

                var rebarSet = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.BottomX,
                    Diameter = barBottomX.Name,
                    SpacingMm = actualSpacingY,
                    BarCount = countX,
                    DistributionVector = new Point3D(0, 1, 0),
                    DistributionLengthMm = spanY - 2 * coverSide,
                    StartHook = settings.BottomXHook,
                    EndHook = settings.BottomXHook,
                    HookLengthMm = settings.BottomXHookLength,
                    Description = "Thép sàn lớp dưới phương X"
                };

                rebarSet.CurvePoints.Add(startPt);
                rebarSet.CurvePoints.Add(endPt);
                result.RebarSets.Add(rebarSet);
            }

            // ==============================================================
            // 2. Thép Lớp Dưới Phương Y (Bottom Y) - nằm trên lớp X
            // ==============================================================
            double startY = minY + coverSide;
            double endY = maxY - coverSide;
            double zBottomY = zBottomX + barBottomX.DiameterMm / 2.0 + barBottomY.DiameterMm / 2.0;

            if (endY > startY)
            {
                GeometryHelper.CalculateSpacing(
                    rangeLengthMm: spanX,
                    coverStartMm: coverSide,
                    coverEndMm: coverSide,
                    targetSpacingMm: settings.BottomYSpacing,
                    out int countY,
                    out double actualSpacingX);

                var startPt = new Point3D(minX + coverSide, startY, zBottomY);
                var endPt = new Point3D(minX + coverSide, endY, zBottomY);

                var rebarSet = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.BottomY,
                    Diameter = barBottomY.Name,
                    SpacingMm = actualSpacingX,
                    BarCount = countY,
                    DistributionVector = new Point3D(1, 0, 0),
                    DistributionLengthMm = spanX - 2 * coverSide,
                    StartHook = settings.BottomYHook,
                    EndHook = settings.BottomYHook,
                    HookLengthMm = settings.BottomYHookLength,
                    Description = "Thép sàn lớp dưới phương Y"
                };

                rebarSet.CurvePoints.Add(startPt);
                rebarSet.CurvePoints.Add(endPt);
                result.RebarSets.Add(rebarSet);
            }

            // ==============================================================
            // 3. Thép Lớp Trên Toàn Bộ (Top Full Layer - nếu bật)
            // ==============================================================
            if (settings.IsCreateTopFullLayer)
            {
                var barTopX = BarCatalog.Parse(settings.TopXDiameter) ?? new BarInfo(10);
                var barTopY = BarCatalog.Parse(settings.TopYDiameter) ?? new BarInfo(10);

                double zTopX = topElev - coverTop - barTopX.DiameterMm / 2.0;
                double zTopY = zTopX - barTopX.DiameterMm / 2.0 - barTopY.DiameterMm / 2.0;

                // Top X
                GeometryHelper.CalculateSpacing(spanY, coverSide, coverSide, settings.TopXSpacing, out int countTopX, out double actualTopSpacingY);
                var topSetX = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.TopX,
                    Diameter = barTopX.Name,
                    SpacingMm = actualTopSpacingY,
                    BarCount = countTopX,
                    DistributionVector = new Point3D(0, 1, 0),
                    DistributionLengthMm = spanY - 2 * coverSide,
                    StartHook = settings.TopXHook,
                    EndHook = settings.TopXHook,
                    HookLengthMm = settings.TopXHookLength,
                    Description = "Thép sàn lớp trên phương X"
                };
                topSetX.CurvePoints.Add(new Point3D(startX, minY + coverSide, zTopX));
                topSetX.CurvePoints.Add(new Point3D(endX, minY + coverSide, zTopX));
                result.RebarSets.Add(topSetX);

                // Top Y
                GeometryHelper.CalculateSpacing(spanX, coverSide, coverSide, settings.TopYSpacing, out int countTopY, out double actualTopSpacingX);
                var topSetY = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.TopY,
                    Diameter = barTopY.Name,
                    SpacingMm = actualTopSpacingX,
                    BarCount = countTopY,
                    DistributionVector = new Point3D(1, 0, 0),
                    DistributionLengthMm = spanX - 2 * coverSide,
                    StartHook = settings.TopYHook,
                    EndHook = settings.TopYHook,
                    HookLengthMm = settings.TopYHookLength,
                    Description = "Thép sàn lớp trên phương Y"
                };
                topSetY.CurvePoints.Add(new Point3D(minX + coverSide, startY, zTopY));
                topSetY.CurvePoints.Add(new Point3D(minX + coverSide, endY, zTopY));
                result.RebarSets.Add(topSetY);
            }

            // ==============================================================
            // 4. Thép Mũ Tăng Cường Gối (Top Add Rebar - L/4)
            // ==============================================================
            if (settings.IsCreateTopAdd)
            {
                CalculateTopAddRebars(slab, settings, bounds, result);
            }

            return result;
        }

        /// <summary>
        /// Tính toán thép mũ gối dầm (Top Add) theo nhịp L/4, làm tròn bội số 50mm.
        /// </summary>
        private static void CalculateTopAddRebars(
            SlabModel slab,
            SlabRebarSettings settings,
            (double MinX, double MinY, double MaxX, double MaxY) bounds,
            SlabLayoutResult result)
        {
            var barTopAdd = BarCatalog.Parse(settings.TopAddDiameter) ?? new BarInfo(10);
            var barDist = BarCatalog.Parse(settings.TopAddDistributionDiameter) ?? new BarInfo(6);

            double topElev = slab.TopElevationMm;
            double zMux = topElev - settings.CoverTop - barTopAdd.DiameterMm / 2.0;
            double hookDown = Math.Max(slab.ThicknessMm - settings.CoverTop - settings.CoverBottom - 20.0, 50.0);

            // 1. Thép mũ gối biên trái (X Min) nếu nhịp X >= MinSpan
            double spanX = bounds.MaxX - bounds.MinX;
            if (spanX >= settings.MinSpanToCreateTopAdd)
            {
                // Chiều dài vươn mũ = L/4 làm tròn bội số 50
                double cantileverL = GeometryHelper.RoundUpToMultiple(spanX * settings.TopAddSpanRatio, 50);

                double xStart = bounds.MinX + settings.CoverSide;
                double xEnd = xStart + cantileverL;

                GeometryHelper.CalculateSpacing(
                    bounds.MaxY - bounds.MinY,
                    settings.CoverSide,
                    settings.CoverSide,
                    settings.TopAddSpacing,
                    out int countMuLeft,
                    out double spacingMuY);

                var muLeftSet = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.TopAdd,
                    Diameter = barTopAdd.Name,
                    SpacingMm = spacingMuY,
                    BarCount = countMuLeft,
                    DistributionVector = new Point3D(0, 1, 0),
                    DistributionLengthMm = (bounds.MaxY - bounds.MinY) - 2 * settings.CoverSide,
                    Description = "Thép mũ gối sàn (Biên trái)"
                };

                // Điểm bẻ mỏ xuống mép ngoài
                muLeftSet.CurvePoints.Add(new Point3D(xStart, bounds.MinY + settings.CoverSide, zMux - hookDown));
                muLeftSet.CurvePoints.Add(new Point3D(xStart, bounds.MinY + settings.CoverSide, zMux));
                muLeftSet.CurvePoints.Add(new Point3D(xEnd, bounds.MinY + settings.CoverSide, zMux));
                // Mỏ móc mép trong gối
                muLeftSet.CurvePoints.Add(new Point3D(xEnd, bounds.MinY + settings.CoverSide, zMux - settings.TopAddHookDownB));
                result.RebarSets.Add(muLeftSet);

                // 2. Thép mũ gối biên phải (X Max)
                double xRightEnd = bounds.MaxX - settings.CoverSide;
                double xRightStart = xRightEnd - cantileverL;

                var muRightSet = new RebarSetLayoutInfo
                {
                    LayerType = RebarLayerType.TopAdd,
                    Diameter = barTopAdd.Name,
                    SpacingMm = spacingMuY,
                    BarCount = countMuLeft,
                    DistributionVector = new Point3D(0, 1, 0),
                    DistributionLengthMm = (bounds.MaxY - bounds.MinY) - 2 * settings.CoverSide,
                    Description = "Thép mũ gối sàn (Biên phải)"
                };

                muRightSet.CurvePoints.Add(new Point3D(xRightStart, bounds.MinY + settings.CoverSide, zMux - settings.TopAddHookDownB));
                muRightSet.CurvePoints.Add(new Point3D(xRightStart, bounds.MinY + settings.CoverSide, zMux));
                muRightSet.CurvePoints.Add(new Point3D(xRightEnd, bounds.MinY + settings.CoverSide, zMux));
                muRightSet.CurvePoints.Add(new Point3D(xRightEnd, bounds.MinY + settings.CoverSide, zMux - hookDown));
                result.RebarSets.Add(muRightSet);

                // 3. Thép phân bố cho mũ (D6 a300)
                if (settings.IsCreateTopAddDistribution && cantileverL > 100.0)
                {
                    GeometryHelper.CalculateSpacing(cantileverL, 50, 50, settings.TopAddDistributionSpacing, out int countDist, out double spacingDist);

                    var distLeft = new RebarSetLayoutInfo
                    {
                        LayerType = RebarLayerType.TopAddDistribution,
                        Diameter = barDist.Name,
                        SpacingMm = spacingDist,
                        BarCount = countDist,
                        DistributionVector = new Point3D(1, 0, 0),
                        DistributionLengthMm = cantileverL - 100,
                        Description = "Thép phân bố mũ gối (Biên trái)"
                    };
                    distLeft.CurvePoints.Add(new Point3D(xStart + 50, bounds.MinY + settings.CoverSide, zMux - barTopAdd.DiameterMm));
                    distLeft.CurvePoints.Add(new Point3D(xStart + 50, bounds.MaxY - settings.CoverSide, zMux - barTopAdd.DiameterMm));
                    result.RebarSets.Add(distLeft);
                }
            }
        }
    }
}
