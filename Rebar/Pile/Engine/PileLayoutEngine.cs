using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Pile.Models;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Pile.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán cốt thép cọc khoan nhồi và cọc vuông theo TCVN 5574:2018 (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class PileLayoutEngine
    {
        public static PileLayoutResult Calculate(PileModel pile, PileRebarSettings settings)
        {
            var result = new PileLayoutResult(pile, settings);
            if (pile == null || pile.LengthMm <= 500)
                return result;

            if (pile.Type == PileType.BoredPile)
            {
                CalculateBoredPile(pile, settings, result);
            }
            else
            {
                CalculateSquarePile(pile, settings, result);
            }

            return result;
        }

        private static void CalculateBoredPile(PileModel pile, PileRebarSettings s, PileLayoutResult result)
        {
            var barMain = BarCatalog.Parse(s.MainDiameter) ?? new BarInfo(20);
            var barStirrup = BarCatalog.Parse(s.StirrupDiameter) ?? new BarInfo(10);
            var barStiffener = BarCatalog.Parse(s.StiffenerDiameter) ?? new BarInfo(14);

            double r = pile.DiameterMm / 2.0;
            double cover = s.CoverMm;

            double rCage = r - cover - barStirrup.DiameterMm - barMain.DiameterMm / 2.0;
            if (rCage < 50.0) rCage = r / 2.0;

            double cx = pile.CenterPoint.X;
            double cy = pile.CenterPoint.Y;

            double zBot = pile.BottomElevationMm + cover;
            double zTop = pile.TopElevationMm + s.DowelExtendLengthMm;

            int n = Math.Max(4, s.MainBarCount);

            // ==========================================================
            // 1. Thép Dọc Cọc Khoan Nhồi (Main Longitudinal Bars)
            // ==========================================================
            for (int i = 0; i < n; i++)
            {
                double angle = i * 2.0 * Math.PI / n;
                double bx = cx + rCage * Math.Cos(angle);
                double by = cy + rCage * Math.Sin(angle);

                Point3D pBot = new Point3D(bx, by, zBot);
                Point3D pTop = new Point3D(bx, by, zTop);

                result.RebarSets.Add(new PileRebarSetLayoutInfo
                {
                    Role = PileRebarRole.MainLongitudinal,
                    Diameter = s.MainDiameter,
                    BarCount = 1,
                    SpacingMm = 0,
                    RangeLengthMm = 0,
                    CurvePoints = new List<Point3D> { pBot, pTop },
                    DistributionVector = new Point3D(0, 0, 0),
                    Description = $"Thép dọc cọc #{i + 1} ({s.MainDiameter})"
                });
            }

            // ==========================================================
            // 2. Cốt Đai Tròn / Đai Xoắn Phân Vùng
            // ==========================================================
            double rStirrup = r - cover - barStirrup.DiameterMm / 2.0;
            double pileTopZ = pile.TopElevationMm - cover;
            double pileBotZ = pile.BottomElevationMm + cover;
            double totalH = pileTopZ - pileBotZ;

            double denseLen = Math.Min(s.DenseZoneLengthMm, totalH / 3.0);
            double midLen = Math.Max(0.0, totalH - 2.0 * denseLen);

            // Sinh các điểm đường tròn đai cọc
            List<Point3D> CreateCirclePts(double zLevel)
            {
                var pts = new List<Point3D>();
                int segments = 16;
                for (int j = 0; j <= segments; j++)
                {
                    double ang = j * 2.0 * Math.PI / segments;
                    pts.Add(new Point3D(cx + rStirrup * Math.Cos(ang), cy + rStirrup * Math.Sin(ang), zLevel));
                }
                return pts;
            }

            // Đai dày đầu cọc
            int countDenseHead = Math.Max(2, (int)Math.Ceiling(denseLen / s.StirrupSpacingDense) + 1);
            result.RebarSets.Add(new PileRebarSetLayoutInfo
            {
                Role = PileRebarRole.StirrupDenseHead,
                Diameter = s.StirrupDiameter,
                BarCount = countDenseHead,
                SpacingMm = s.StirrupSpacingDense,
                RangeLengthMm = denseLen,
                CurvePoints = CreateCirclePts(pileTopZ),
                DistributionVector = new Point3D(0, 0, -1),
                Description = $"Đai vùng dày đầu cọc ({s.StirrupDiameter} a{s.StirrupSpacingDense:F0}, {countDenseHead} đai)"
            });

            // Đai thưa thân cọc
            if (midLen > 100.0)
            {
                int countSparse = Math.Max(2, (int)Math.Ceiling(midLen / s.StirrupSpacingSparse) + 1);
                result.RebarSets.Add(new PileRebarSetLayoutInfo
                {
                    Role = PileRebarRole.StirrupSparseBody,
                    Diameter = s.StirrupDiameter,
                    BarCount = countSparse,
                    SpacingMm = s.StirrupSpacingSparse,
                    RangeLengthMm = midLen,
                    CurvePoints = CreateCirclePts(pileTopZ - denseLen),
                    DistributionVector = new Point3D(0, 0, -1),
                    Description = $"Đai vùng thân cọc ({s.StirrupDiameter} a{s.StirrupSpacingSparse:F0}, {countSparse} đai)"
                });
            }

            // ==========================================================
            // 3. Vành Đai Gia Cường / Định Vị (Stiffener Rings)
            // ==========================================================
            if (s.CreateStiffenerRings && s.StiffenerSpacingMm > 200.0)
            {
                double rStiffener = rCage - barMain.DiameterMm / 2.0 - barStiffener.DiameterMm / 2.0;
                int ringCount = Math.Max(1, (int)(totalH / s.StiffenerSpacingMm));

                for (int k = 1; k <= ringCount; k++)
                {
                    double zRing = pileTopZ - k * s.StiffenerSpacingMm;
                    if (zRing < pileBotZ + 200.0) break;

                    var ringPts = new List<Point3D>();
                    int segs = 16;
                    for (int j = 0; j <= segs; j++)
                    {
                        double ang = j * 2.0 * Math.PI / segs;
                        ringPts.Add(new Point3D(cx + rStiffener * Math.Cos(ang), cy + rStiffener * Math.Sin(ang), zRing));
                    }

                    result.RebarSets.Add(new PileRebarSetLayoutInfo
                    {
                        Role = PileRebarRole.StiffenerRing,
                        Diameter = s.StiffenerDiameter,
                        BarCount = 1,
                        SpacingMm = 0,
                        RangeLengthMm = 0,
                        CurvePoints = ringPts,
                        DistributionVector = new Point3D(0, 0, 0),
                        Description = $"Vành đai gia cường cọc #{k} ({s.StiffenerDiameter})"
                    });
                }
            }
        }

        private static void CalculateSquarePile(PileModel pile, PileRebarSettings s, PileLayoutResult result)
        {
            var barMain = BarCatalog.Parse(s.MainDiameter) ?? new BarInfo(18);
            var barStirrup = BarCatalog.Parse(s.StirrupDiameter) ?? new BarInfo(8);

            double b = pile.WidthBMm;
            double h = pile.HeightHMm;
            double cover = s.CoverMm;

            double cx = pile.CenterPoint.X;
            double cy = pile.CenterPoint.Y;

            double halfB = b / 2.0 - cover - barStirrup.DiameterMm - barMain.DiameterMm / 2.0;
            double halfH = h / 2.0 - cover - barStirrup.DiameterMm - barMain.DiameterMm / 2.0;

            double zBot = pile.BottomElevationMm + cover;
            double zTop = pile.TopElevationMm + s.DowelExtendLengthMm;

            // 4 thanh dọc 4 góc
            var corners = new List<(double X, double Y)>
            {
                (cx - halfB, cy - halfH),
                (cx + halfB, cy - halfH),
                (cx + halfB, cy + halfH),
                (cx - halfB, cy + halfH)
            };

            for (int i = 0; i < corners.Count; i++)
            {
                result.RebarSets.Add(new PileRebarSetLayoutInfo
                {
                    Role = PileRebarRole.MainLongitudinal,
                    Diameter = s.MainDiameter,
                    BarCount = 1,
                    CurvePoints = new List<Point3D>
                    {
                        new Point3D(corners[i].X, corners[i].Y, zBot),
                        new Point3D(corners[i].X, corners[i].Y, zTop)
                    },
                    Description = $"Thép dọc cọc vuông góc #{i + 1} ({s.MainDiameter})"
                });
            }

            // Đai vuông khép kín
            double pileTopZ = pile.TopElevationMm - cover;
            double pileBotZ = pile.BottomElevationMm + cover;
            double totalH = pileTopZ - pileBotZ;
            double denseLen = Math.Min(s.DenseZoneLengthMm, totalH / 3.0);

            double sb = b / 2.0 - cover;
            double sh = h / 2.0 - cover;

            var stirrupCorners = new List<Point3D>
            {
                new Point3D(cx - sb, cy - sh, pileTopZ),
                new Point3D(cx + sb, cy - sh, pileTopZ),
                new Point3D(cx + sb, cy + sh, pileTopZ),
                new Point3D(cx - sb, cy + sh, pileTopZ),
                new Point3D(cx - sb, cy - sh, pileTopZ)
            };

            int countDense = Math.Max(2, (int)Math.Ceiling(denseLen / s.StirrupSpacingDense) + 1);
            result.RebarSets.Add(new PileRebarSetLayoutInfo
            {
                Role = PileRebarRole.StirrupDenseHead,
                Diameter = s.StirrupDiameter,
                BarCount = countDense,
                SpacingMm = s.StirrupSpacingDense,
                RangeLengthMm = denseLen,
                CurvePoints = stirrupCorners,
                DistributionVector = new Point3D(0, 0, -1),
                Description = $"Đai vùng dày đầu cọc vuông ({s.StirrupDiameter} a{s.StirrupSpacingDense:F0}, {countDense} đai)"
            });
        }
    }
}
