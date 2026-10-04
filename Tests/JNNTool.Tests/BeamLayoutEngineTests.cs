using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Beam.Engine;
using JNNTool.RebarSuite.Beam.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class BeamLayoutEngineTests
    {
        [Fact]
        public void Calculate_Stirrups_GeneratesDenseAndSparseZones()
        {
            var beam = new ContinuousBeamModel
            {
                Spans = new List<BeamSpanModel>
                {
                    new BeamSpanModel
                    {
                        SpanIndex = 0,
                        LengthMm = 4000,
                        WidthMm = 300,
                        HeightMm = 500,
                        LeftSupportWidthMm = 300,
                        RightSupportWidthMm = 300
                    }
                }
            };

            var settings = new BeamRebarSettings
            {
                StirrupDiameter = "D8",
                StirrupSpacingEnd = 100,
                StirrupSpacingMid = 200,
                StirrupDenseRangeRatio = 0.25
            };

            var result = BeamLayoutEngine.Calculate(beam, settings);

            Assert.NotNull(result);
            var denseLeft = result.RebarSets.FirstOrDefault(s => s.Role == BeamRebarRole.StirrupDense);
            Assert.NotNull(denseLeft);
            Assert.Equal("D8", denseLeft.Diameter);
            // 3700 * 0.25 = 925 -> round 950 mm
            Assert.Equal(950.0, denseLeft.RangeLengthMm);

            var sparseMid = result.RebarSets.FirstOrDefault(s => s.Role == BeamRebarRole.StirrupSparse);
            Assert.NotNull(sparseMid);
            // 3700 - 2 * 950 = 1800 mm
            Assert.Equal(1800.0, sparseMid.RangeLengthMm);
        }

        [Fact]
        public void Calculate_ContinuousSpans_GeneratesTopAddAtIntermediateSupport()
        {
            var beam = new ContinuousBeamModel
            {
                Spans = new List<BeamSpanModel>
                {
                    new BeamSpanModel
                    {
                        SpanIndex = 0,
                        LengthMm = 4000,
                        WidthMm = 300,
                        HeightMm = 500,
                        LeftSupportWidthMm = 300,
                        RightSupportWidthMm = 300
                    },
                    new BeamSpanModel
                    {
                        SpanIndex = 1,
                        LengthMm = 5000,
                        WidthMm = 300,
                        HeightMm = 500,
                        LeftSupportWidthMm = 300,
                        RightSupportWidthMm = 300
                    }
                }
            };

            var settings = new BeamRebarSettings
            {
                EnableTopAdd = true,
                TopAddDiameter = "D20",
                TopAddCount = 2,
                TopAddSpanRatio = 0.25
            };

            var result = BeamLayoutEngine.Calculate(beam, settings);

            var topAdd = result.RebarSets.FirstOrDefault(s => s.Role == BeamRebarRole.TopAddSupport);
            Assert.NotNull(topAdd);
            Assert.Equal(2, topAdd.BarCount);
            Assert.Equal("D20", topAdd.Diameter);

            // max(3700, 4700) = 4700 * 0.25 = 1175 -> round 1200 mm mỗi bên
            // Tổng chiều dài = 2400 mm
            double len = topAdd.CurvePoints[1].X - topAdd.CurvePoints[0].X;
            Assert.Equal(2400.0, len);
        }

        [Fact]
        public void Calculate_TallBeam_GeneratesSideRebar()
        {
            var beam500 = new ContinuousBeamModel
            {
                Spans = new List<BeamSpanModel>
                {
                    new BeamSpanModel { HeightMm = 500, LengthMm = 4000 }
                }
            };

            var beam700 = new ContinuousBeamModel
            {
                Spans = new List<BeamSpanModel>
                {
                    new BeamSpanModel { HeightMm = 700, LengthMm = 4000 }
                }
            };

            var settings = new BeamRebarSettings
            {
                AutoSideRebar = true,
                MinBeamHeightForSideRebar = 600
            };

            var result500 = BeamLayoutEngine.Calculate(beam500, settings);
            var result700 = BeamLayoutEngine.Calculate(beam700, settings);

            Assert.DoesNotContain(result500.RebarSets, s => s.Role == BeamRebarRole.SideRebar);
            Assert.Contains(result700.RebarSets, s => s.Role == BeamRebarRole.SideRebar);
        }
    }
}
