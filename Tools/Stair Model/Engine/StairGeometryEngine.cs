#nullable enable
using System;
using System.Collections.Generic;

namespace JNNTool.Tools.StairModel.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán toàn bộ hình học 2D/3D của thang kết cấu bê tông.
    /// Không chứa bất kỳ tham chiếu Revit API nào → Hoàn toàn độc lập và Unit-test được.
    /// </summary>
    public static class StairGeometryEngine
    {
        /// <summary>
        /// Điểm vào chính: nhận StairConfig và xuất ra StairGeometryResult.
        /// </summary>
        public static StairGeometryResult Generate(StairConfig config)
        {
            var (isValid, error, warning) = StairValidator.Validate(config);
            if (!isValid)
            {
                return new StairGeometryResult
                {
                    Success = false,
                    ErrorMessage = error,
                    WarningMessage = warning
                };
            }

            var result = new StairGeometryResult
            {
                Success = true,
                WarningMessage = warning,
                TotalRiseMm = config.TotalRiseMm,
                RiserHeightMm = config.TotalRiseMm / config.TotalRisers,
                BlondelNumberMm = (2.0 * (config.TotalRiseMm / config.TotalRisers)) + config.TreadDepthMm
            };

            switch (config.StairType)
            {
                case StairType.Straight:
                    BuildStraightStair(config, result);
                    break;

                case StairType.UShaped:
                    BuildUShapedStair(config, result);
                    break;

                case StairType.LShaped:
                    BuildLShapedStair(config, result);
                    break;

                default:
                    BuildUShapedStair(config, result);
                    break;
            }

            // Áp dụng xoay (Rotation) và tịnh tiến (Translation) theo điểm chèn InsertionPoint
            ApplyTransform(result, config.InsertionPointX, config.InsertionPointY, config.RotationAngleDeg);

            return result;
        }

        #region 1. Straight Stair (Thang thẳng 1 vế hoặc 2 vế thẳng)

        private static void BuildStraightStair(StairConfig config, StairGeometryResult result)
        {
            double w = config.StairWidthMm;
            double t = config.TreadDepthMm;
            double hTotal = config.TotalRiseMm;
            int n = config.TotalRisers;
            double r = hTotal / n;

            // Nếu người dùng cấu hình chia 2 vế thẳng có chiếu nghỉ giữa
            bool hasIntermediateLanding = config.LandingLengthMm > 200.0 &&
                                         config.Flight1Risers > 0 &&
                                         config.Flight2Risers > 0 &&
                                         (config.Flight1Risers + config.Flight2Risers == n);

            if (!hasIntermediateLanding)
            {
                // Thang thẳng 1 vế chạy suốt
                double runLen = Math.Max((n - 1) * t, t);
                double slope = hTotal / runLen;
                result.InclineAngleDeg = Math.Atan(slope) * (180.0 / Math.PI);

                var flight = new FlightGeometry
                {
                    FlightIndex = 1,
                    Name = "Vế thang thẳng",
                    ThicknessMm = config.WaistSlabThicknessMm,
                    RiserCount = n,
                    RiserHeightMm = r,
                    TreadDepthMm = t,
                    FlightWidthMm = w,
                    RunLengthMm = runLen,
                    BaseOffsetMm = config.BaseOffsetMm,
                    Slope = slope,
                    SlopeArrowStart = new StairPoint3D(0, w / 2.0, 0),
                    SlopeArrowEnd = new StairPoint3D(runLen, w / 2.0, 0),
                    ProfilePoints = new List<StairPoint3D>
                    {
                        new StairPoint3D(0, 0, 0),
                        new StairPoint3D(runLen, 0, 0),
                        new StairPoint3D(runLen, w, 0),
                        new StairPoint3D(0, w, 0)
                    }
                };
                result.Flights.Add(flight);

                // Dầm đỡ
                if (config.SupportBeamOption == SupportBeamOption.FullBeams)
                {
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm chân thang",
                        StartPoint = new StairPoint3D(0, 0, config.BaseOffsetMm),
                        EndPoint = new StairPoint3D(0, w, config.BaseOffsetMm),
                        ElevationMm = config.BaseOffsetMm
                    });
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm đỉnh thang",
                        StartPoint = new StairPoint3D(runLen, 0, config.BaseOffsetMm + hTotal),
                        EndPoint = new StairPoint3D(runLen, w, config.BaseOffsetMm + hTotal),
                        ElevationMm = config.BaseOffsetMm + hTotal
                    });
                }
            }
            else
            {
                // Thang thẳng 2 vế nối tiếp qua chiếu nghỉ ở giữa
                int n1 = config.Flight1Risers;
                int n2 = config.Flight2Risers;
                double h1 = n1 * r;
                double h2 = hTotal - h1;
                double run1 = Math.Max((n1 - 1) * t, t);
                double run2 = Math.Max((n2 - 1) * t, t);
                double lLand = config.LandingLengthMm;

                double slope1 = h1 / run1;
                double slope2 = h2 / run2;
                result.InclineAngleDeg = Math.Atan(slope1) * (180.0 / Math.PI);

                // Vế 1
                result.Flights.Add(new FlightGeometry
                {
                    FlightIndex = 1,
                    Name = "Vế thang 1",
                    ThicknessMm = config.WaistSlabThicknessMm,
                    RiserCount = n1,
                    RiserHeightMm = r,
                    TreadDepthMm = t,
                    FlightWidthMm = w,
                    RunLengthMm = run1,
                    BaseOffsetMm = config.BaseOffsetMm,
                    Slope = slope1,
                    SlopeArrowStart = new StairPoint3D(0, w / 2.0, 0),
                    SlopeArrowEnd = new StairPoint3D(run1, w / 2.0, 0),
                    ProfilePoints = new List<StairPoint3D>
                    {
                        new StairPoint3D(0, 0, 0),
                        new StairPoint3D(run1, 0, 0),
                        new StairPoint3D(run1, w, 0),
                        new StairPoint3D(0, w, 0)
                    }
                });

                // Chiếu nghỉ giữa
                result.Landings.Add(new LandingGeometry
                {
                    LandingIndex = 1,
                    Name = "Chiếu nghỉ giữa",
                    ThicknessMm = config.LandingSlabThicknessMm,
                    LengthMm = lLand,
                    WidthMm = w,
                    ElevationOffsetMm = config.BaseOffsetMm + h1,
                    ProfilePoints = new List<StairPoint3D>
                    {
                        new StairPoint3D(run1, 0, 0),
                        new StairPoint3D(run1 + lLand, 0, 0),
                        new StairPoint3D(run1 + lLand, w, 0),
                        new StairPoint3D(run1, w, 0)
                    }
                });

                // Vế 2
                double x2Start = run1 + lLand;
                double x2End = x2Start + run2;
                result.Flights.Add(new FlightGeometry
                {
                    FlightIndex = 2,
                    Name = "Vế thang 2",
                    ThicknessMm = config.WaistSlabThicknessMm,
                    RiserCount = n2,
                    RiserHeightMm = r,
                    TreadDepthMm = t,
                    FlightWidthMm = w,
                    RunLengthMm = run2,
                    BaseOffsetMm = config.BaseOffsetMm + h1,
                    Slope = slope2,
                    SlopeArrowStart = new StairPoint3D(x2Start, w / 2.0, 0),
                    SlopeArrowEnd = new StairPoint3D(x2End, w / 2.0, 0),
                    ProfilePoints = new List<StairPoint3D>
                    {
                        new StairPoint3D(x2Start, 0, 0),
                        new StairPoint3D(x2End, 0, 0),
                        new StairPoint3D(x2End, w, 0),
                        new StairPoint3D(x2Start, w, 0)
                    }
                });

                // Dầm đỡ
                if (config.SupportBeamOption != SupportBeamOption.None)
                {
                    // Dầm chiếu nghỉ
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm chiếu nghỉ",
                        StartPoint = new StairPoint3D(run1, 0, config.BaseOffsetMm + h1),
                        EndPoint = new StairPoint3D(run1, w, config.BaseOffsetMm + h1),
                        ElevationMm = config.BaseOffsetMm + h1
                    });

                    if (config.SupportBeamOption == SupportBeamOption.FullBeams)
                    {
                        result.Beams.Add(new BeamGeometry
                        {
                            Description = "Dầm chân thang",
                            StartPoint = new StairPoint3D(0, 0, config.BaseOffsetMm),
                            EndPoint = new StairPoint3D(0, w, config.BaseOffsetMm),
                            ElevationMm = config.BaseOffsetMm
                        });
                        result.Beams.Add(new BeamGeometry
                        {
                            Description = "Dầm đỉnh thang",
                            StartPoint = new StairPoint3D(x2End, 0, config.BaseOffsetMm + hTotal),
                            EndPoint = new StairPoint3D(x2End, w, config.BaseOffsetMm + hTotal),
                            ElevationMm = config.BaseOffsetMm + hTotal
                        });
                    }
                }
            }
        }

        #endregion

        #region 2. U-Shaped Stair (Thang chữ U 2 vế đảo chiều 180°)

        private static void BuildUShapedStair(StairConfig config, StairGeometryResult result)
        {
            double w = config.StairWidthMm;
            double t = config.TreadDepthMm;
            double g = config.GapBetweenFlightsMm;
            double lLand = config.LandingLengthMm;
            double hTotal = config.TotalRiseMm;
            int n1 = config.Flight1Risers;
            int n2 = config.Flight2Risers;
            double r = hTotal / config.TotalRisers;

            double hLanding = n1 * r;
            double hFlight2 = hTotal - hLanding;

            double run1 = Math.Max((n1 - 1) * t, t);
            double run2 = Math.Max((n2 - 1) * t, t);

            double slope1 = hLanding / run1;
            double slope2 = hFlight2 / run2;
            result.InclineAngleDeg = Math.Atan(slope1) * (180.0 / Math.PI);

            double totalWidth = 2.0 * w + g;

            // Xác định tọa độ Y của vế 1 và vế 2 theo TurnDirection
            // Left: Vế 1 ở [0, w], vế 2 ở [w + g, totalWidth]
            // Right: Vế 1 ở [w + g, totalWidth], vế 2 ở [0, w]
            double y1Min = (config.TurnDirection == TurnDirection.Left) ? 0.0 : (w + g);
            double y1Max = y1Min + w;

            double y2Min = (config.TurnDirection == TurnDirection.Left) ? (w + g) : 0.0;
            double y2Max = y2Min + w;

            // 1. Vế thang 1: chạy từ X = 0 đến X = run1
            var flight1 = new FlightGeometry
            {
                FlightIndex = 1,
                Name = "Vế thang 1",
                ThicknessMm = config.WaistSlabThicknessMm,
                RiserCount = n1,
                RiserHeightMm = r,
                TreadDepthMm = t,
                FlightWidthMm = w,
                RunLengthMm = run1,
                BaseOffsetMm = config.BaseOffsetMm,
                Slope = slope1,
                SlopeArrowStart = new StairPoint3D(0, (y1Min + y1Max) / 2.0, 0),
                SlopeArrowEnd = new StairPoint3D(run1, (y1Min + y1Max) / 2.0, 0),
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(0, y1Min, 0),
                    new StairPoint3D(run1, y1Min, 0),
                    new StairPoint3D(run1, y1Max, 0),
                    new StairPoint3D(0, y1Max, 0)
                }
            };
            result.Flights.Add(flight1);

            // 2. Chiếu nghỉ (Landing): nằm từ X = run1 đến X = run1 + lLand, phủ suốt bề ngang 2 vế
            var landing = new LandingGeometry
            {
                LandingIndex = 1,
                Name = "Chiếu nghỉ tầng lửng",
                ThicknessMm = config.LandingSlabThicknessMm,
                LengthMm = lLand,
                WidthMm = totalWidth,
                ElevationOffsetMm = config.BaseOffsetMm + hLanding,
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(run1, 0, 0),
                    new StairPoint3D(run1 + lLand, 0, 0),
                    new StairPoint3D(run1 + lLand, totalWidth, 0),
                    new StairPoint3D(run1, totalWidth, 0)
                }
            };
            result.Landings.Add(landing);

            // 3. Vế thang 2: bắt đầu từ mép chiếu nghỉ X = run1, đi lùi về X = run1 - run2
            // SlopeArrow: Start tại X = run1 (thấp, hLanding), End tại X = run1 - run2 (cao, hTotal)
            double x2End = run1 - run2;
            var flight2 = new FlightGeometry
            {
                FlightIndex = 2,
                Name = "Vế thang 2",
                ThicknessMm = config.WaistSlabThicknessMm,
                RiserCount = n2,
                RiserHeightMm = r,
                TreadDepthMm = t,
                FlightWidthMm = w,
                RunLengthMm = run2,
                BaseOffsetMm = config.BaseOffsetMm + hLanding,
                Slope = slope2,
                SlopeArrowStart = new StairPoint3D(run1, (y2Min + y2Max) / 2.0, 0),
                SlopeArrowEnd = new StairPoint3D(x2End, (y2Min + y2Max) / 2.0, 0),
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(run1, y2Min, 0),
                    new StairPoint3D(x2End, y2Min, 0),
                    new StairPoint3D(x2End, y2Max, 0),
                    new StairPoint3D(run1, y2Max, 0)
                }
            };
            result.Flights.Add(flight2);

            // 4. Bố trí Dầm đỡ kết cấu
            if (config.SupportBeamOption != SupportBeamOption.None)
            {
                // Dầm chiếu nghỉ (nằm dưới mép giao giữa vế và chiếu nghỉ tại X = run1)
                result.Beams.Add(new BeamGeometry
                {
                    Description = "Dầm chiếu nghỉ",
                    StartPoint = new StairPoint3D(run1, 0, config.BaseOffsetMm + hLanding),
                    EndPoint = new StairPoint3D(run1, totalWidth, config.BaseOffsetMm + hLanding),
                    ElevationMm = config.BaseOffsetMm + hLanding
                });

                if (config.SupportBeamOption == SupportBeamOption.FullBeams)
                {
                    // Dầm chân thang (Start của vế 1)
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm chân thang",
                        StartPoint = new StairPoint3D(0, y1Min, config.BaseOffsetMm),
                        EndPoint = new StairPoint3D(0, y1Max, config.BaseOffsetMm),
                        ElevationMm = config.BaseOffsetMm
                    });

                    // Dầm đỉnh thang (End của vế 2)
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm đỉnh thang",
                        StartPoint = new StairPoint3D(x2End, y2Min, config.BaseOffsetMm + hTotal),
                        EndPoint = new StairPoint3D(x2End, y2Max, config.BaseOffsetMm + hTotal),
                        ElevationMm = config.BaseOffsetMm + hTotal
                    });
                }
            }
        }

        #endregion

        #region 3. L-Shaped Stair (Thang chữ L vuông góc 90°)

        private static void BuildLShapedStair(StairConfig config, StairGeometryResult result)
        {
            double w = config.StairWidthMm;
            double t = config.TreadDepthMm;
            double hTotal = config.TotalRiseMm;
            int n1 = config.Flight1Risers;
            int n2 = config.Flight2Risers;
            double r = hTotal / config.TotalRisers;

            double hLanding = n1 * r;
            double hFlight2 = hTotal - hLanding;

            double run1 = Math.Max((n1 - 1) * t, t);
            double run2 = Math.Max((n2 - 1) * t, t);

            double slope1 = hLanding / run1;
            double slope2 = hFlight2 / run2;
            result.InclineAngleDeg = Math.Atan(slope1) * (180.0 / Math.PI);

            // 1. Vế 1: chạy theo phương +X từ X = 0 đến X = run1, Y nằm trong [0, w]
            var flight1 = new FlightGeometry
            {
                FlightIndex = 1,
                Name = "Vế thang 1",
                ThicknessMm = config.WaistSlabThicknessMm,
                RiserCount = n1,
                RiserHeightMm = r,
                TreadDepthMm = t,
                FlightWidthMm = w,
                RunLengthMm = run1,
                BaseOffsetMm = config.BaseOffsetMm,
                Slope = slope1,
                SlopeArrowStart = new StairPoint3D(0, w / 2.0, 0),
                SlopeArrowEnd = new StairPoint3D(run1, w / 2.0, 0),
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(0, 0, 0),
                    new StairPoint3D(run1, 0, 0),
                    new StairPoint3D(run1, w, 0),
                    new StairPoint3D(0, w, 0)
                }
            };
            result.Flights.Add(flight1);

            // 2. Chiếu nghỉ góc (Corner Landing): kích thước w x w tại góc đổi hướng
            // Nếu rẽ trái (Left): phát triển lên theo phương +Y
            // Nếu rẽ phải (Right): phát triển xuống theo phương -Y
            double yLandingStart = (config.TurnDirection == TurnDirection.Left) ? 0.0 : -w;
            double yLandingEnd = (config.TurnDirection == TurnDirection.Left) ? w : 0.0;

            var landing = new LandingGeometry
            {
                LandingIndex = 1,
                Name = "Chiếu nghỉ góc chữ L",
                ThicknessMm = config.LandingSlabThicknessMm,
                LengthMm = w,
                WidthMm = w,
                ElevationOffsetMm = config.BaseOffsetMm + hLanding,
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(run1, yLandingStart, 0),
                    new StairPoint3D(run1 + w, yLandingStart, 0),
                    new StairPoint3D(run1 + w, yLandingEnd, 0),
                    new StairPoint3D(run1, yLandingEnd, 0)
                }
            };
            result.Landings.Add(landing);

            // 3. Vế 2: chạy theo phương vuông góc (phương Y)
            double x2Start = run1;
            double x2End = run1 + w;
            double y2Start, y2End;

            if (config.TurnDirection == TurnDirection.Left)
            {
                // Đi từ Y = w lên Y = w + run2
                y2Start = w;
                y2End = w + run2;
            }
            else
            {
                // Đi từ Y = -w xuống Y = -(w + run2)
                y2Start = -w;
                y2End = -(w + run2);
            }

            var flight2 = new FlightGeometry
            {
                FlightIndex = 2,
                Name = "Vế thang 2",
                ThicknessMm = config.WaistSlabThicknessMm,
                RiserCount = n2,
                RiserHeightMm = r,
                TreadDepthMm = t,
                FlightWidthMm = w,
                RunLengthMm = run2,
                BaseOffsetMm = config.BaseOffsetMm + hLanding,
                Slope = slope2,
                SlopeArrowStart = new StairPoint3D((x2Start + x2End) / 2.0, y2Start, 0),
                SlopeArrowEnd = new StairPoint3D((x2Start + x2End) / 2.0, y2End, 0),
                ProfilePoints = new List<StairPoint3D>
                {
                    new StairPoint3D(x2Start, y2Start, 0),
                    new StairPoint3D(x2End, y2Start, 0),
                    new StairPoint3D(x2End, y2End, 0),
                    new StairPoint3D(x2Start, y2End, 0)
                }
            };
            result.Flights.Add(flight2);

            // 4. Bố trí Dầm đỡ kết cấu
            if (config.SupportBeamOption != SupportBeamOption.None)
            {
                // Dầm chiếu nghỉ (dưới mép giao chiếu nghỉ và vế 1)
                result.Beams.Add(new BeamGeometry
                {
                    Description = "Dầm chiếu nghỉ",
                    StartPoint = new StairPoint3D(run1, yLandingStart, config.BaseOffsetMm + hLanding),
                    EndPoint = new StairPoint3D(run1, yLandingEnd, config.BaseOffsetMm + hLanding),
                    ElevationMm = config.BaseOffsetMm + hLanding
                });

                if (config.SupportBeamOption == SupportBeamOption.FullBeams)
                {
                    // Dầm chân thang (vế 1)
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm chân thang",
                        StartPoint = new StairPoint3D(0, 0, config.BaseOffsetMm),
                        EndPoint = new StairPoint3D(0, w, config.BaseOffsetMm),
                        ElevationMm = config.BaseOffsetMm
                    });

                    // Dầm đỉnh thang (vế 2)
                    result.Beams.Add(new BeamGeometry
                    {
                        Description = "Dầm đỉnh thang",
                        StartPoint = new StairPoint3D(x2Start, y2End, config.BaseOffsetMm + hTotal),
                        EndPoint = new StairPoint3D(x2End, y2End, config.BaseOffsetMm + hTotal),
                        ElevationMm = config.BaseOffsetMm + hTotal
                    });
                }
            }
        }

        #endregion

        #region 4. Transform: Rotate & Translate

        private static void ApplyTransform(StairGeometryResult result, double insertX, double insertY, double rotationDeg)
        {
            if (Math.Abs(insertX) < 1e-6 && Math.Abs(insertY) < 1e-6 && Math.Abs(rotationDeg) < 1e-6)
                return;

            double rad = rotationDeg * (Math.PI / 180.0);
            double cos = Math.Cos(rad);
            double sin = Math.Sin(rad);

            StairPoint3D TransformPoint(StairPoint3D pt)
            {
                double xRot = pt.X * cos - pt.Y * sin;
                double yRot = pt.X * sin + pt.Y * cos;
                return new StairPoint3D(xRot + insertX, yRot + insertY, pt.Z);
            }

            // 1. Flights
            foreach (var flight in result.Flights)
            {
                flight.SlopeArrowStart = TransformPoint(flight.SlopeArrowStart);
                flight.SlopeArrowEnd = TransformPoint(flight.SlopeArrowEnd);

                for (int i = 0; i < flight.ProfilePoints.Count; i++)
                {
                    flight.ProfilePoints[i] = TransformPoint(flight.ProfilePoints[i]);
                }
            }

            // 2. Landings
            foreach (var landing in result.Landings)
            {
                for (int i = 0; i < landing.ProfilePoints.Count; i++)
                {
                    landing.ProfilePoints[i] = TransformPoint(landing.ProfilePoints[i]);
                }
            }

            // 3. Beams
            foreach (var beam in result.Beams)
            {
                beam.StartPoint = TransformPoint(beam.StartPoint);
                beam.EndPoint = TransformPoint(beam.EndPoint);
            }
        }

        #endregion
    }
}
