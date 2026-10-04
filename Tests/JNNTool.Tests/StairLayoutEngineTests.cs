using System;
using System.Linq;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Stair.Engine;
using JNNTool.RebarSuite.Stair.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class StairLayoutEngineTests
    {
        [Fact]
        public void Calculate_StandardFlight_GeneratesBottomBarsTopSupportAndDistribution()
        {
            var stair = new StairModel
            {
                StairName = "Stair 1",
                Flights =
                {
                    new StairFlightModel
                    {
                        FlightIndex = 1,
                        WidthMm = 1200,
                        SlabThicknessMm = 120,
                        RiserCount = 10,
                        RiserHeightMm = 160,
                        TreadDepthMm = 280,
                        StartPoint = new Point3D(0, 0, 0),
                        EndPoint = new Point3D(2520, 0, 1600),
                        Direction = new Point3D(1, 0, 0),
                        Normal = new Point3D(0, 1, 0)
                    }
                }
            };

            var settings = new StairRebarSettings
            {
                BottomDiameter = "D10",
                BottomSpacing = 150,
                BottomAnchorLengthMm = 350,
                CreateTopBars = true,
                TopDiameter = "D10",
                TopSpacing = 150,
                TopSpanFactor = 0.25,
                DistributionDiameter = "D6",
                DistributionSpacing = 200
            };

            var result = StairLayoutEngine.Calculate(stair, settings);

            Assert.NotNull(result);

            // Kiểm tra thép lớp dưới
            var botBar = result.RebarSets.FirstOrDefault(r => r.Role == StairRebarRole.BottomMainFlight);
            Assert.NotNull(botBar);
            Assert.Equal("D10", botBar.Diameter);
            Assert.True(botBar.BarCount >= 8);
            Assert.Equal(4, botBar.CurvePoints.Count); // Neo chân -> chân dốc -> đỉnh dốc -> neo chiếu nghỉ

            // Kiểm tra thép mũ gối trên
            var topBar = result.RebarSets.FirstOrDefault(r => r.Role == StairRebarRole.TopSupportTop);
            Assert.NotNull(topBar);
            Assert.Equal("D10", topBar.Diameter);
            Assert.True(topBar.BarCount >= 8);

            // Kiểm tra thép phân bố dọc theo chiều dốc
            var distBar = result.RebarSets.FirstOrDefault(r => r.Role == StairRebarRole.DistributionFlight);
            Assert.NotNull(distBar);
            Assert.Equal("D6", distBar.Diameter);
            Assert.True(distBar.BarCount >= 14);
        }

        [Fact]
        public void Calculate_WithoutTopBars_GeneratesOnlyBottomAndDistribution()
        {
            var stair = new StairModel
            {
                Flights =
                {
                    new StairFlightModel
                    {
                        FlightIndex = 1,
                        WidthMm = 1000,
                        StartPoint = new Point3D(0, 0, 0),
                        EndPoint = new Point3D(2000, 0, 1500)
                    }
                }
            };

            var settings = new StairRebarSettings
            {
                CreateTopBars = false
            };

            var result = StairLayoutEngine.Calculate(stair, settings);

            Assert.DoesNotContain(result.RebarSets, r => r.Role == StairRebarRole.TopSupportTop);
            Assert.Contains(result.RebarSets, r => r.Role == StairRebarRole.BottomMainFlight);
            Assert.Contains(result.RebarSets, r => r.Role == StairRebarRole.DistributionFlight);
        }
    }
}
