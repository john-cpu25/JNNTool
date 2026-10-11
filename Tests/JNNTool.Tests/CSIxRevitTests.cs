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

        [Theory]
        [InlineData("F600", "ShellThick", 600, SlabKind.Foundation)]
        [InlineData("F900", "ShellThick", 900, SlabKind.Foundation)]
        [InlineData("f700", "", 700, SlabKind.Foundation)]          // Tên F + số → móng dù thiếu MODELINGTYPE
        [InlineData("RAFT1", "ShellThick", 1500, SlabKind.Foundation)] // ShellThick → móng
        [InlineData("S200", "ShellThin", 200, SlabKind.Slab)]
        [InlineData("S100", "ShellThin", 100, SlabKind.Slab)]
        [InlineData("CT120", "Membrane", 120, SlabKind.Slab)]       // Membrane dày thật vẫn là sàn S
        [InlineData("SS1", "Membrane", 1, SlabKind.Virtual)]        // 0.001 m → 1 mm
        [InlineData("S1", "Membrane", 0.1, SlabKind.Virtual)]       // 0.0001 m → 0.1 mm
        [InlineData("FLOOR200", "ShellThin", 200, SlabKind.Slab)]   // "FL..." không phải F + số
        [InlineData("Slab1", "ShellThin", 0, SlabKind.Slab)]        // Chưa rõ dày → không coi là sàn ảo
        public void SlabClassifier_Classify(string name, string modelingType, double thicknessMm, SlabKind expected)
        {
            Assert.Equal(expected, SlabClassifier.Classify(name, modelingType, thicknessMm));
        }

        [Fact]
        public void SlabClassifier_ToElementKind()
        {
            Assert.Equal(ElementKinds.Slab, SlabClassifier.ToElementKind(SlabKind.Slab));
            Assert.Equal(ElementKinds.FoundationSlab, SlabClassifier.ToElementKind(SlabKind.Foundation));
            Assert.Equal(ElementKinds.VirtualSlab, SlabClassifier.ToElementKind(SlabKind.Virtual));
        }

        [Theory]
        [InlineData(100, 100.0, true)]   // S100 ↔ JNN_SanBT_100 dày 100
        [InlineData(200, 200.3, true)]   // sai số đổi đơn vị
        [InlineData(100, 120, false)]
        [InlineData(100, 0, false)]      // không đọc được chiều dày
        public void SectionSizeMatcher_Thickness(double want, double actual, bool expected)
        {
            Assert.Equal(expected, SectionSizeMatcher.ThicknessMatches(want, actual));
        }

        [Theory]
        [InlineData(200, 500, 200, 500, false, true)]
        [InlineData(200, 500, 500, 200, false, false)] // dầm: không cho đảo b/h
        [InlineData(200, 500, 500, 200, true, true)]   // cột: cho đảo b/h
        [InlineData(200, 500, 200, 600, true, false)]
        public void SectionSizeMatcher_Rect(double wantB, double wantH, double b, double h, bool allowSwap, bool expected)
        {
            Assert.Equal(expected, SectionSizeMatcher.RectMatches(wantB, wantH, b, h, allowSwap));
        }

        [Fact]
        public void Parse_NP_ChiHoa_SlabKinds()
        {
            string path = @"C:\Users\nhann\OneDrive\AI\JNNTool\Tools\CSI x Revit\NP CHI HOA-TAN SON-GV_PA MONG COC_PA4 (GIO FULL).e2k";
            if (!System.IO.File.Exists(path)) return;

            var tables = new JNNTool.Tools.CSIxRevit.Parser.E2KReader().Read(path);
            new JNNTool.Tools.CSIxRevit.Parser.E2KParser().Parse(tables, out _, out _, out _, out _, out _, out _, out _, out _, out var defs);

            Assert.Equal(ElementKinds.FoundationSlab, defs["F600"].ElementType);
            Assert.Equal("ShellThick", defs["F600"].ModelingType);
            Assert.Equal(600, defs["F600"].DepthMm, 1);
            Assert.Equal(ElementKinds.Slab, defs["S200"].ElementType);
            Assert.Equal(200, defs["S200"].DepthMm, 1);
            Assert.Equal(ElementKinds.Slab, defs["CT120"].ElementType);
            Assert.Equal(ElementKinds.VirtualSlab, defs["SS1"].ElementType);
            Assert.Equal(ElementKinds.Wall, defs["W250"].ElementType);
            Assert.Equal(250, defs["W250"].DepthMm, 1);
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

        [Theory]
        // Cột 200x250 (b=200, h=250), góc 0:
        // Phương Y -> kích thước là h = 250 -> dời 125 mm (H/2)
        // Phương X -> kích thước là b = 200 -> dời 100 mm (B/2)
        [InlineData(200, 250, 0, 0, 1, 0, 125.0)]   // UpY (+Y): dời 125 mm (H/2)
        [InlineData(200, 250, 0, 0, -1, 0, 125.0)]  // DownY (-Y): dời 125 mm (H/2)
        [InlineData(200, 250, 0, 1, 0, 0, 100.0)]   // RightX (+X): dời 100 mm (B/2)
        [InlineData(200, 250, 0, -1, 0, 0, 100.0)]  // LeftX (-X): dời 100 mm (B/2)

        // Cột 200x250 (b=200, h=250), xoay 90 độ:
        // Cạnh b quay sang trục Y (200), cạnh h quay sang trục X (250)
        [InlineData(200, 250, 90, 0, 1, 0, 100.0)]  // UpY (+Y): dời 100 mm (B/2)
        [InlineData(200, 250, 90, 1, 0, 0, 125.0)]  // RightX (+X): dời 125 mm (H/2)

        // Cột 250x500 (b=250, h=500), góc 0:
        [InlineData(250, 500, 0, 0, 1, 0, 250.0)]   // UpY: 500 / 2 = 250 mm (H/2)
        [InlineData(250, 500, 0, 1, 0, 0, 125.0)]   // RightX: 250 / 2 = 125 mm (B/2)

        // Cột vuông 200x200:
        [InlineData(200, 200, 0, 0, 1, 0, 100.0)]
        [InlineData(200, 200, 90, 0, 1, 0, 100.0)]

        // Custom offset override:
        [InlineData(200, 250, 0, 0, 1, 80.0, 80.0)] // Custom 80mm
        public void ColumnDimensionHelper_CalculateShiftDistance(
            double bMm, double hMm, double rotDeg, double dirX, double dirY, double customMm, double expectedShiftMm)
        {
            double actual = JNNTool.Tools.CSIxRevit.Builder.ColumnDimensionHelper.CalculateShiftDistanceMm(
                bMm, hMm, rotDeg, dirX, dirY, customMm);
            Assert.Equal(expectedShiftMm, actual, 1);
        }

        [Theory]
        [InlineData("C50X20", ElementKinds.Column, 200, 500, false, "C200x500")]
        [InlineData("B30X25", ElementKinds.Beam, 300, 250, false, "B300x250")]
        [InlineData("DC-30x60", ElementKinds.Beam, 300, 600, false, "DC300x600")]
        [InlineData("C-D50", ElementKinds.Column, 0, 500, true, "CD500")]
        [InlineData("S20", ElementKinds.Slab, 0, 200, false, "S200")]
        [InlineData("F700", ElementKinds.FoundationSlab, 0, 700, false, "F700")]
        [InlineData("W25", ElementKinds.Wall, 0, 250, false, "W250")]
        [InlineData("50X20", ElementKinds.Column, 200, 500, false, "C200x500")]
        [InlineData("C50X20", ElementKinds.Column, 0, 0, false, "C50X20")] // thiếu kích thước → giữ tên ETABS
        public void TypeNameSuggester_Suggest(string etabs, string kind, double w, double d, bool circ, string expected)
        {
            Assert.Equal(expected, TypeNameSuggester.Suggest(etabs, kind, w, d, circ));
        }

        [Fact]
        public void ScopeBoxPlanner_BigBox_MovesToCenterAndFits()
        {
            // Công trình 0..20000 × 0..10000, cao 0..12000; hộp mẫu 40 m × 40 m × 30 m đặt lệch.
            var box = new BoxMm(100000, 100000, 0, 140000, 140000, 30000);
            var r = ScopeBoxPlanner.Plan(new double[] { 0, 20000 }, new double[] { 0, 10000 }, 0, 12000, box, 2000);

            Assert.True(r.HasBuilding);
            Assert.Equal(10000 - 120000, r.MoveX, 3);
            Assert.Equal(5000 - 120000, r.MoveY, 3);
            Assert.Equal(6000 - 15000, r.MoveZ, 3);
            Assert.True(r.FitsPlan);
            Assert.True(r.FitsHeight);
            Assert.Equal(24000, r.Required.SizeX, 3);
        }

        [Fact]
        public void ScopeBoxPlanner_SmallBox_ReportsShortfall()
        {
            var box = new BoxMm(0, 0, 0, 10000, 10000, 5000);
            var r = ScopeBoxPlanner.Plan(new double[] { 0, 20000 }, new double[] { 0, 8000 }, 0, 12000, box, 1000);

            Assert.False(r.FitsPlan);
            Assert.Equal(12000, r.ShortX, 3); // cần 22000
            Assert.Equal(0, r.ShortY, 3);     // cần 10000
            Assert.False(r.FitsHeight);
            Assert.Equal(9000, r.ShortZ, 3);  // cần 14000
        }

        [Fact]
        public void ScopeBoxPlanner_NoPoints_NoBuilding()
        {
            var r = ScopeBoxPlanner.Plan(new double[0], new double[0], 0, 0, new BoxMm(0, 0, 0, 1, 1, 1), 1000);
            Assert.False(r.HasBuilding);
        }
    }
}
