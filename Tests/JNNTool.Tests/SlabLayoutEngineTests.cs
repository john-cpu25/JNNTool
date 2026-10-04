using System.Linq;
using JNNTool.RebarSuite.Slab.Engine;
using JNNTool.RebarSuite.Slab.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class SlabLayoutEngineTests
    {
        [Fact]
        public void Calculate_StandardSlab_GeneratesBottomLayersAndTopAdd()
        {
            var slab = new SlabModel
            {
                ThicknessMm = 120,
                TopElevationMm = 3000,
                OuterBoundary = new System.Collections.Generic.List<Point2D>
                {
                    new Point2D(0, 0),
                    new Point2D(4000, 0),
                    new Point2D(4000, 5000),
                    new Point2D(0, 5000)
                }
            };

            var settings = new SlabRebarSettings
            {
                BottomXDiameter = "D10",
                BottomXSpacing = 150,
                BottomYDiameter = "D10",
                BottomYSpacing = 150,
                IsCreateTopAdd = true,
                TopAddSpanRatio = 0.25, // L/4 = 1000mm
                CoverBottom = 15,
                CoverTop = 15,
                CoverSide = 15
            };

            var result = SlabLayoutEngine.Calculate(slab, settings);

            Assert.NotNull(result);
            Assert.NotEmpty(result.RebarSets);

            // Kiểm tra lớp dưới X
            var botX = result.RebarSets.FirstOrDefault(s => s.LayerType == RebarLayerType.BottomX);
            Assert.NotNull(botX);
            Assert.Equal("D10", botX.Diameter);
            Assert.True(botX.BarCount > 20);
            // Chiều dài thanh = 4000 - 2 * 15 = 3970 mm
            double lenX = botX.CurvePoints[1].X - botX.CurvePoints[0].X;
            Assert.Equal(3970.0, lenX);

            // Kiểm tra lớp dưới Y
            var botY = result.RebarSets.FirstOrDefault(s => s.LayerType == RebarLayerType.BottomY);
            Assert.NotNull(botY);
            // Cao độ Z của lớp Y phải nằm trên lớp X
            Assert.True(botY.CurvePoints[0].Z > botX.CurvePoints[0].Z);

            // Kiểm tra thép mũ L/4
            var topAdd = result.RebarSets.Where(s => s.LayerType == RebarLayerType.TopAdd).ToList();
            Assert.NotEmpty(topAdd);
            // 4000 * 0.25 = 1000 mm
            var muLeft = topAdd[0];
            double muLen = muLeft.CurvePoints[2].X - muLeft.CurvePoints[1].X;
            Assert.Equal(1000.0, muLen);
        }

        [Fact]
        public void Calculate_TopAdd_RoundsUpTo50mm()
        {
            var slab = new SlabModel
            {
                ThicknessMm = 120,
                TopElevationMm = 3000,
                OuterBoundary = new System.Collections.Generic.List<Point2D>
                {
                    new Point2D(0, 0),
                    new Point2D(3500, 0), // Nhịp 3500: L/4 = 875 -> làm tròn 900 mm
                    new Point2D(3500, 4000),
                    new Point2D(0, 4000)
                }
            };

            var settings = new SlabRebarSettings
            {
                IsCreateTopAdd = true,
                TopAddSpanRatio = 0.25
            };

            var result = SlabLayoutEngine.Calculate(slab, settings);
            var muLeft = result.RebarSets.First(s => s.LayerType == RebarLayerType.TopAdd);
            double muLen = muLeft.CurvePoints[2].X - muLeft.CurvePoints[1].X;

            Assert.Equal(900.0, muLen); // 875 -> 900
        }

        [Fact]
        public void Calculate_SmallSpan_IgnoresTopAdd()
        {
            var slab = new SlabModel
            {
                ThicknessMm = 100,
                OuterBoundary = new System.Collections.Generic.List<Point2D>
                {
                    new Point2D(0, 0),
                    new Point2D(700, 0), // Nhịp 700 < 800 mm
                    new Point2D(700, 2000),
                    new Point2D(0, 2000)
                }
            };

            var settings = new SlabRebarSettings
            {
                IsCreateTopAdd = true,
                MinSpanToCreateTopAdd = 800.0
            };

            var result = SlabLayoutEngine.Calculate(slab, settings);
            var topAddCount = result.RebarSets.Count(s => s.LayerType == RebarLayerType.TopAdd);

            Assert.Equal(0, topAddCount);
        }
    }
}
