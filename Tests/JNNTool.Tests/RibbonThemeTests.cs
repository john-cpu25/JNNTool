using System;
using System.Linq;
using JNNTool.Tools.UIManager.Models;
using JNNTool.Tools.UIManager.Services;
using Xunit;

namespace JNNTool.Tests
{
    public class RibbonThemeTests
    {
        [Fact]
        public void JNNIcon_ShouldContainExactly50Icons()
        {
            var allIcons = Enum.GetValues(typeof(JNNIcon)).Cast<JNNIcon>().ToList();
            Assert.Equal(50, allIcons.Count);
        }

        [Fact]
        public void VectorIconCatalog_ShouldReturnValidGeometry_ForAllIcons()
        {
            var allIcons = Enum.GetValues(typeof(JNNIcon)).Cast<JNNIcon>();

            foreach (var icon in allIcons)
            {
                Assert.True(VectorIconCatalog.HasGeometry(icon), $"Icon {icon} must be explicitly registered in VectorIconCatalog without falling back to Settings");
                var geom = VectorIconCatalog.GetGeometry(icon);
                Assert.NotNull(geom);
                Assert.False(geom.IsEmpty(), $"Icon {icon} must have non-empty geometry");
                var svg = VectorIconCatalog.ToSvg(icon);
                Assert.False(string.IsNullOrWhiteSpace(svg), $"Icon {icon} must have valid SVG markup");
            }
        }

        [Fact]
        public void RibbonThemeService_KnownButtons_ShouldBePopulatedAndValid()
        {
            var buttons = RibbonThemeService.GetKnownButtons();

            Assert.NotNull(buttons);
            Assert.True(buttons.Count > 20, "Known buttons should include all major JNNTool commands");

            // Verify each button has essential fields
            foreach (var btn in buttons)
            {
                Assert.False(string.IsNullOrWhiteSpace(btn.CommandId), "Button must have CommandId");
                Assert.False(string.IsNullOrWhiteSpace(btn.DisplayName), "Button must have DisplayName");
                Assert.False(string.IsNullOrWhiteSpace(btn.PanelName), "Button must have PanelName");
            }

            // CommandIds must be unique
            var distinctIds = buttons.Select(b => b.CommandId).Distinct(StringComparer.OrdinalIgnoreCase).Count();
            Assert.Equal(buttons.Count, distinctIds);
        }

        [Fact]
        public void RibbonThemeSettings_DefaultValues_ShouldMatchStandard()
        {
            var settings = new RibbonThemeSettings();

            Assert.Equal(RibbonThemeSettings.DefaultColor, settings.PrimaryColorHex);
            Assert.Equal(RibbonIconStyle.Badge, settings.IconStyle);
            Assert.True(settings.UseVectorTheme);
            Assert.NotNull(settings.PanelColors);
        }

        [Fact]
        public void VectorIconCatalog_AllIcons_ShouldHavePositiveBounds()
        {
            var allIcons = Enum.GetValues(typeof(JNNIcon)).Cast<JNNIcon>();

            foreach (var icon in allIcons)
            {
                var geom = VectorIconCatalog.GetGeometry(icon);
                Assert.NotNull(geom);
                Assert.True(geom.Bounds.Width > 0, $"Icon {icon} must have Bounds.Width > 0");
                Assert.True(geom.Bounds.Height > 0, $"Icon {icon} must have Bounds.Height > 0");
            }
        }

        [Fact]
        public void RibbonThemeService_CreateRibbonImage_ShouldRenderSupersampledBitmap()
        {
            // Test 32px large button icon rendering (96x96 pixels at 288 DPI)
            var img32 = RibbonThemeService.CreateRibbonImage(JNNIcon.Slab, "#2563EB", RibbonIconStyle.Badge, 32);
            Assert.NotNull(img32);
            if (img32 is System.Windows.Media.Imaging.BitmapSource bmp32)
            {
                Assert.Equal(32, bmp32.Width, 0.5);
                Assert.Equal(32, bmp32.Height, 0.5);
                Assert.Equal(96, bmp32.PixelWidth);
                Assert.Equal(96, bmp32.PixelHeight);
            }

            // Test 16px small button icon rendering (48x48 pixels at 288 DPI)
            var img16 = RibbonThemeService.CreateRibbonImage(JNNIcon.Beam, "#10B981", RibbonIconStyle.Glyph, 16);
            Assert.NotNull(img16);
            if (img16 is System.Windows.Media.Imaging.BitmapSource bmp16)
            {
                Assert.Equal(16, bmp16.Width, 0.5);
                Assert.Equal(16, bmp16.Height, 0.5);
                Assert.Equal(48, bmp16.PixelWidth);
                Assert.Equal(48, bmp16.PixelHeight);
            }
        }
    }
}
