using System;
using System.Linq;
using JNNTool.Tools.StairModel.Engine;
using Xunit;

namespace JNNTool.Tests
{
    public class StairGeometryEngineTests
    {
        [Fact]
        public void Test_StraightStair_SingleFlight()
        {
            var config = new StairConfig
            {
                StairType = StairType.Straight,
                TotalRisers = 20,
                BaseElevationMm = 0.0,
                TopElevationMm = 3600.0,
                StairWidthMm = 1200.0,
                TreadDepthMm = 280.0,
                WaistSlabThicknessMm = 120.0,
                LandingLengthMm = 0.0 // single flight
            };

            var result = StairGeometryEngine.Generate(config);

            Assert.True(result.Success);
            Assert.Single(result.Flights);
            Assert.Empty(result.Landings);

            var flight = result.Flights[0];
            Assert.Equal(180.0, result.RiserHeightMm, 2); // 3600 / 20 = 180
            Assert.Equal(640.0, result.BlondelNumberMm, 2); // 2*180 + 280 = 640

            double expectedRun = 19 * 280.0; // (20-1) * 280 = 5320
            Assert.Equal(expectedRun, flight.RunLengthMm, 2);
            Assert.Equal(3600.0 / expectedRun, flight.Slope, 4);

            // Profile has 4 points
            Assert.Equal(4, flight.ProfilePoints.Count);
        }

        [Fact]
        public void Test_UShapedStair_LeftTurn()
        {
            var config = new StairConfig
            {
                StairType = StairType.UShaped,
                TurnDirection = TurnDirection.Left,
                TotalRisers = 21,
                Flight1Risers = 11,
                Flight2Risers = 10,
                BaseElevationMm = 0.0,
                TopElevationMm = 3570.0,
                StairWidthMm = 1100.0,
                TreadDepthMm = 270.0,
                LandingLengthMm = 1200.0,
                GapBetweenFlightsMm = 100.0,
                SupportBeamOption = SupportBeamOption.FullBeams
            };

            var result = StairGeometryEngine.Generate(config);

            Assert.True(result.Success);
            Assert.Equal(2, result.Flights.Count);
            Assert.Single(result.Landings);
            Assert.Equal(3, result.Beams.Count); // Landing, Foot, Top

            double r = 3570.0 / 21.0; // 170.0 mm
            Assert.Equal(r, result.RiserHeightMm, 2);

            // Flight 1
            var f1 = result.Flights[0];
            double expectedH1 = 11 * r; // 1870.0 mm
            double expectedRun1 = 10 * 270.0; // 2700.0 mm
            Assert.Equal(expectedRun1, f1.RunLengthMm, 2);
            Assert.Equal(expectedH1 / expectedRun1, f1.Slope, 4);

            // Landing
            var land = result.Landings[0];
            double expectedTotalWidth = 2.0 * 1100.0 + 100.0; // 2300.0 mm
            Assert.Equal(expectedTotalWidth, land.WidthMm, 2);
            Assert.Equal(1200.0, land.LengthMm, 2);
            Assert.Equal(expectedH1, land.ElevationOffsetMm, 2);

            // Flight 2
            var f2 = result.Flights[1];
            double expectedH2 = 10 * 170.0; // 1700.0 mm
            double expectedRun2 = 9 * 270.0; // 2430.0 mm
            Assert.Equal(expectedRun2, f2.RunLengthMm, 2);
            Assert.Equal(expectedH2 / expectedRun2, f2.Slope, 4);
            Assert.Equal(expectedH1, f2.BaseOffsetMm, 2);
        }

        [Fact]
        public void Test_LShapedStair_Geometry()
        {
            var config = new StairConfig
            {
                StairType = StairType.LShaped,
                TurnDirection = TurnDirection.Left,
                TotalRisers = 20,
                Flight1Risers = 10,
                Flight2Risers = 10,
                BaseElevationMm = 0.0,
                TopElevationMm = 3400.0,
                StairWidthMm = 1000.0,
                TreadDepthMm = 260.0
            };

            var result = StairGeometryEngine.Generate(config);

            Assert.True(result.Success);
            Assert.Equal(2, result.Flights.Count);
            Assert.Single(result.Landings);

            var f1 = result.Flights[0];
            var f2 = result.Flights[1];
            var land = result.Landings[0];

            Assert.Equal(1000.0, land.WidthMm);
            Assert.Equal(1000.0, land.LengthMm);

            // Flight 2 runs perpendicular (along Y)
            double run2 = (10 - 1) * 260.0; // 2340.0
            Assert.Equal(run2, f2.RunLengthMm, 2);
        }

        [Fact]
        public void Test_StairValidator_CatchesInvalidInputs()
        {
            var config = new StairConfig
            {
                TotalRisers = 0,
                BaseElevationMm = 0.0,
                TopElevationMm = 0.0 // total rise = 0
            };

            var result = StairGeometryEngine.Generate(config);
            Assert.False(result.Success);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public void Test_Transform_RigidRotationPreservesDimensions()
        {
            var config = new StairConfig
            {
                StairType = StairType.UShaped,
                TotalRisers = 20,
                Flight1Risers = 10,
                Flight2Risers = 10,
                BaseElevationMm = 0.0,
                TopElevationMm = 3400.0,
                StairWidthMm = 1000.0,
                TreadDepthMm = 260.0,
                LandingLengthMm = 1200.0,
                InsertionPointX = 5000.0,
                InsertionPointY = 3000.0,
                RotationAngleDeg = 90.0
            };

            var result = StairGeometryEngine.Generate(config);
            Assert.True(result.Success);

            // Flight 1 profile width should still be 1000.0 mm
            var p0 = result.Flights[0].ProfilePoints[0];
            var p3 = result.Flights[0].ProfilePoints[3];
            double widthDist = Math.Sqrt(Math.Pow(p3.X - p0.X, 2) + Math.Pow(p3.Y - p0.Y, 2));
            Assert.Equal(1000.0, widthDist, 1);
        }
    }
}
