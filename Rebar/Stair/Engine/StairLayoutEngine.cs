using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Stair.Models;

namespace JNNTool.RebarSuite.Stair.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán cốt thép bản thang theo TCVN 5574:2018 (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class StairLayoutEngine
    {
        public static StairLayoutResult Calculate(StairModel stair, StairRebarSettings settings)
        {
            var result = new StairLayoutResult(stair, settings);
            if (stair == null || stair.Flights == null || stair.Flights.Count == 0)
                return result;

            var barBot = BarCatalog.Parse(settings.BottomDiameter) ?? new BarInfo(10);
            var barTop = BarCatalog.Parse(settings.TopDiameter) ?? new BarInfo(10);
            var barDist = BarCatalog.Parse(settings.DistributionDiameter) ?? new BarInfo(6);

            double cover = settings.CoverMm;

            foreach (var flight in stair.Flights)
            {
                CalculateFlightRebar(flight, settings, barBot, barTop, barDist, cover, result);
            }

            return result;
        }

        private static void CalculateFlightRebar(
            StairFlightModel flight,
            StairRebarSettings s,
            BarInfo barBot,
            BarInfo barTop,
            BarInfo barDist,
            double cover,
            StairLayoutResult result)
        {
            double w = flight.WidthMm;
            double hs = flight.SlabThicknessMm;
            double dMain = barBot.DiameterMm;
            double dDist = barDist.DiameterMm;

            double clearW = w - 2.0 * cover;
            if (clearW < 50.0) clearW = 50.0;

            Point3D pStart = flight.StartPoint;
            Point3D pEnd = flight.EndPoint;
            Point3D dirHoriz = new Point3D(pEnd.X - pStart.X, pEnd.Y - pStart.Y, 0);
            double horizLen = Math.Sqrt(dirHoriz.X * dirHoriz.X + dirHoriz.Y * dirHoriz.Y);
            if (horizLen > 0.001)
            {
                dirHoriz = new Point3D(dirHoriz.X / horizLen, dirHoriz.Y / horizLen, 0);
            }
            else
            {
                dirHoriz = new Point3D(1, 0, 0);
            }

            Point3D norm = flight.Normal;
            double totalRise = pEnd.Z - pStart.Z;

            // ==========================================================
            // 1. Thép Bản Thang Lớp Dưới (Bottom Main Longitudinal Bars)
            // ==========================================================
            int countBot = Math.Max(2, (int)Math.Ceiling(clearW / s.BottomSpacing) + 1);
            double spacingBot = clearW / (countBot - 1);

            double anchor = s.BottomAnchorLengthMm;
            double zBot = pStart.Z + cover + dMain / 2.0;
            double zTop = pEnd.Z + cover + dMain / 2.0;

            // Đường cong thanh thép dưới:
            // 1. Điểm neo nằm ngang chân thang: (pStart - dirHoriz * anchor, zBot)
            // 2. Điểm chân dốc thang: (pStart, zBot)
            // 3. Điểm đỉnh dốc thang: (pEnd, zTop)
            // 4. Điểm neo nằm ngang chiếu nghỉ: (pEnd + dirHoriz * anchor, zTop)
            Point3D pb1 = (pStart - dirHoriz * anchor) + norm * (cover + dMain / 2.0);
            pb1 = new Point3D(pb1.X, pb1.Y, zBot);

            Point3D pb2 = pStart + norm * (cover + dMain / 2.0);
            pb2 = new Point3D(pb2.X, pb2.Y, zBot);

            Point3D pb3 = pEnd + norm * (cover + dMain / 2.0);
            pb3 = new Point3D(pb3.X, pb3.Y, zTop);

            Point3D pb4 = (pEnd + dirHoriz * anchor) + norm * (cover + dMain / 2.0);
            pb4 = new Point3D(pb4.X, pb4.Y, zTop);

            result.RebarSets.Add(new StairRebarSetLayoutInfo
            {
                Role = StairRebarRole.BottomMainFlight,
                Diameter = s.BottomDiameter,
                BarCount = countBot,
                SpacingMm = spacingBot,
                RangeLengthMm = clearW,
                CurvePoints = new List<Point3D> { pb1, pb2, pb3, pb4 },
                DistributionVector = norm,
                Description = $"Thép lớp dưới vế thang #{flight.FlightIndex} ({s.BottomDiameter} a{spacingBot:F0}, {countBot} thanh)"
            });

            // ==========================================================
            // 2. Thép Bản Thang Lớp Trên / Mũ Gối (Top Support Bars)
            // ==========================================================
            if (s.CreateTopBars)
            {
                int countTop = Math.Max(2, (int)Math.Ceiling(clearW / s.TopSpacing) + 1);
                double spacingTop = clearW / (countTop - 1);

                double topSpanLen = flight.InclinedLengthMm * s.TopSpanFactor;
                double zTopSlabBot = zBot + hs - 2.0 * cover - dMain;
                double zTopSlabTop = zTop + hs - 2.0 * cover - dMain;

                // Mũ gối trên (tiếp giáp chiếu nghỉ)
                Point3D pt1 = pEnd + norm * (cover + dMain / 2.0) - dirHoriz * (topSpanLen * (horizLen / flight.InclinedLengthMm));
                pt1 = new Point3D(pt1.X, pt1.Y, zTopSlabTop - (topSpanLen * (totalRise / flight.InclinedLengthMm)));

                Point3D pt2 = pEnd + norm * (cover + dMain / 2.0);
                pt2 = new Point3D(pt2.X, pt2.Y, zTopSlabTop);

                Point3D pt3 = (pEnd + dirHoriz * anchor) + norm * (cover + dMain / 2.0);
                pt3 = new Point3D(pt3.X, pt3.Y, zTopSlabTop);

                result.RebarSets.Add(new StairRebarSetLayoutInfo
                {
                    Role = StairRebarRole.TopSupportTop,
                    Diameter = s.TopDiameter,
                    BarCount = countTop,
                    SpacingMm = spacingTop,
                    RangeLengthMm = clearW,
                    CurvePoints = new List<Point3D> { pt1, pt2, pt3 },
                    DistributionVector = norm,
                    Description = $"Thép mũ gối trên vế thang #{flight.FlightIndex} ({s.TopDiameter} a{spacingTop:F0}, {countTop} thanh)"
                });
            }

            // ==========================================================
            // 3. Thép Phân Bố Ngang (Distribution Bars)
            // ==========================================================
            double incLen = flight.InclinedLengthMm;
            int countDist = Math.Max(2, (int)Math.Ceiling(incLen / s.DistributionSpacing) + 1);
            double spacingDist = incLen / (countDist - 1);

            // 1 thanh phân bố ngang mẫu ở chân vế
            Point3D pd1 = pStart + norm * cover;
            pd1 = new Point3D(pd1.X, pd1.Y, zBot + dMain + dDist / 2.0);

            Point3D pd2 = pStart + norm * (w - cover);
            pd2 = new Point3D(pd2.X, pd2.Y, zBot + dMain + dDist / 2.0);

            Point3D dirInclined = (pb3 - pb2);
            double lenInc = Math.Sqrt(dirInclined.X * dirInclined.X + dirInclined.Y * dirInclined.Y + dirInclined.Z * dirInclined.Z);
            if (lenInc > 0.001)
            {
                dirInclined = new Point3D(dirInclined.X / lenInc, dirInclined.Y / lenInc, dirInclined.Z / lenInc);
            }

            result.RebarSets.Add(new StairRebarSetLayoutInfo
            {
                Role = StairRebarRole.DistributionFlight,
                Diameter = s.DistributionDiameter,
                BarCount = countDist,
                SpacingMm = spacingDist,
                RangeLengthMm = incLen,
                CurvePoints = new List<Point3D> { pd1, pd2 },
                DistributionVector = dirInclined,
                Description = $"Thép phân bố vế thang #{flight.FlightIndex} ({s.DistributionDiameter} a{spacingDist:F0}, {countDist} thanh)"
            });
        }
    }
}
