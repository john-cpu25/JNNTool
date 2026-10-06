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

        [Fact]
        public void Parse_TypicalE2K_Grids()
        {
            string path = @"C:\Users\nhann\OneDrive\AI\JNNTool\Tools\CSI x Revit\Typical.e2k";
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Tools\CSI x Revit\Typical.e2k"));
            }
            if (!System.IO.File.Exists(path)) return;

            var reader = new JNNTool.Tools.CSIxRevit.Parser.E2KReader();
            var tables = reader.Read(path);
            var parser = new JNNTool.Tools.CSIxRevit.Parser.E2KParser();
            parser.Parse(tables, out var pts, out var bms, out var cols, out var wls, out var fls, out var props, out var stories, out var grids, out var defs);

            Assert.True(grids.Count > 0, $"Grids count: {grids.Count}");
            Assert.True(stories.Count > 0, $"Stories count: {stories.Count}");
            Assert.True(bms.Count > 0, $"Beams count: {bms.Count}");
            Assert.True(wls.Count > 0, $"Walls count: {wls.Count}");
        }

        [Fact]
        public void Parse_BonNuocMai_ETABS()
        {
            string path = @"C:\Users\nhann\OneDrive\AI\JNNTool\Tools\CSI x Revit\NHA LAM VIEC_26.04.17_BONNUOCMAI.$et";
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Tools\CSI x Revit\NHA LAM VIEC_26.04.17_BONNUOCMAI.$et"));
            }
            if (!System.IO.File.Exists(path)) return;

            var reader = new JNNTool.Tools.CSIxRevit.Parser.E2KReader();
            var tables = reader.Read(path);
            var parser = new JNNTool.Tools.CSIxRevit.Parser.E2KParser();
            parser.Parse(tables, out var pts, out var bms, out var cols, out var wls, out var fls, out var props, out var stories, out var grids, out var defs);

            Assert.True(grids.Count > 0, $"Grids count: {grids.Count}");
            Assert.True(stories.Count > 0, $"Stories count: {stories.Count}");
            Assert.True(cols.Count > 0, $"Columns count: {cols.Count}");
        }

        [Fact]
        public void Parse_NP_ChiHoa_ETABS()
        {
            string path = @"C:\Users\nhann\OneDrive\AI\JNNTool\Tools\CSI x Revit\NP CHI HOA-TAN SON-GV_PA MONG COC_PA4 (GIO FULL).e2k";
            if (!System.IO.File.Exists(path))
            {
                path = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\..\Tools\CSI x Revit\NP CHI HOA-TAN SON-GV_PA MONG COC_PA4 (GIO FULL).e2k"));
            }
            if (!System.IO.File.Exists(path)) return;

            var reader = new JNNTool.Tools.CSIxRevit.Parser.E2KReader();
            var tables = reader.Read(path);
            var parser = new JNNTool.Tools.CSIxRevit.Parser.E2KParser();
            parser.Parse(tables, out var pts, out var bms, out var cols, out var wls, out var fls, out var props, out var stories, out var grids, out var defs);

            Assert.Equal(10, stories.Count);
            Assert.Equal(10, grids.Count);
            Assert.Equal(150, cols.Count);
            Assert.Equal(352, bms.Count);
            Assert.Equal(187, fls.Count);
        }

        [Theory]
        [InlineData(500, 200, 0, 200, 500, 90.0)]  // C50X20 perimeter col: Rotate 90 to align 500 with X (Fig 2)
        [InlineData(600, 200, 0, 200, 600, 90.0)]  // C60X20 perimeter col: Rotate 90 to align 600 with X (Fig 2)
        [InlineData(200, 300, 0, 200, 300, 0.0)]   // C20X30 gable col: Keep 0, 300 along Y
        [InlineData(150, 400, 0, 150, 400, 0.0)]   // C15X40 elevator shaft col: Keep 0, 400 along Y
        [InlineData(150, 300, 90, 150, 300, 90.0)] // C8 col with ETABS ANG 90: Rotate 90
        [InlineData(200, 200, 0, 200, 200, 0.0)]   // C20X20 square col: Keep 0
        [InlineData(500, 200, 0, 500, 200, 0.0)]   // Reversed Family (b=500, h=200): Already at 0, no rotate
        public void ColumnRotationHelper_AccurateOrientation(
            double etabsD, double etabsW, double etabsAngle, double revitB, double revitH, double expectedAngle)
        {
            double actual = JNNTool.Tools.CSIxRevit.Builder.ColumnRotationHelper.CalculateRotationAngle(
                etabsD, etabsW, etabsAngle, revitB, revitH);
            Assert.Equal(expectedAngle, actual, 1);
        }
    }
}
