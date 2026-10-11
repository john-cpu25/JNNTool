using System;
using System.Collections.Generic;
using JNNTool.Tools.AutoDimColumn.Engine;
using Xunit;

namespace JNNTool.Tests
{
    public class AutoDimColumnEngineTests
    {
        [Fact]
        public void UnitConversion_WorksCorrectly()
        {
            double mm = 304.8;
            double ft = AutoDimColumnEngine.MmToFt(mm);
            Assert.Equal(1.0, ft, 5);

            double backMm = AutoDimColumnEngine.FtToMm(ft);
            Assert.Equal(304.8, backMm, 5);
        }

        [Fact]
        public void Normalize2D_ReturnsUnitVector()
        {
            var (nx, ny) = AutoDimColumnEngine.Normalize2D(3.0, 4.0);
            Assert.Equal(0.6, nx, 5);
            Assert.Equal(0.8, ny, 5);

            double length = Math.Sqrt((nx * nx) + (ny * ny));
            Assert.Equal(1.0, length, 5);
        }

        [Fact]
        public void AngleBetweenDeg_CalculatesCorrectAngle()
        {
            // Trục X và Trục Y vuông góc 90 độ
            double angle90 = AutoDimColumnEngine.AngleBetweenDeg(1.0, 0.0, 0.0, 1.0);
            Assert.Equal(90.0, angle90, 4);

            // Hai vector cùng hướng: 0 độ
            double angle0 = AutoDimColumnEngine.AngleBetweenDeg(1.0, 0.0, 1.0, 0.0);
            Assert.Equal(0.0, angle0, 4);

            // Hai vector ngược hướng: 180 độ
            double angle180 = AutoDimColumnEngine.AngleBetweenDeg(1.0, 0.0, -1.0, 0.0);
            Assert.Equal(180.0, angle180, 4);
        }

        [Theory]
        [InlineData(1, 0, 1, 0, true)]       // Cùng hướng
        [InlineData(1, 0, -1, 0, true)]      // Ngược hướng (đối song song)
        [InlineData(1, 0, 0, 1, false)]      // Vuông góc
        [InlineData(1, 0, 0.999, 0.01, true)]// Lệch rất nhỏ (< 5 độ)
        public void IsParallel_DetectsParallelVectors(double ax, double ay, double bx, double by, bool expected)
        {
            bool parallel = AutoDimColumnEngine.IsParallel(ax, ay, bx, by, toleranceDeg: 5.0);
            Assert.Equal(expected, parallel);
        }

        [Fact]
        public void PerpendicularDistance_ComputesCorrectDistanceToLine()
        {
            // Điểm (10, 5), Đường thẳng đi qua (0, 0) dọc theo trục X (1, 0)
            // Khoảng cách vuông góc tới trục X phải là 5
            double dist = AutoDimColumnEngine.PerpendicularDistance(10.0, 5.0, 0.0, 0.0, 1.0, 0.0);
            Assert.Equal(5.0, dist, 5);

            // Đường thẳng đi qua (2, 0) dọc theo trục Y (0, 1), điểm (5, 10)
            // Khoảng cách vuông góc tới trục Y x=2 phải là 3
            double dist2 = AutoDimColumnEngine.PerpendicularDistance(5.0, 10.0, 2.0, 0.0, 0.0, 1.0);
            Assert.Equal(3.0, dist2, 5);
        }

        [Fact]
        public void CalculateDimLine2D_GeneratesExpectedLineCoordinates()
        {
            // Cột đặt tại (0,0), đo theo trục X (1,0), offset theo trục Y (0,1) khoảng cách 2 ft
            // Extent từ -1 đến +1 ft, margin 0.5 ft
            var (sx, sy, ex, ey) = AutoDimColumnEngine.CalculateDimLine2D(
                centerX: 0, centerY: 0,
                measureDirX: 1, measureDirY: 0,
                offsetDirX: 0, offsetDirY: 1,
                offsetDist: 2.0,
                minExtent: -1.0, maxExtent: 1.0,
                margin: 0.5
            );

            // Điểm neo trên đường dim: (0, 2)
            // Start point: anchor + (1,0) * (-1 - 0.5) = (-1.5, 2.0)
            // End point: anchor + (1,0) * (1 + 0.5) = (1.5, 2.0)
            Assert.Equal(-1.5, sx, 4);
            Assert.Equal(2.0, sy, 4);
            Assert.Equal(1.5, ex, 4);
            Assert.Equal(2.0, ey, 4);
        }

        [Fact]
        public void RankGridCandidates_SelectsClosestParallelGrid()
        {
            var rawGrids = new List<(string name, double distMm, double angleDiffDeg)>
            {
                ("Grid A", 800.0, 0.2),    // Rất song song, cách 800mm -> Rất tốt
                ("Grid B", 3500.0, 0.1),   // Quá xa (vượt searchRange 2500mm) -> Bỏ qua
                ("Grid C", 400.0, 45.0),   // Gần nhưng xiên 45 độ (không song song) -> Bỏ qua
                ("Grid D", 1200.0, 179.8)  // Ngược hướng (đối song song), cách 1200mm -> Hợp lệ
            };

            var ranked = AutoDimColumnEngine.RankGridCandidates(rawGrids, searchRangeMm: 2500.0, angleTolDeg: 5.0);

            Assert.Equal(2, ranked.Count);
            Assert.Equal("Grid A", ranked[0].Name);
            Assert.Equal("Grid D", ranked[1].Name);
        }

        [Fact]
        public void BuildDimSignature_ConsistentIndependentOfOrder()
        {
            var refs1 = new[] { "ref_face_A", "ref_face_B" };
            var refs2 = new[] { "ref_face_B", "ref_face_A" };

            string sig1 = AutoDimColumnEngine.BuildDimSignature(refs1, 1);
            string sig2 = AutoDimColumnEngine.BuildDimSignature(refs2, 1);

            Assert.Equal(sig1, sig2);
        }
    }
}
