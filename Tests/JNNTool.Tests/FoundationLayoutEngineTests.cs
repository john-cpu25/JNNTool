using System;
using System.Linq;
using JNNTool.RebarSuite.Foundation.Engine;
using JNNTool.RebarSuite.Foundation.Models;
using JNNTool.RebarSuite.Slab.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class FoundationLayoutEngineTests
    {
        [Fact]
        public void Calculate_IsolatedFooting_GeneratesBottomLayersAndDowels()
        {
            var foundation = new FoundationModel
            {
                LengthXMm = 2000,
                WidthYMm = 2000,
                HeightZMm = 800,
                BottomElevationMm = -1500,
                CenterPoint = new Point3D(0, 0, -1100),
                ColumnWidthBMm = 400,
                ColumnHeightHMm = 400
            };

            var settings = new FoundationRebarSettings
            {
                BottomDiameterX = "D14",
                BottomSpacingX = 150,
                BottomDiameterY = "D14",
                BottomSpacingY = 150,
                HookBottomBarsUp = true,
                BottomHookHeightMm = 200,
                CreateTopGrid = false,
                CreateColumnDowels = true,
                DowelsCountB = 3,
                DowelsCountH = 3,
                DowelDiameter = "D20",
                DowelFootLengthMm = 300,
                DowelExtendHeightMm = 800
            };

            var result = FoundationLayoutEngine.Calculate(foundation, settings);

            Assert.NotNull(result);

            // Kiểm tra thép đáy phương X
            var botX = result.RebarSets.FirstOrDefault(r => r.Role == FoundationRebarRole.BottomLayerX);
            Assert.NotNull(botX);
            Assert.Equal("D14", botX.Diameter);
            Assert.True(botX.BarCount >= 13);
            Assert.Equal(4, botX.CurvePoints.Count); // Có móc bẻ 2 đầu

            // Kiểm tra thép đáy phương Y
            var botY = result.RebarSets.FirstOrDefault(r => r.Role == FoundationRebarRole.BottomLayerY);
            Assert.NotNull(botY);
            Assert.Equal("D14", botY.Diameter);
            Assert.True(botY.BarCount >= 13);

            // Kiểm tra thép chờ cột (3x3 = 8 thanh)
            var dowels = result.RebarSets.Where(r => r.Role == FoundationRebarRole.ColumnDowel).ToList();
            Assert.Equal(8, dowels.Count);
            foreach (var d in dowels)
            {
                Assert.Equal(3, d.CurvePoints.Count); // Chân vịt -> góc uốn -> đỉnh chờ
                double zFoot = d.CurvePoints[0].Z;
                double zTop = d.CurvePoints[2].Z;
                Assert.True(zTop > foundation.TopElevationMm); // Vươn cao hơn đỉnh móng
            }

            // Kiểm tra đai định vị
            var tie = result.RebarSets.FirstOrDefault(r => r.Role == FoundationRebarRole.ColumnDowelTie);
            Assert.NotNull(tie);
        }

        [Fact]
        public void Calculate_PileCapWithTopGrid_GeneratesTopAndBottomLayers()
        {
            var foundation = new FoundationModel
            {
                LengthXMm = 3000,
                WidthYMm = 2500,
                HeightZMm = 1200,
                BottomElevationMm = -2000,
                CenterPoint = new Point3D(0, 0, -1400)
            };

            var settings = new FoundationRebarSettings
            {
                CreateTopGrid = true,
                CreateColumnDowels = false
            };

            var result = FoundationLayoutEngine.Calculate(foundation, settings);

            Assert.Contains(result.RebarSets, r => r.Role == FoundationRebarRole.BottomLayerX);
            Assert.Contains(result.RebarSets, r => r.Role == FoundationRebarRole.BottomLayerY);
            Assert.Contains(result.RebarSets, r => r.Role == FoundationRebarRole.TopLayerX);
            Assert.Contains(result.RebarSets, r => r.Role == FoundationRebarRole.TopLayerY);
            Assert.DoesNotContain(result.RebarSets, r => r.Role == FoundationRebarRole.ColumnDowel);
        }
    }
}
