using JNNTool.RebarSuite.Common;
using Xunit;

namespace JNNTool.Tests
{
    public class BarCatalogTests
    {
        [Theory]
        [InlineData(6, "D6", 28.27, 0.222)]
        [InlineData(10, "D10", 78.54, 0.617)]
        [InlineData(16, "D16", 201.06, 1.578)]
        [InlineData(25, "D25", 490.87, 3.853)]
        public void GetByDiameter_ReturnsCorrectBarInfo(int dia, string expectedName, double minArea, double minWeight)
        {
            var bar = BarCatalog.GetByDiameter(dia);
            Assert.NotNull(bar);
            Assert.Equal(expectedName, bar.Name);
            Assert.Equal(dia, bar.NominalDiameter);
            Assert.InRange(bar.AreaMm2, minArea - 1.0, minArea + 1.0);
            Assert.InRange(bar.WeightKgPerM, minWeight - 0.05, minWeight + 0.05);
        }

        [Theory]
        [InlineData("D10", 10)]
        [InlineData("d16", 16)]
        [InlineData("20", 20)]
        [InlineData("phi 25", 25)]
        [InlineData("T12", 12)]
        public void Parse_ValidInputs_ReturnsExpectedDiameter(string input, int expectedDia)
        {
            var bar = BarCatalog.Parse(input);
            Assert.NotNull(bar);
            Assert.Equal(expectedDia, bar.NominalDiameter);
        }

        [Fact]
        public void Parse_InvalidInput_ReturnsNull()
        {
            Assert.Null(BarCatalog.Parse(""));
            Assert.Null(BarCatalog.Parse("ABC"));
            Assert.Null(BarCatalog.Parse(null));
        }

        [Fact]
        public void GetClosest_ReturnsClosestStandardBar()
        {
            var bar = BarCatalog.GetClosest(11.8);
            Assert.Equal(12, bar.NominalDiameter);

            var bar2 = BarCatalog.GetClosest(24.2);
            Assert.Equal(25, bar2.NominalDiameter);
        }
    }
}
