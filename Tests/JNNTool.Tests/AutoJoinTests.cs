using System;
using JNNTool.Tools.AutoJoin;
using Xunit;

namespace JNNTool.Tests
{
    public class AutoJoinTests
    {
        [Fact]
        public void AutoJoinSummary_TotalProcessedPairs_CalculatesCorrectly()
        {
            var summary = new AutoJoinSummary
            {
                JoinedCount = 12,
                SwitchedCount = 5,
                AlreadyCorrectCount = 30,
                SkippedCount = 8,
                ElapsedTime = TimeSpan.FromSeconds(1.5)
            };

            Assert.Equal(47, summary.TotalProcessedPairs);
            Assert.Equal(12, summary.JoinedCount);
            Assert.Equal(5, summary.SwitchedCount);
            Assert.Equal(30, summary.AlreadyCorrectCount);
        }

        [Fact]
        public void AutoJoinScope_EnumValues_AreDefined()
        {
            Assert.Equal(0, (int)AutoJoinScope.ActiveView);
            Assert.Equal(1, (int)AutoJoinScope.EntireProject);
        }
    }
}
