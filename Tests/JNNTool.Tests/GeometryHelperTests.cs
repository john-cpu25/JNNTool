using JNNTool.RebarSuite.Common;
using Xunit;

namespace JNNTool.Tests
{
    public class GeometryHelperTests
    {
        [Theory]
        [InlineData(100, 50, 100)]
        [InlineData(101, 50, 150)]
        [InlineData(149, 50, 150)]
        [InlineData(150, 50, 150)]
        public void RoundUpToMultiple_WorksAccurately(double val, int multiple, double expected)
        {
            Assert.Equal(expected, GeometryHelper.RoundUpToMultiple(val, multiple));
        }

        [Theory]
        [InlineData(149, 50, 100)]
        [InlineData(150, 50, 150)]
        public void RoundDownToMultiple_WorksAccurately(double val, int multiple, double expected)
        {
            Assert.Equal(expected, GeometryHelper.RoundDownToMultiple(val, multiple));
        }

        [Fact]
        public void CalculateSpacing_ReturnsExpectedCountAndSpacing()
        {
            // Range 2000 mm, cover 50 at both ends -> net = 1900 mm.
            // Target spacing 200 mm -> 1900 / 200 = 9.5 -> 10 intervals -> 11 bars.
            // Actual spacing = 1900 / 10 = 190 mm.
            GeometryHelper.CalculateSpacing(2000, 50, 50, 200, out int count, out double actualSpacing);

            Assert.Equal(11, count);
            Assert.Equal(190.0, actualSpacing);
        }

        [Fact]
        public void UnitConversion_FeetAndMm_RoundtripsAccurately()
        {
            double mm = 1500.0;
            double feet = GeometryHelper.MmToFeet(mm);
            double backToMm = GeometryHelper.FeetToMm(feet);

            Assert.InRange(backToMm, 1499.99, 1500.01);
        }
    }
}
