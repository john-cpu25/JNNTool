using System.Collections.Generic;
using System.Linq;
using JNNTool.Tools.ElementMark.Engine;
using JNNTool.Tools.ElementMark.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class ElementMarkEngineTests
    {
        private static Dictionary<long, string> MarksById(IEnumerable<MarkAssignment> list) =>
            list.ToDictionary(a => a.Id, a => a.Mark);

        // ── Template ─────────────────────────────────────────────

        [Fact]
        public void Template_DefaultBeam_FormatsDS1X01()
        {
            var m = MarkTemplateEngine.Format("{P}{L}{D}{N:00}",
                new MarkTokens { Prefix = "DS", Level = "1", Axis = "X", Number = 1 });
            Assert.Equal("DS1X01", m);
        }

        [Fact]
        public void Template_SpanTokenEmptyWhenNoSpan_AndUnknownTokenKept()
        {
            Assert.Equal("C12", MarkTemplateEngine.Format("{p}{n}{S}", new MarkTokens { Prefix = "C", Number = 12 }));
            Assert.Equal("A{Q}", MarkTemplateEngine.Format("A{Q}", new MarkTokens()));
            Assert.Equal(".03", MarkTemplateEngine.Format(".{S:00}", new MarkTokens { Span = 3 }));
        }

        [Fact]
        public void Template_HasNumberToken()
        {
            Assert.True(MarkTemplateEngine.HasNumberToken("{P}{N:00}"));
            Assert.False(MarkTemplateEngine.HasNumberToken("{P}{L}"));
        }

        // ── Mã tầng ───────────────────────────────────────────────

        [Theory]
        [InlineData("Tầng 1", "1")]
        [InlineData("Level 02", "2")]
        [InlineData("Tầng hầm 1", "B1")]
        [InlineData("B2", "B2")]
        [InlineData("Mái", "M")]
        [InlineData("Roof", "R")]
        [InlineData("Tầng trệt", "TT")]
        public void LevelCode_SuggestFromName(string name, string expected)
        {
            Assert.Equal(expected, LevelCodeResolver.Suggest(name));
        }

        [Fact]
        public void LevelCode_UserOverrideWins()
        {
            var codes = new Dictionary<string, string> { { "Tầng 1", "T1" } };
            Assert.Equal("T1", ElementMarkEngine.ResolveLevelCode(codes, "Tầng 1"));
            Assert.Equal("2", ElementMarkEngine.ResolveLevelCode(codes, "Tầng 2"));
        }

        // ── Phương dầm ────────────────────────────────────────────

        [Theory]
        [InlineData(1000, 0, BeamAxis.X)]
        [InlineData(1000, 900, BeamAxis.X)]   // ~42°
        [InlineData(1000, 1000, BeamAxis.X)]  // đúng 45°
        [InlineData(900, 1000, BeamAxis.Y)]   // ~48°
        [InlineData(0, -1000, BeamAxis.Y)]
        public void Axis_LessOrEqual45IsX(double dx, double dy, BeamAxis expected)
        {
            Assert.Equal(expected, ElementMarkEngine.ClassifyAxis(dx, dy));
        }

        // ── Dầm ──────────────────────────────────────────────────

        /// <summary>
        /// Lưới 2 hàng X (y = 6000 và y = 0), mỗi hàng 2 nhịp nối liền qua cột,
        /// cộng một dầm đơn và 2 dầm Y.
        /// </summary>
        private static List<BeamItem> SampleGrid() => new List<BeamItem>
        {
            new BeamItem(1, "Tầng 1", 0, 6000, 5000, 6000),
            new BeamItem(2, "Tầng 1", 5300, 6000, 10000, 6000), // khe 300 (cột) → liên tục với 1
            new BeamItem(3, "Tầng 1", 0, 0, 5000, 0),
            new BeamItem(4, "Tầng 1", 8000, 0, 10000, 0),       // khe 3000 → dầm khác
            new BeamItem(5, "Tầng 1", 0, 0, 0, 6000),
            new BeamItem(6, "Tầng 1", 10000, 6000, 10000, 0),
        };

        [Fact]
        public void Beams_TopToBottom_LeftToRight_WithContinuousSpans()
        {
            var s = new ElementMarkSettings();
            var marks = MarksById(ElementMarkEngine.MarkBeams(SampleGrid(), s));

            Assert.Equal("DS1X01.1", marks[1]);
            Assert.Equal("DS1X01.2", marks[2]);
            Assert.Equal("DS1X02", marks[3]);
            Assert.Equal("DS1X03", marks[4]);
            Assert.Equal("DS1Y01", marks[5]);
            Assert.Equal("DS1Y02", marks[6]);
        }

        [Fact]
        public void Beams_BottomToTop_RightToLeft()
        {
            var s = new ElementMarkSettings
            {
                BeamVertical = VerticalOrder.BottomToTop,
                BeamHorizontal = HorizontalOrder.RightToLeft
            };
            var marks = MarksById(ElementMarkEngine.MarkBeams(SampleGrid(), s));

            // Hàng dưới trước, trong hàng từ phải sang trái.
            Assert.Equal("DS1X01", marks[4]);
            Assert.Equal("DS1X02", marks[3]);
            // Nhịp cũng đánh từ phải sang trái.
            Assert.Equal("DS1X03.1", marks[2]);
            Assert.Equal("DS1X03.2", marks[1]);
            // Dầm Y: cột dọc bên phải trước.
            Assert.Equal("DS1Y01", marks[6]);
            Assert.Equal("DS1Y02", marks[5]);
        }

        [Fact]
        public void Beams_MergeDisabled_EachSpanOwnNumber()
        {
            var s = new ElementMarkSettings { BeamMergeSpans = false };
            var marks = MarksById(ElementMarkEngine.MarkBeams(SampleGrid(), s));

            Assert.Equal("DS1X01", marks[1]);
            Assert.Equal("DS1X02", marks[2]);
            Assert.Equal("DS1X03", marks[3]);
        }

        [Fact]
        public void Beams_SmallOffsetWithinTolerance_SameRow()
        {
            var beams = new List<BeamItem>
            {
                new BeamItem(1, "L", 0, 0, 5000, 0),
                new BeamItem(2, "L", 5000, 120, 10000, 120), // lệch 120 < 200 → cùng hàng, nối liền
            };
            var marks = MarksById(ElementMarkEngine.MarkBeams(beams, new ElementMarkSettings()));
            Assert.Equal("DSLX01.1", marks[1]);
            Assert.Equal("DSLX01.2", marks[2]);
        }

        [Fact]
        public void Beams_NumberedPerLevelAndAxis()
        {
            var beams = new List<BeamItem>
            {
                new BeamItem(1, "Tầng 1", 0, 0, 5000, 0),
                new BeamItem(2, "Tầng 2", 0, 0, 5000, 0),
            };
            var marks = MarksById(ElementMarkEngine.MarkBeams(beams, new ElementMarkSettings()));
            Assert.Equal("DS1X01", marks[1]);
            Assert.Equal("DS2X01", marks[2]);
        }

        // ── Cột ──────────────────────────────────────────────────

        [Fact]
        public void Columns_SameMarkFromFoundationToRoof()
        {
            var cols = new List<ColumnItem>
            {
                // vị trí A (trái trên) qua 3 tầng, tầng trên lệch tâm 100 do thu tiết diện
                new ColumnItem(1, "Móng", 0, 6000, -1500),
                new ColumnItem(2, "Tầng 1", 0, 6000, 0),
                new ColumnItem(3, "Tầng 2", 100, 6000, 3600),
                // vị trí B (phải trên)
                new ColumnItem(4, "Tầng 1", 8000, 6000, 0),
                // vị trí C (trái dưới)
                new ColumnItem(5, "Tầng 1", 0, 0, 0),
            };
            var marks = MarksById(ElementMarkEngine.MarkColumns(cols, new ElementMarkSettings()));

            Assert.Equal("C1", marks[1]);
            Assert.Equal("C1", marks[2]);
            Assert.Equal("C1", marks[3]);
            Assert.Equal("C2", marks[4]);
            Assert.Equal("C3", marks[5]);
        }

        [Fact]
        public void Columns_ColumnFirst_RightToLeft_BottomToTop()
        {
            var cols = new List<ColumnItem>
            {
                new ColumnItem(1, "L", 0, 6000, 0),
                new ColumnItem(2, "L", 8000, 6000, 0),
                new ColumnItem(3, "L", 0, 0, 0),
                new ColumnItem(4, "L", 8000, 0, 0),
            };
            var s = new ElementMarkSettings
            {
                ColumnPriority = ColumnPriority.ColumnFirst,
                ColumnHorizontal = HorizontalOrder.RightToLeft,
                ColumnVertical = VerticalOrder.BottomToTop
            };
            var marks = MarksById(ElementMarkEngine.MarkColumns(cols, s));

            Assert.Equal("C1", marks[4]);
            Assert.Equal("C2", marks[2]);
            Assert.Equal("C3", marks[3]);
            Assert.Equal("C4", marks[1]);
        }

        [Fact]
        public void Columns_FarApart_NotStacked()
        {
            var cols = new List<ColumnItem>
            {
                new ColumnItem(1, "Tầng 1", 0, 0, 0),
                new ColumnItem(2, "Tầng 2", 500, 0, 3600), // lệch 500 > 300
            };
            var marks = MarksById(ElementMarkEngine.MarkColumns(cols, new ElementMarkSettings()));
            Assert.NotEqual(marks[1], marks[2]);
        }

        // ── Sàn ──────────────────────────────────────────────────

        [Fact]
        public void Floors_SameMarkPerLevel()
        {
            var floors = new List<FloorItem>
            {
                new FloorItem(1, "Tầng 1"),
                new FloorItem(2, "Tầng 1"),
                new FloorItem(3, "Tầng 2"),
                new FloorItem(4, "Tầng hầm 1"),
            };
            var marks = MarksById(ElementMarkEngine.MarkFloors(floors, new ElementMarkSettings()));

            Assert.Equal("S1", marks[1]);
            Assert.Equal("S1", marks[2]);
            Assert.Equal("S2", marks[3]);
            Assert.Equal("SB1", marks[4]);
        }

        // ── Gom hàng ──────────────────────────────────────────────

        [Fact]
        public void Cluster_UsesMeanSoChainOfSmallStepsDoesNotDrift()
        {
            var values = new[] { 0.0, 150, 300, 450, 600 };
            var clusters = ElementMarkEngine.Cluster(values, v => v, 200, false);
            Assert.True(clusters.Count >= 2);
        }
    }
}
