using JNNTool.Tools.CSIxRevit.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class CSIxRevitTests
    {
        [Fact]
        public void SectionDefinition_DimensionSummary_Rectangular()
        {
            var sec = new SectionDefinition
            {
                Name = "B-300x1100",
                Shape = "Concrete Rectangular",
                WidthMm = 300,
                DepthMm = 1100,
                ElementType = "Dầm (Beam)"
            };

            Assert.False(sec.IsCircular);
            Assert.Equal("300×1100 mm", sec.DimensionSummary);
        }

        [Fact]
        public void SectionDefinition_DimensionSummary_Circular()
        {
            var sec = new SectionDefinition
            {
                Name = "C-500-B45",
                Shape = "Concrete Circle",
                WidthMm = 500,
                DepthMm = 500,
                ElementType = "Cột (Column)"
            };

            Assert.True(sec.IsCircular);
            Assert.Equal("Ø500 mm", sec.DimensionSummary);
        }
    }
}
