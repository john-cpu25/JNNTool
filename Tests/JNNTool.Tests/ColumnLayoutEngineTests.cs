using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Column.Engine;
using JNNTool.RebarSuite.Column.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class ColumnLayoutEngineTests
    {
        [Theory]
        [InlineData(2, 2, 4)]
        [InlineData(3, 3, 8)]
        [InlineData(4, 3, 10)]
        [InlineData(4, 4, 12)]
        public void Calculate_VerticalBars_GeneratesExactBarCount(int countB, int countH, int expectedTotal)
        {
            var stack = new ColumnStackModel
            {
                Stories = new List<ColumnStoryModel>
                {
                    new ColumnStoryModel
                    {
                        StoryIndex = 0,
                        BottomElevationMm = 0,
                        TopElevationMm = 3600,
                        WidthBMm = 400,
                        HeightHMm = 500,
                        IntersectingBeamDepthMm = 500
                    }
                }
            };

            var settings = new ColumnRebarSettings
            {
                CountB = countB,
                CountH = countH,
                MainDiameter = "D20"
            };

            var result = ColumnLayoutEngine.Calculate(stack, settings);

            Assert.NotNull(result);
            int vertBarCount = result.RebarSets.Count(s => s.Role == ColumnRebarRole.VerticalMain);
            Assert.Equal(expectedTotal, vertBarCount);
        }

        [Fact]
        public void Calculate_DenseStirrupZone_FollowsTCVN()
        {
            // Cột cao 3600, dầm 500 -> Clear = 3100
            // H_clear / 6 = 516.67, B = 400, H = 500
            // max(516.67, 500) = 516.67 -> làm tròn 50mm = 550 mm
            var stack = new ColumnStackModel
            {
                Stories = new List<ColumnStoryModel>
                {
                    new ColumnStoryModel
                    {
                        StoryIndex = 0,
                        BottomElevationMm = 0,
                        TopElevationMm = 3600,
                        WidthBMm = 400,
                        HeightHMm = 500,
                        IntersectingBeamDepthMm = 500
                    }
                }
            };

            var settings = new ColumnRebarSettings
            {
                StirrupSpacingDense = 100,
                StirrupSpacingSparse = 200
            };

            var result = ColumnLayoutEngine.Calculate(stack, settings);

            var baseDense = result.RebarSets.FirstOrDefault(s => s.Role == ColumnRebarRole.StirrupBaseDense);
            Assert.NotNull(baseDense);
            Assert.Equal(550.0, baseDense.RangeHeightMm);

            var topDense = result.RebarSets.FirstOrDefault(s => s.Role == ColumnRebarRole.StirrupTopDense);
            Assert.NotNull(topDense);
            Assert.Equal(550.0, topDense.RangeHeightMm);

            // Vùng thưa = 3100 - 2 * 550 = 2000 mm
            var midSparse = result.RebarSets.FirstOrDefault(s => s.Role == ColumnRebarRole.StirrupMidSparse);
            Assert.NotNull(midSparse);
            Assert.Equal(2000.0, midSparse.RangeHeightMm);
        }

        [Fact]
        public void Calculate_SectionChange_GeneratesCrankedLap()
        {
            // Tầng 1: 500x500 -> Tầng 2: 400x400 (thu nhỏ mỗi bên 50mm)
            var stack = new ColumnStackModel
            {
                Stories = new List<ColumnStoryModel>
                {
                    new ColumnStoryModel
                    {
                        StoryIndex = 0,
                        BottomElevationMm = 0,
                        TopElevationMm = 3600,
                        WidthBMm = 500,
                        HeightHMm = 500,
                        IntersectingBeamDepthMm = 500
                    },
                    new ColumnStoryModel
                    {
                        StoryIndex = 1,
                        BottomElevationMm = 3600,
                        TopElevationMm = 7200,
                        WidthBMm = 400,
                        HeightHMm = 400,
                        IntersectingBeamDepthMm = 500
                    }
                }
            };

            var settings = new ColumnRebarSettings
            {
                EnableCrankedLap = true,
                CountB = 2,
                CountH = 2
            };

            var result = ColumnLayoutEngine.Calculate(stack, settings);

            // Kiểm tra các thanh thép tầng 1 có bẻ cổ chai
            var crankedBars = result.RebarSets.Where(s => s.Role == ColumnRebarRole.VerticalCranked).ToList();
            Assert.Equal(4, crankedBars.Count); // 4 thanh góc tầng 1
            Assert.Equal(4, crankedBars[0].CurvePoints.Count); // 4 điểm uốn cổ chai
        }
    }
}
