using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.RebarSuite.Utilities.Engine;
using Xunit;

namespace JNNTool.Tests
{
    public class RebarUtilitiesTests
    {
        [Fact]
        public void RebarVolumeCalculator_CalculatesCorrectWeightByDiameter()
        {
            var rawList = new List<RawRebarInput>
            {
                new RawRebarInput { Diameter = "D20", Quantity = 10, LengthMm = 3000 },
                new RawRebarInput { Diameter = "D10", Quantity = 20, LengthMm = 2000 }
            };

            var result = RebarVolumeCalculator.CalculateByDiameter(rawList);

            Assert.Equal(2, result.Count);

            var d10 = result.FirstOrDefault(r => r.Diameter == "D10");
            Assert.NotNull(d10);
            Assert.Equal(40.0, d10.TotalLengthM);
            Assert.True(Math.Abs(d10.TotalWeightKg - 40.0 * 0.617) < 0.1);

            var d20 = result.FirstOrDefault(r => r.Diameter == "D20");
            Assert.NotNull(d20);
            Assert.Equal(30.0, d20.TotalLengthM);
            Assert.True(Math.Abs(d20.TotalWeightKg - 30.0 * 2.466) < 0.1);

            double totalTon = RebarVolumeCalculator.GetTotalWeightTon(result);
            Assert.True(totalTon > 0.09 && totalTon < 0.11);
        }

        [Fact]
        public void RebarNumberingEngine_AssignsSameMarkForIdenticalBarsAndSortsByDiameter()
        {
            var items = new List<RebarNumberingItem>
            {
                new RebarNumberingItem { ElementId = 1, Diameter = "D20", LengthMm = 4000, ShapeName = "Straight" },
                new RebarNumberingItem { ElementId = 2, Diameter = "D10", LengthMm = 2000, ShapeName = "Straight" },
                new RebarNumberingItem { ElementId = 3, Diameter = "D20", LengthMm = 4005, ShapeName = "Straight" }, // Cùng nhóm D20 L4000
                new RebarNumberingItem { ElementId = 4, Diameter = "D10", LengthMm = 2002, ShapeName = "Straight" }  // Cùng nhóm D10 L2000
            };

            var numbered = RebarNumberingEngine.AssignNumbers(items, startNumber: 1, lengthToleranceMm: 10.0);

            var b1 = numbered.First(x => x.ElementId == 1);
            var b2 = numbered.First(x => x.ElementId == 2);
            var b3 = numbered.First(x => x.ElementId == 3);
            var b4 = numbered.First(x => x.ElementId == 4);

            // D10 được đánh số trước D20
            Assert.Equal(b2.AssignedMark, b4.AssignedMark);
            Assert.Equal(1, b2.AssignedMark);

            // D20 nhận số tiếp theo
            Assert.Equal(b1.AssignedMark, b3.AssignedMark);
            Assert.Equal(2, b1.AssignedMark);
        }
    }
}
