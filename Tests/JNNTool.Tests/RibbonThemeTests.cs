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
        public void JNNIcon_ShouldContainAllRegisteredIcons()
        {
            var allIcons = Enum.GetValues(typeof(JNNIcon)).Cast<JNNIcon>().ToList();
            Assert.True(allIcons.Count >= 50, $"Expected at least 50 icons, found {allIcons.Count}");
            Assert.Contains(JNNIcon.MergeFloor, allIcons);
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

        [Fact]
        public void MergeFloor_ShouldHaveCrispVectorGeometry_AndSavePng()
        {
            Assert.True(VectorIconCatalog.HasGeometry(JNNIcon.MergeFloor));
            var geom = VectorIconCatalog.GetGeometry(JNNIcon.MergeFloor);
            Assert.NotNull(geom);
            Assert.False(geom.IsEmpty());

            // Render supersampled vector icon
            var img32 = RibbonThemeService.CreateRibbonImage(JNNIcon.MergeFloor, "#0284C7", RibbonIconStyle.Badge, 32);
            Assert.NotNull(img32);

            if (img32 is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));

                string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
                string targetPng = System.IO.Path.Combine(solutionDir, "Tools", "MergeFloor", "MergeFloor.png");
                using (var ms = new System.IO.MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] pngBytes = ms.ToArray();
                    System.IO.File.WriteAllBytes(targetPng, pngBytes);

                    string bundlePng = System.IO.Path.Combine(solutionDir, "JNNTool.bundle", "Contents", "Resources", "MergeFloor.png");
                    if (System.IO.File.Exists(bundlePng))
                    {
                        System.IO.File.WriteAllBytes(bundlePng, pngBytes);
                    }
                }
            }
        }

        [Fact]
        public void CutFloor_ShouldHaveCrispVectorGeometry_AndSavePng()
        {
            Assert.True(VectorIconCatalog.HasGeometry(JNNIcon.CutFloor));
            var geom = VectorIconCatalog.GetGeometry(JNNIcon.CutFloor);
            Assert.NotNull(geom);
            Assert.False(geom.IsEmpty());

            // Render supersampled vector icon
            var img32 = RibbonThemeService.CreateRibbonImage(JNNIcon.CutFloor, "#0284C7", RibbonIconStyle.Badge, 32);
            Assert.NotNull(img32);

            if (img32 is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));

                string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
                string targetPng = System.IO.Path.Combine(solutionDir, "Tools", "CutByFloor", "CutFloor.png");
                using (var ms = new System.IO.MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] pngBytes = ms.ToArray();
                    System.IO.File.WriteAllBytes(targetPng, pngBytes);

                    string resourcesPng = System.IO.Path.Combine(solutionDir, "Resources", "CutFloor.png");
                    if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(resourcesPng)))
                        System.IO.File.WriteAllBytes(resourcesPng, pngBytes);

                    string bundlePng = System.IO.Path.Combine(solutionDir, "JNNTool.bundle", "Contents", "Resources", "CutFloor.png");
                    if (System.IO.Directory.Exists(System.IO.Path.GetDirectoryName(bundlePng)))
                        System.IO.File.WriteAllBytes(bundlePng, pngBytes);
                }
            }
        }

        [Fact]
        public void CreateView_ShouldHaveCrispVectorGeometry_AndSavePng()
        {
            Assert.True(VectorIconCatalog.HasGeometry(JNNIcon.CreateView));
            var geom = VectorIconCatalog.GetGeometry(JNNIcon.CreateView);
            Assert.NotNull(geom);
            Assert.False(geom.IsEmpty());

            // Render supersampled vector icon (3x 96x96 @ 288 DPI)
            var img32 = RibbonThemeService.CreateRibbonImage(JNNIcon.CreateView, "#0284C7", RibbonIconStyle.Badge, 32);
            Assert.NotNull(img32);

            if (img32 is System.Windows.Media.Imaging.BitmapSource bmp)
            {
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));

                string solutionDir = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
                string targetDir = System.IO.Path.Combine(solutionDir, "Tools", "Create View");
                if (!System.IO.Directory.Exists(targetDir))
                    System.IO.Directory.CreateDirectory(targetDir);

                string targetPng = System.IO.Path.Combine(targetDir, "CreateView.png");
                using (var ms = new System.IO.MemoryStream())
                {
                    encoder.Save(ms);
                    byte[] pngBytes = ms.ToArray();
                    System.IO.File.WriteAllBytes(targetPng, pngBytes);

                    string bundleDir = System.IO.Path.Combine(solutionDir, "JNNTool.bundle", "Contents", "Resources");
                    if (!System.IO.Directory.Exists(bundleDir))
                        System.IO.Directory.CreateDirectory(bundleDir);

                    string bundlePng = System.IO.Path.Combine(bundleDir, "CreateView.png");
                    System.IO.File.WriteAllBytes(bundlePng, pngBytes);
                }
            }
        }
    }
}

