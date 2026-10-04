using JNNTool.RebarSuite.Common;
using Xunit;

namespace JNNTool.Tests
{
    public class AnchorAndLapTests
    {
        [Fact]
        public void AnchorCalculator_B25_CB400V_D10_ComputesValidAnchor()
        {
            // D10, B25, CB400_V
            // Rs = 350, Rbt = 1.05, eta1 = 2.5, eta2 = 1.0 -> Rbond = 2.625
            // l0_an = (350 * 10) / (4 * 2.625) = 333.33 mm
            // Min length = max(15*10, 200) = 200 mm
            // Rounded to 50mm -> 350 mm
            double len = AnchorCalculator.CalculateAnchorLengthMm(10, ConcreteGrade.B25, RebarSteelGrade.CB400_V);
            Assert.Equal(350.0, len);
        }

        [Fact]
        public void AnchorCalculator_Multiplier_WorksCorrectly()
        {
            // 35 * 10 = 350 mm -> round 50 -> 350 mm
            double len = AnchorCalculator.CalculateByMultiplier(10, 35.0);
            Assert.Equal(350.0, len);

            // 35 * 12 = 420 mm -> round 50 -> 450 mm
            double len12 = AnchorCalculator.CalculateByMultiplier(12, 35.0);
            Assert.Equal(450.0, len12);
        }

        [Fact]
        public void LapCalculator_Ratio50_ComputesValidLap()
        {
            // D10, B25, CB400_V, Ratio50 -> alpha_l = 1.4
            // l0_an = 333.33 mm
            // ll = 1.4 * 333.33 = 466.67 mm
            // Min = max(20*10, 250, 0.4*1.4*333.33) = 250 mm
            // Rounded to 50 -> 500 mm
            double lap = LapCalculator.CalculateLapLengthMm(10, ConcreteGrade.B25, RebarSteelGrade.CB400_V, LapSpliceRatio.Ratio50OrLess);
            Assert.Equal(500.0, lap);
        }

        [Fact]
        public void HookAndCover_StandardValues_AreCorrect()
        {
            // 90 deg hook for D10: max(10 * 10, 100) = 100
            Assert.Equal(100.0, HookHelper.GetHookExtensionLengthMm(10, HookAngle.Deg90));

            // 135 deg stirrup hook for D8: max(10 * 8, 75) = 80
            Assert.Equal(80.0, HookHelper.GetHookExtensionLengthMm(8, HookAngle.Deg135, isStirrup: true));

            // Slab cover: indoor dry, thickness 120mm -> 15mm
            double slabCover = CoverHelper.GetMinCoverMm(StructuralElementType.Slab, ExposureEnvironment.IndoorDry, 120);
            Assert.Equal(15.0, slabCover);

            // Validate cover: D20 bar on 15mm cover -> must be at least 20mm
            Assert.Equal(20.0, CoverHelper.ValidateCover(15.0, 20));
        }
    }
}
