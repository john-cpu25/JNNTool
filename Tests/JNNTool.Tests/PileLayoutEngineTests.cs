using System;
using System.Linq;
using JNNTool.RebarSuite.Pile.Engine;
using JNNTool.RebarSuite.Pile.Models;
using JNNTool.RebarSuite.Slab.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class PileLayoutEngineTests
    {
        [Fact]
        public void Calculate_BoredPile_GeneratesLongitudinalBarsDenseAndSparseStirrupsAndStiffeners()
        {
            var pile = new PileModel
            {
                Type = PileType.BoredPile,
                DiameterMm = 800,
                LengthMm = 15000,
                TopElevationMm = -1000,
                CenterPoint = new Point3D(0, 0, -8500)
            };

            var settings = new PileRebarSettings
            {
                Type = PileType.BoredPile,
                MainBarCount = 8,
                MainDiameter = "D20",
                DowelExtendLengthMm = 800,
                StirrupDiameter = "D10",
                StirrupSpacingDense = 100,
                StirrupSpacingSparse = 200,
                DenseZoneLengthMm = 2000,
                CreateStiffenerRings = true,
                StiffenerSpacingMm = 2000
            };

            var result = PileLayoutEngine.Calculate(pile, settings);

            Assert.NotNull(result);

            // Kiểm tra 8 thanh thép dọc
            var mainBars = result.RebarSets.Where(r => r.Role == PileRebarRole.MainLongitudinal).ToList();
            Assert.Equal(8, mainBars.Count);

            // Kiểm tra đoạn neo nhô đầu cọc: -1000 + 800 = -200
            double topZ = mainBars[0].CurvePoints[1].Z;
            Assert.Equal(-200.0, topZ, 1);

            // Kiểm tra đai dày đầu cọc
            var stirrupDense = result.RebarSets.FirstOrDefault(r => r.Role == PileRebarRole.StirrupDenseHead);
            Assert.NotNull(stirrupDense);
            Assert.True(stirrupDense.BarCount >= 20);

            // Kiểm tra đai thưa thân cọc
            var stirrupSparse = result.RebarSets.FirstOrDefault(r => r.Role == PileRebarRole.StirrupSparseBody);
            Assert.NotNull(stirrupSparse);

            // Kiểm tra vành gia cường (chiều dài 15m / 2m ~ 6-7 vành)
            var stiffeners = result.RebarSets.Where(r => r.Role == PileRebarRole.StiffenerRing).ToList();
            Assert.True(stiffeners.Count >= 5);
        }

        [Fact]
        public void Calculate_SquarePile_Generates4CornerBarsAndSquareStirrups()
        {
            var pile = new PileModel
            {
                Type = PileType.PrecastSquarePile,
                WidthBMm = 400,
                HeightHMm = 400,
                LengthMm = 8000,
                TopElevationMm = -500,
                CenterPoint = new Point3D(1000, 2000, -4500)
            };

            var settings = new PileRebarSettings
            {
                Type = PileType.PrecastSquarePile,
                MainDiameter = "D18",
                DowelExtendLengthMm = 600,
                StirrupDiameter = "D8",
                StirrupSpacingDense = 100
            };

            var result = PileLayoutEngine.Calculate(pile, settings);

            Assert.NotNull(result);

            var mainBars = result.RebarSets.Where(r => r.Role == PileRebarRole.MainLongitudinal).ToList();
            Assert.Equal(4, mainBars.Count);

            var stirrup = result.RebarSets.FirstOrDefault(r => r.Role == PileRebarRole.StirrupDenseHead);
            Assert.NotNull(stirrup);
            Assert.Equal(5, stirrup.CurvePoints.Count); // Khép kín 4 cạnh + điểm chốt
        }
    }
}
