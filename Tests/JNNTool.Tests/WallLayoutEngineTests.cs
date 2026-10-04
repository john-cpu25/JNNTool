using System;
using System.Linq;
using JNNTool.RebarSuite.Slab.Models;
using JNNTool.RebarSuite.Wall.Engine;
using JNNTool.RebarSuite.Wall.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class WallLayoutEngineTests
    {
        [Fact]
        public void Calculate_WallWithStandardSettings_GeneratesVerticalHorizontalAndBoundaryTies()
        {
            var wall = new WallModel
            {
                LengthMm = 3000,
                HeightMm = 3300,
                ThicknessMm = 250,
                BottomElevationMm = 0,
                TopElevationMm = 3300,
                StartPoint = new Point3D(0, 0, 0),
                EndPoint = new Point3D(3000, 0, 0),
                Direction = new Point3D(1, 0, 0),
                Normal = new Point3D(0, 1, 0),
                IsTopStory = false
            };

            var settings = new WallRebarSettings
            {
                VerticalDiameter = "D12",
                VerticalSpacing = 150,
                HorizontalDiameter = "D10",
                HorizontalSpacing = 150,
                CreateBoundaryTies = true,
                BoundaryTieDiameter = "D8",
                BoundaryTieSpacing = 150
            };

            var result = WallLayoutEngine.Calculate(wall, settings);

            Assert.NotNull(result);
            Assert.Equal(6, result.RebarSets.Count);

            var vertOuter = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.VerticalLayerOuter);
            var vertInner = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.VerticalLayerInner);
            var horizOuter = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.HorizontalLayerOuter);
            var horizInner = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.HorizontalLayerInner);
            var tieStart = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.BoundaryTieStart);
            var tieEnd = result.RebarSets.FirstOrDefault(r => r.Role == WallRebarRole.BoundaryTieEnd);

            Assert.NotNull(vertOuter);
            Assert.NotNull(vertInner);
            Assert.NotNull(horizOuter);
            Assert.NotNull(horizInner);
            Assert.NotNull(tieStart);
            Assert.NotNull(tieEnd);

            Assert.True(vertOuter.BarCount >= 18);
            Assert.True(horizOuter.BarCount >= 20);
        }

        [Fact]
        public void Calculate_WallIsTopStory_VerticalBarsDoNotExtendLapLength()
        {
            var wallNormalStory = new WallModel
            {
                LengthMm = 3000,
                HeightMm = 3000,
                ThicknessMm = 200,
                BottomElevationMm = 0,
                TopElevationMm = 3000,
                StartPoint = new Point3D(0, 0, 0),
                IsTopStory = false
            };

            var wallTopStory = new WallModel
            {
                LengthMm = 3000,
                HeightMm = 3000,
                ThicknessMm = 200,
                BottomElevationMm = 0,
                TopElevationMm = 3000,
                StartPoint = new Point3D(0, 0, 0),
                IsTopStory = true
            };

            var settings = new WallRebarSettings
            {
                VerticalDiameter = "D12",
                LapMultiplier = 40.0,
                CoverNormalMm = 20.0,
                IsWaterproofOrBasement = false
            };

            var resNormal = WallLayoutEngine.Calculate(wallNormalStory, settings);
            var resTop = WallLayoutEngine.Calculate(wallTopStory, settings);

            var vNormal = resNormal.RebarSets.First(r => r.Role == WallRebarRole.VerticalLayerOuter);
            var vTop = resTop.RebarSets.First(r => r.Role == WallRebarRole.VerticalLayerOuter);

            double topZNormal = vNormal.CurvePoints[1].Z;
            double topZTop = vTop.CurvePoints[1].Z;

            // Nối chồng: 3000 + 40 * 12 = 3480
            Assert.Equal(3480.0, topZNormal, 1);

            // Tầng trên cùng: 3000 - 20 = 2980
            Assert.Equal(2980.0, topZTop, 1);
        }

        [Fact]
        public void Calculate_WaterproofBasement_IncreasesCover()
        {
            var wall = new WallModel
            {
                LengthMm = 2000,
                HeightMm = 3000,
                ThicknessMm = 300,
                BottomElevationMm = 0,
                TopElevationMm = 3000,
                StartPoint = new Point3D(0, 0, 0),
                Direction = new Point3D(1, 0, 0),
                Normal = new Point3D(0, 1, 0)
            };

            var settingsNormal = new WallRebarSettings
            {
                IsWaterproofOrBasement = false,
                CoverNormalMm = 20.0
            };

            var settingsWaterproof = new WallRebarSettings
            {
                IsWaterproofOrBasement = true,
                CoverWaterproofMm = 40.0
            };

            var resNorm = WallLayoutEngine.Calculate(wall, settingsNormal);
            var resWater = WallLayoutEngine.Calculate(wall, settingsWaterproof);

            var vertNorm = resNorm.RebarSets.First(r => r.Role == WallRebarRole.VerticalLayerOuter);
            var vertWater = resWater.RebarSets.First(r => r.Role == WallRebarRole.VerticalLayerOuter);

            // Tọa độ Y (theo Normal) của thép đứng phải nhỏ hơn khi lớp bảo vệ dày hơn
            Assert.True(vertWater.CurvePoints[0].Y < vertNorm.CurvePoints[0].Y);
        }

        [Fact]
        public void Calculate_WithoutBoundaryTies_GeneratesOnly4RebarSets()
        {
            var wall = new WallModel
            {
                LengthMm = 2000,
                HeightMm = 2500,
                ThicknessMm = 200,
                StartPoint = new Point3D(0, 0, 0)
            };

            var settings = new WallRebarSettings
            {
                CreateBoundaryTies = false
            };

            var res = WallLayoutEngine.Calculate(wall, settings);

            Assert.Equal(4, res.RebarSets.Count);
            Assert.DoesNotContain(res.RebarSets, r => r.Role == WallRebarRole.BoundaryTieStart);
            Assert.DoesNotContain(res.RebarSets, r => r.Role == WallRebarRole.BoundaryTieEnd);
        }
    }
}
