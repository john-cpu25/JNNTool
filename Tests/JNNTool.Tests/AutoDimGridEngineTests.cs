using JNNTool.Tools.AutoDimGrid.Engine;
using Xunit;

namespace JNNTool.Tests
{
    public class AutoDimGridEngineTests
    {
        [Fact]
        public void SortAndDedup_SortsAscending_AndDropsDuplicates()
        {
            var projs = new[] { 30.0, 10.0, 10.0005, 20.0, 30.0 };

            var order = AutoDimGridEngine.SortAndDedup(projs, 0.001);

            Assert.Equal(new[] { 1, 3, 0 }, order);
        }

        [Fact]
        public void SortAndDedup_EmptyInput_ReturnsEmpty()
        {
            Assert.Empty(AutoDimGridEngine.SortAndDedup(new double[0], 0.001));
        }

        [Theory]
        [InlineData(15, 100, 1500)]
        [InlineData(8, 50, 400)]
        [InlineData(10, 0, 10)] // scale lỗi → coi như 1:1
        public void PaperToModelMm_MultipliesByScale(double paper, int scale, double expected)
        {
            Assert.Equal(expected, AutoDimGridEngine.PaperToModelMm(paper, scale), 6);
        }

        [Fact]
        public void LayerPositions_Outward_MaxSide_GoesAwayFromAnchor()
        {
            var pos = AutoDimGridEngine.LayerPositions(1000, 3, 15, 8, +1, insideFromEndpoint: false);

            Assert.Equal(new[] { 1015.0, 1023.0, 1031.0 }, pos);
        }

        [Fact]
        public void LayerPositions_Outward_MinSide_GoesAwayFromAnchor()
        {
            var pos = AutoDimGridEngine.LayerPositions(0, 2, 15, 8, -1, insideFromEndpoint: false);

            Assert.Equal(new[] { -15.0, -23.0 }, pos);
        }

        [Fact]
        public void LayerPositions_InsideFromEndpoint_OutermostIsFirstOffsetInside()
        {
            var max = AutoDimGridEngine.LayerPositions(1000, 3, 15, 8, +1, insideFromEndpoint: true);
            var min = AutoDimGridEngine.LayerPositions(0, 3, 15, 8, -1, insideFromEndpoint: true);

            // Thứ tự trong → ngoài; lớp ngoài cùng cách đầu trục đúng firstOffset.
            Assert.Equal(new[] { 969.0, 977.0, 985.0 }, max);
            Assert.Equal(new[] { 31.0, 23.0, 15.0 }, min);
        }

        [Fact]
        public void LayerPositions_ZeroLayers_ReturnsEmpty()
        {
            Assert.Empty(AutoDimGridEngine.LayerPositions(0, 0, 15, 8, 1, false));
        }
    }
}
