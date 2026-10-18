using System;
using System.Collections.Generic;
using JNNTool.Tools.CutByFloor.Engine;
using JNNTool.Tools.CutByFloor.Models;
using Xunit;

namespace JNNTool.Tests
{
    public class CutByFloorTests
    {
        [Fact]
        public void SquareHoleInsideLargeSquare_CreatesHoleLoop()
        {
            // Sàn to: 10 x 10 m (tương đương 0,0 đến 10,10)
            var target = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(0, 0),
                    new Pt2D(10, 0),
                    new Pt2D(10, 10),
                    new Pt2D(0, 10)
                }
            };

            // Sàn nhỏ nằm trong sàn to: 4 x 4 m (3,3 đến 7,7)
            var cutter = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(3, 3),
                    new Pt2D(7, 3),
                    new Pt2D(7, 7),
                    new Pt2D(3, 7)
                }
            };

            var islands = FloorDifferenceEngine.Difference(target, new List<FloorPolyRegion> { cutter });

            Assert.NotNull(islands);
            Assert.Single(islands); // 1 đảo sàn duy nhất

            var island = islands[0];
            Assert.Equal(4, island.OuterLoop.Count);
            Assert.True(FloorDifferenceEngine.ComputeSignedArea(island.OuterLoop) > 0); // CCW
            Assert.Equal(100.0, FloorDifferenceEngine.ComputeSignedArea(island.OuterLoop), 2);

            Assert.Single(island.HoleLoops); // Có đúng 1 lỗ khoét
            var hole = island.HoleLoops[0];
            Assert.Equal(4, hole.Count);
            Assert.True(FloorDifferenceEngine.ComputeSignedArea(hole) < 0); // CW (Lỗ khoét theo chuẩn Revit)
            Assert.Equal(-16.0, FloorDifferenceEngine.ComputeSignedArea(hole), 2);
        }

        [Fact]
        public void EdgeNotchCut_ModifiesOuterBoundary()
        {
            // Sàn to 10 x 10
            var target = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(0, 0),
                    new Pt2D(10, 0),
                    new Pt2D(10, 10),
                    new Pt2D(0, 10)
                }
            };

            // Sàn nhỏ cắt lẹm cạnh trái: rộng 3, cao 4 (từ Y=2 đến Y=6)
            var cutter = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(-1, 2),
                    new Pt2D(3, 2),
                    new Pt2D(3, 6),
                    new Pt2D(-1, 6)
                }
            };

            var islands = FloorDifferenceEngine.Difference(target, new List<FloorPolyRegion> { cutter });

            Assert.NotNull(islands);
            Assert.Single(islands);

            var island = islands[0];
            Assert.Empty(island.HoleLoops); // Không có lỗ khoét vì cắt từ mép ngoài
            double area = FloorDifferenceEngine.ComputeSignedArea(island.OuterLoop);
            // Diện tích còn lại: 100 - (3 * 4) = 88
            Assert.Equal(88.0, area, 1);
        }

        [Fact]
        public void MultipleCuttersInside_CreatesMultipleHoles()
        {
            // Sàn to 20 x 20 (Diện tích 400)
            var target = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(0, 0),
                    new Pt2D(20, 0),
                    new Pt2D(20, 20),
                    new Pt2D(0, 20)
                }
            };

            // Ô 1: 4 x 4 = 16
            var c1 = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(2, 2),
                    new Pt2D(6, 2),
                    new Pt2D(6, 6),
                    new Pt2D(2, 6)
                }
            };

            // Ô 2: 5 x 5 = 25
            var c2 = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(10, 10),
                    new Pt2D(15, 10),
                    new Pt2D(15, 15),
                    new Pt2D(10, 15)
                }
            };

            var islands = FloorDifferenceEngine.Difference(target, new List<FloorPolyRegion> { c1, c2 });

            Assert.Single(islands);
            Assert.Equal(2, islands[0].HoleLoops.Count); // Có đúng 2 lỗ khoét
        }

        [Fact]
        public void SliceAcross_SplitsIntoTwoIslands()
        {
            // Sàn to 10 x 10
            var target = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(0, 0),
                    new Pt2D(10, 0),
                    new Pt2D(10, 10),
                    new Pt2D(0, 10)
                }
            };

            // Dao cắt chạy dọc từ dưới lên trên cắt đôi sàn (từ X=4 đến X=6)
            var cutter = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(4, -2),
                    new Pt2D(6, -2),
                    new Pt2D(6, 12),
                    new Pt2D(4, 12)
                }
            };

            var islands = FloorDifferenceEngine.Difference(target, new List<FloorPolyRegion> { cutter });

            Assert.Equal(2, islands.Count); // Tách thành 2 mảng sàn riêng biệt
            Assert.Equal(40.0, islands[0].Area, 1);
            Assert.Equal(40.0, islands[1].Area, 1);
        }

        [Fact]
        public void NoOverlap_ReturnsOriginalFloor()
        {
            var target = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(0, 0),
                    new Pt2D(10, 0),
                    new Pt2D(10, 10),
                    new Pt2D(0, 10)
                }
            };

            var cutter = new FloorPolyRegion
            {
                Outer = new List<Pt2D>
                {
                    new Pt2D(50, 50),
                    new Pt2D(60, 50),
                    new Pt2D(60, 60),
                    new Pt2D(50, 60)
                }
            };

            var islands = FloorDifferenceEngine.Difference(target, new List<FloorPolyRegion> { cutter });

            Assert.Single(islands);
            Assert.Equal(100.0, islands[0].Area, 1);
            Assert.Empty(islands[0].HoleLoops);
        }
    }
}
