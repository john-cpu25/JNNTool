using System;
using System.Linq;
using JNNTool.Tools.CreateView.Models;
using JNNTool.Tools.CreateView.Services;
using Xunit;

namespace JNNTool.Tests
{
    public class CreateViewTests
    {
        [Fact]
        public void ViewRuleItem_ResolveName_ShouldReplaceLevelTokenCorrectly()
        {
            var rule1 = new ViewRuleItem { ViewNamePattern = "XR-COLUMN {Level}" };
            Assert.Equal("XR-COLUMN TANG 1", rule1.ResolveName("TANG 1"));

            var rule2 = new ViewRuleItem { ViewNamePattern = "XR-{LEVEL}" };
            Assert.Equal("XR-ROOF", rule2.ResolveName("ROOF"));

            var rule3 = new ViewRuleItem { ViewNamePattern = "XR-{level}" };
            Assert.Equal("XR-TANG HAM", rule3.ResolveName("TANG HAM"));

            // Static name without token should remain unchanged
            var ruleStatic = new ViewRuleItem { ViewNamePattern = "XR-GRID" };
            Assert.Equal("XR-GRID", ruleStatic.ResolveName("TANG 1"));

            var ruleStaticPile = new ViewRuleItem { ViewNamePattern = "XR-PILE" };
            Assert.Equal("XR-PILE", ruleStaticPile.ResolveName("TANG 1"));

            var ruleStaticCap = new ViewRuleItem { ViewNamePattern = "XR-PILECAP" };
            Assert.Equal("XR-PILECAP", ruleStaticCap.ResolveName("TANG 1"));
        }

        [Fact]
        public void CreateViewPresetService_DefaultPreset_ShouldContain5ViewTypeTabs()
        {
            var defaultPreset = CreateViewPresetService.GetDefaultAthenaPreset();
            Assert.NotNull(defaultPreset);
            Assert.Equal(5, defaultPreset.Tabs.Count);

            // Tab 1: XR-GRID (1 view Móng)
            var tabGrid = defaultPreset.Tabs.FirstOrDefault(t => t.Title == "XR-GRID");
            Assert.NotNull(tabGrid);
            Assert.Equal("XR-GRID", tabGrid.ViewFamilyTypeName);
            Assert.Equal("ATHENA_GRID", tabGrid.DefaultTemplateName);
            Assert.Equal("SingleLevel", tabGrid.LevelScope);
            Assert.Contains(tabGrid.Views, v => v.ViewNamePattern == "XR-GRID");

            // Tab 2: XR-COLUMN (Đủ các tầng)
            var tabCol = defaultPreset.Tabs.FirstOrDefault(t => t.Title == "XR-COLUMN");
            Assert.NotNull(tabCol);
            Assert.Equal("XR-COLUMN", tabCol.ViewFamilyTypeName);
            Assert.Equal("ATHENA_COLUMN", tabCol.DefaultTemplateName);
            Assert.Equal("AllSelectedLevels", tabCol.LevelScope);
            Assert.Contains(tabCol.Views, v => v.ViewNamePattern == "XR-COLUMN {Level}");

            // Tab 3: XR-FOUNDATION (1 view Móng)
            var tabFnd = defaultPreset.Tabs.FirstOrDefault(t => t.Title == "XR-FOUNDATION");
            Assert.NotNull(tabFnd);
            Assert.Equal("XR-FOUNDATION", tabFnd.ViewFamilyTypeName);
            Assert.Equal("ATHENA_MẶT BẰNG MÓNG", tabFnd.DefaultTemplateName);
            Assert.Equal("SingleLevel", tabFnd.LevelScope);
            Assert.Contains(tabFnd.Views, v => v.ViewNamePattern == "XR-PILE");
            Assert.Contains(tabFnd.Views, v => v.ViewNamePattern == "XR-PILECAP");

            // Tab 4: XR-MBKC (Đủ các tầng)
            var tabMbkc = defaultPreset.Tabs.FirstOrDefault(t => t.Title == "XR-MBKC");
            Assert.NotNull(tabMbkc);
            Assert.Equal("XR-MBKC", tabMbkc.ViewFamilyTypeName);
            Assert.Equal("ATHENA_MBKC", tabMbkc.DefaultTemplateName);
            Assert.Equal("AllSelectedLevels", tabMbkc.LevelScope);
            Assert.Contains(tabMbkc.Views, v => v.ViewNamePattern == "XR-BE NUOC");
            Assert.Contains(tabMbkc.Views, v => v.ViewNamePattern == "XR-{Level}");

            // Tab 5: XR-STAIR PLAN (1 view Móng)
            var tabStair = defaultPreset.Tabs.FirstOrDefault(t => t.Title == "XR-STAIR PLAN");
            Assert.NotNull(tabStair);
            Assert.Equal("XR-STAIR PLAN", tabStair.ViewFamilyTypeName);
            Assert.Equal("ATHENA_MBKC", tabStair.DefaultTemplateName);
            Assert.Equal("SingleLevel", tabStair.LevelScope);
            Assert.Contains(tabStair.Views, v => v.ViewNamePattern == "XR-STAIR {Level}");
        }

        [Fact]
        public void ViewTypeTabItem_Operations_ShouldWorkCorrectly()
        {
            var tab = new ViewTypeTabItem
            {
                Title = "XR-COLUMN",
                ViewFamilyTypeName = "XR-COLUMN",
                BrowserSubDiscipline = "XR-COLUMN"
            };

            tab.AddView("XR-COLUMN {Level}", "TANG 1");
            Assert.Single(tab.Views);
            Assert.Equal("XR-COLUMN TANG 1", tab.Views[0].PreviewName);

            // Duplicate
            tab.DuplicateSelected("TANG 1");
            Assert.Equal(2, tab.Views.Count);
            Assert.Equal("XR-COLUMN {Level}_Copy", tab.Views[1].ViewNamePattern);

            // Remove
            tab.RemoveSelected();
            Assert.Single(tab.Views);
        }

        [Fact]
        public void ViewRuleItem_Clone_ShouldCreateIndependentCopy()
        {
            var original = new ViewRuleItem
            {
                IsSelected = true,
                ViewNamePattern = "XR-COLUMN {Level}",
                BrowserSubDiscipline = "XR-COLUMN",
                SampleLevelName = "TANG 2"
            };

            var clone = original.Clone();
            Assert.Equal(original.ViewNamePattern, clone.ViewNamePattern);
            Assert.Equal(original.BrowserSubDiscipline, clone.BrowserSubDiscipline);
            Assert.Equal("XR-COLUMN TANG 2", clone.PreviewName);

            clone.ViewNamePattern = "NEW-PATTERN";
            Assert.Equal("XR-COLUMN {Level}", original.ViewNamePattern);
            Assert.Equal("NEW-PATTERN", clone.ViewNamePattern);
        }
    }
}
