using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Wall.Models;

namespace JNNTool.RebarSuite.Wall.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán bố trí cốt thép 2 lớp và đai C biên cho vách bê tông cốt thép (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class WallLayoutEngine
    {
        public static WallLayoutResult Calculate(WallModel wall, WallRebarSettings settings)
        {
            var result = new WallLayoutResult(wall, settings);
            if (wall == null || wall.LengthMm <= 100 || wall.HeightMm <= 100)
                return result;

            var barVert = BarCatalog.Parse(settings.VerticalDiameter) ?? new BarInfo(12);
            var barHoriz = BarCatalog.Parse(settings.HorizontalDiameter) ?? new BarInfo(10);
            var barTie = BarCatalog.Parse(settings.BoundaryTieDiameter) ?? new BarInfo(8);

            double cover = settings.EffectiveCoverMm;
            double t = wall.ThicknessMm;
            double l = wall.LengthMm;
            double h = wall.HeightMm;

            // Offset từ mặt phẳng tim vách đến tim cốt thép
            // Thép ngang nằm sát lớp bảo vệ ngoài, thép đứng nằm bên trong thép ngang
            double offsetHorizMm = t / 2.0 - cover - barHoriz.DiameterMm / 2.0;
            double offsetVertMm = t / 2.0 - cover - barHoriz.DiameterMm - barVert.DiameterMm / 2.0;

            if (offsetHorizMm <= 0 || offsetVertMm <= 0)
            {
                // Vách quá mỏng so với lớp bảo vệ, tự điều chỉnh
                offsetHorizMm = Math.Max(10.0, t / 4.0);
                offsetVertMm = Math.Max(5.0, t / 4.0 - 5.0);
            }

            // ==============================================================
            // 1. Thép Đứng 2 Lớp (Vertical Reinforcement - 2 Layers)
            // ==============================================================
            CalculateVerticalRebarSets(wall, settings, barVert, barHoriz, cover, offsetVertMm, result);

            // ==============================================================
            // 2. Thép Ngang 2 Lớp (Horizontal Reinforcement - 2 Layers)
            // ==============================================================
            CalculateHorizontalRebarSets(wall, settings, barHoriz, cover, offsetHorizMm, result);

            // ==============================================================
            // 3. Đai / Móc C Biên Vách (Boundary C-Ties)
            // ==============================================================
            if (settings.CreateBoundaryTies)
            {
                CalculateBoundaryCTies(wall, settings, barTie, cover, offsetHorizMm, result);
            }

            return result;
        }

        private static void CalculateVerticalRebarSets(
            WallModel wall,
            WallRebarSettings settings,
            BarInfo barVert,
            BarInfo barHoriz,
            double cover,
            double offsetVertMm,
            WallLayoutResult result)
        {
            double l = wall.LengthMm;
            double startOffset = cover + barHoriz.DiameterMm + barVert.DiameterMm / 2.0;
            double endOffset = l - startOffset;
            double clearDist = endOffset - startOffset;

            if (clearDist < 50.0)
                clearDist = 50.0;

            int barCount = Math.Max(2, (int)Math.Ceiling(clearDist / settings.VerticalSpacing) + 1);
            double actualSpacing = clearDist / (barCount - 1);

            double zBot = wall.BottomElevationMm + cover;
            double lapLength = settings.LapMultiplier * barVert.DiameterMm;
            double zTop = wall.IsTopStory
                ? wall.TopElevationMm - cover
                : wall.TopElevationMm + lapLength;

            Point3D dir = wall.Direction;
            Point3D norm = wall.Normal;

            // --- Lớp ngoài (Outer Layer: +offsetVertMm) ---
            Point3D pStartOuter1 = wall.StartPoint + (dir * startOffset) + (norm * offsetVertMm);
            pStartOuter1 = new Point3D(pStartOuter1.X, pStartOuter1.Y, zBot);
            Point3D pStartOuter2 = new Point3D(pStartOuter1.X, pStartOuter1.Y, zTop);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.VerticalLayerOuter,
                Diameter = settings.VerticalDiameter,
                BarCount = barCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearDist,
                CurvePoints = new List<Point3D> { pStartOuter1, pStartOuter2 },
                DistributionVector = dir,
                Description = $"Thép đứng vách lớp ngoài ({settings.VerticalDiameter} a{actualSpacing:F0}, {barCount} thanh)"
            });

            // --- Lớp trong (Inner Layer: -offsetVertMm) ---
            Point3D pStartInner1 = wall.StartPoint + (dir * startOffset) - (norm * offsetVertMm);
            pStartInner1 = new Point3D(pStartInner1.X, pStartInner1.Y, zBot);
            Point3D pStartInner2 = new Point3D(pStartInner1.X, pStartInner1.Y, zTop);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.VerticalLayerInner,
                Diameter = settings.VerticalDiameter,
                BarCount = barCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearDist,
                CurvePoints = new List<Point3D> { pStartInner1, pStartInner2 },
                DistributionVector = dir,
                Description = $"Thép đứng vách lớp trong ({settings.VerticalDiameter} a{actualSpacing:F0}, {barCount} thanh)"
            });
        }

        private static void CalculateHorizontalRebarSets(
            WallModel wall,
            WallRebarSettings settings,
            BarInfo barHoriz,
            double cover,
            double offsetHorizMm,
            WallLayoutResult result)
        {
            double h = wall.HeightMm;
            double zStart = wall.BottomElevationMm + cover + barHoriz.DiameterMm / 2.0;
            double zEnd = wall.TopElevationMm - cover - barHoriz.DiameterMm / 2.0;
            double clearHeight = zEnd - zStart;

            if (clearHeight < 50.0)
                clearHeight = 50.0;

            int barCount = Math.Max(2, (int)Math.Ceiling(clearHeight / settings.HorizontalSpacing) + 1);
            double actualSpacing = clearHeight / (barCount - 1);

            Point3D dir = wall.Direction;
            Point3D norm = wall.Normal;
            double l = wall.LengthMm;
            double anchorLen = Math.Min(settings.HorizontalAnchorLength, wall.ThicknessMm - 2.0 * cover);
            if (anchorLen < 50.0) anchorLen = 100.0;

            // --- Lớp ngoài (+offsetHorizMm), có móc bẻ 2 đầu quay vào trong ---
            Point3D p1Outer = wall.StartPoint + (dir * (cover + anchorLen)) + (norm * (offsetHorizMm - anchorLen));
            p1Outer = new Point3D(p1Outer.X, p1Outer.Y, zStart);

            Point3D p2Outer = wall.StartPoint + (dir * cover) + (norm * offsetHorizMm);
            p2Outer = new Point3D(p2Outer.X, p2Outer.Y, zStart);

            Point3D p3Outer = wall.StartPoint + (dir * (l - cover)) + (norm * offsetHorizMm);
            p3Outer = new Point3D(p3Outer.X, p3Outer.Y, zStart);

            Point3D p4Outer = wall.StartPoint + (dir * (l - cover - anchorLen)) + (norm * (offsetHorizMm - anchorLen));
            p4Outer = new Point3D(p4Outer.X, p4Outer.Y, zStart);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.HorizontalLayerOuter,
                Diameter = settings.HorizontalDiameter,
                BarCount = barCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearHeight,
                CurvePoints = new List<Point3D> { p1Outer, p2Outer, p3Outer, p4Outer },
                DistributionVector = new Point3D(0, 0, 1),
                Description = $"Thép ngang vách lớp ngoài ({settings.HorizontalDiameter} a{actualSpacing:F0}, {barCount} thanh)"
            });

            // --- Lớp trong (-offsetHorizMm), có móc bẻ 2 đầu quay vào trong ---
            Point3D p1Inner = wall.StartPoint + (dir * (cover + anchorLen)) - (norm * (offsetHorizMm - anchorLen));
            p1Inner = new Point3D(p1Inner.X, p1Inner.Y, zStart);

            Point3D p2Inner = wall.StartPoint + (dir * cover) - (norm * offsetHorizMm);
            p2Inner = new Point3D(p2Inner.X, p2Inner.Y, zStart);

            Point3D p3Inner = wall.StartPoint + (dir * (l - cover)) - (norm * offsetHorizMm);
            p3Inner = new Point3D(p3Inner.X, p3Inner.Y, zStart);

            Point3D p4Inner = wall.StartPoint + (dir * (l - cover - anchorLen)) - (norm * (offsetHorizMm - anchorLen));
            p4Inner = new Point3D(p4Inner.X, p4Inner.Y, zStart);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.HorizontalLayerInner,
                Diameter = settings.HorizontalDiameter,
                BarCount = barCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearHeight,
                CurvePoints = new List<Point3D> { p1Inner, p2Inner, p3Inner, p4Inner },
                DistributionVector = new Point3D(0, 0, 1),
                Description = $"Thép ngang vách lớp trong ({settings.HorizontalDiameter} a{actualSpacing:F0}, {barCount} thanh)"
            });
        }

        private static void CalculateBoundaryCTies(
            WallModel wall,
            WallRebarSettings settings,
            BarInfo barTie,
            double cover,
            double offsetHorizMm,
            WallLayoutResult result)
        {
            double zStart = wall.BottomElevationMm + cover;
            double zEnd = wall.TopElevationMm - cover;
            double clearHeight = zEnd - zStart;

            if (clearHeight < 50.0)
                clearHeight = 50.0;

            int tieCount = Math.Max(2, (int)Math.Ceiling(clearHeight / settings.BoundaryTieSpacing) + 1);
            double actualSpacing = clearHeight / (tieCount - 1);

            Point3D dir = wall.Direction;
            Point3D norm = wall.Normal;
            double l = wall.LengthMm;
            double hookLen = 150.0;

            // 1. Móc C biên đầu vách (Start Edge)
            Point3D startCenter = wall.StartPoint + (dir * (cover + 20.0));
            Point3D sc1 = startCenter + (dir * hookLen) + (norm * offsetHorizMm);
            Point3D sc2 = startCenter + (norm * offsetHorizMm);
            Point3D sc3 = startCenter - (norm * offsetHorizMm);
            Point3D sc4 = startCenter + (dir * hookLen) - (norm * offsetHorizMm);

            sc1 = new Point3D(sc1.X, sc1.Y, zStart);
            sc2 = new Point3D(sc2.X, sc2.Y, zStart);
            sc3 = new Point3D(sc3.X, sc3.Y, zStart);
            sc4 = new Point3D(sc4.X, sc4.Y, zStart);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.BoundaryTieStart,
                Diameter = settings.BoundaryTieDiameter,
                BarCount = tieCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearHeight,
                CurvePoints = new List<Point3D> { sc1, sc2, sc3, sc4 },
                DistributionVector = new Point3D(0, 0, 1),
                Description = $"Đai C biên đầu vách ({settings.BoundaryTieDiameter} a{actualSpacing:F0}, {tieCount} đai)"
            });

            // 2. Móc C biên cuối vách (End Edge)
            Point3D endCenter = wall.StartPoint + (dir * (l - cover - 20.0));
            Point3D ec1 = endCenter - (dir * hookLen) + (norm * offsetHorizMm);
            Point3D ec2 = endCenter + (norm * offsetHorizMm);
            Point3D ec3 = endCenter - (norm * offsetHorizMm);
            Point3D ec4 = endCenter - (dir * hookLen) - (norm * offsetHorizMm);

            ec1 = new Point3D(ec1.X, ec1.Y, zStart);
            ec2 = new Point3D(ec2.X, ec2.Y, zStart);
            ec3 = new Point3D(ec3.X, ec3.Y, zStart);
            ec4 = new Point3D(ec4.X, ec4.Y, zStart);

            result.RebarSets.Add(new WallRebarSetLayoutInfo
            {
                Role = WallRebarRole.BoundaryTieEnd,
                Diameter = settings.BoundaryTieDiameter,
                BarCount = tieCount,
                SpacingMm = actualSpacing,
                RangeLengthMm = clearHeight,
                CurvePoints = new List<Point3D> { ec1, ec2, ec3, ec4 },
                DistributionVector = new Point3D(0, 0, 1),
                Description = $"Đai C biên cuối vách ({settings.BoundaryTieDiameter} a{actualSpacing:F0}, {tieCount} đai)"
            });
        }
    }
}
