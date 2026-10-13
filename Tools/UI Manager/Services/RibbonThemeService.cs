using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using JNNTool.Tools.UIManager.Models;

namespace JNNTool.Tools.UIManager.Services
{
    /// <summary>
    /// Service xử lý tạo ảnh render đồ họa theo theme (32x32 và 16x16)
    /// và cập nhật trực tiếp lên các nút bấm của Ribbon JNNTool.
    /// </summary>
    public static class RibbonThemeService
    {
        private static readonly List<RibbonButtonItem> _knownButtons = new List<RibbonButtonItem>
        {
            // ───────────────── Panel: General ─────────────────
            new RibbonButtonItem { CommandId = "cmdPalette", DisplayName = "Command\nPalette", PanelName = "General", Icon = JNNIcon.Palette, ToolTip = "Mở bảng Command Palette (Ctrl+F) tra cứu và thực thi nhanh toàn bộ công cụ JNNTool." },
            new RibbonButtonItem { CommandId = "cmdLicense", DisplayName = "Account\nLicense", PanelName = "General", Icon = JNNIcon.Key, ToolTip = "Quản lý bản quyền, đăng nhập tài khoản và kích hoạt JNNTool." },
            new RibbonButtonItem { CommandId = "cmdUIManager", DisplayName = "Ribbon\nTheme", PanelName = "General", Icon = JNNIcon.Palette, ToolTip = "Tùy biến màu sắc theme và kiểu hiển thị Icon trên thanh Ribbon JNNTool." },

#if false
            // ───────────────── Panel: JNN Rebar ─────────────────
            new RibbonButtonItem { CommandId = "cmdSlabRebar", DisplayName = "Rebar\nSlab", PanelName = "JNN Rebar", Icon = JNNIcon.Slab, ToolTip = "Bố trí thép sàn tự động: Lớp dưới, lớp trên và thép mũ gối L/4 theo TCVN 5574:2018." },
            new RibbonButtonItem { CommandId = "cmdBeamRebarSuite", DisplayName = "Rebar\nBeam", PanelName = "JNN Rebar", Icon = JNNIcon.Beam, ToolTip = "Bố trí thép dầm liên tục: Thép chủ, gia cường gối L/4, đai dày/thưa và tạo mặt cắt tự động." },
            new RibbonButtonItem { CommandId = "cmdColumnRebarSuite", DisplayName = "Rebar\nColumn", PanelName = "JNN Rebar", Icon = JNNIcon.Column, ToolTip = "Bố trí thép chuỗi cột theo tầng: Bẻ cổ chai 1:6, đai dày/thưa, đai AB/CN và tạo MCD." },
            new RibbonButtonItem { CommandId = "cmdWallRebarSuite", DisplayName = "Rebar\nWall", PanelName = "JNN Rebar", Icon = JNNIcon.Wall, ToolTip = "Bố trí thép vách 2 lớp: Thép đứng, thép ngang, đai C ghim biên, chống thấm và tạo MCD." },
            new RibbonButtonItem { CommandId = "cmdFoundationRebarSuite", DisplayName = "Rebar\nFooting", PanelName = "JNN Rebar", Icon = JNNIcon.Foundation, ToolTip = "Bố trí thép móng & đài cọc: Lưới đáy móc uốn 90°, lưới trên, thép chờ cột chân vịt." },
            new RibbonButtonItem { CommandId = "cmdPileRebarSuite", DisplayName = "Rebar\nPile", PanelName = "JNN Rebar", Icon = JNNIcon.Pile, ToolTip = "Bố trí thép cọc khoan nhồi / cọc vuông: Lồng thép, đai phân vùng và xuất tọa độ CSV." },
            new RibbonButtonItem { CommandId = "cmdStairRebarSuite", DisplayName = "Rebar\nStair", PanelName = "JNN Rebar", Icon = JNNIcon.Stair, ToolTip = "Bố trí thép bản thang: Thép lớp dưới chịu nhịp, thép mũ gối trên neo dầm và thép phân bố ngang." },
            new RibbonButtonItem { CommandId = "cmdRebarUtilsSuite", DisplayName = "Rebar\nUtils", PanelName = "JNN Rebar", Icon = JNNIcon.Settings, ToolTip = "Bộ tiện ích cốt thép: Thống kê khối lượng, đánh số hiệu Mark, Unobscured, Isolate và chuẩn hóa RebarType." },
            new RibbonButtonItem { CommandId = "cmdRebarVolume", DisplayName = "Rebar\nVolume", PanelName = "JNN Rebar", Icon = JNNIcon.Quantity, ToolTip = "Thống kê khối lượng thép (CSV) theo đường kính, cấu kiện và tầng." },
            new RibbonButtonItem { CommandId = "cmdRebarNumber", DisplayName = "Rebar\nNumber", PanelName = "JNN Rebar", Icon = JNNIcon.Mark, ToolTip = "Đánh số hiệu thép tự động (Auto Mark)." },
            new RibbonButtonItem { CommandId = "cmdRebarUnobscured", DisplayName = "View\nUnobscured", PanelName = "JNN Rebar", Icon = JNNIcon.Check, ToolTip = "Bật/Tắt hiển thị nhìn xuyên cốt thép (View Unobscured) trong 3D." },
            new RibbonButtonItem { CommandId = "cmdRebarIsolate", DisplayName = "Isolate\nRebar", PanelName = "JNN Rebar", Icon = JNNIcon.Filter, ToolTip = "Cô lập hiển thị cốt thép trong view hiện hành." },
            new RibbonButtonItem { CommandId = "cmdRebarTypeManager", DisplayName = "Rebar\nManager", PanelName = "JNN Rebar", Icon = JNNIcon.Settings, ToolTip = "Chuẩn hóa và tạo mới các RebarBarType D6-D32, HookType, CoverType." },
#endif

            // ───────────────── Panel: Modeling ─────────────────
#if false
            new RibbonButtonItem { CommandId = "cmdFloorByRoom", DisplayName = "Floor By\nRoom", PanelName = "Modeling", Icon = JNNIcon.Floor, ToolTip = "Vẽ sàn tự động theo Boundary của Room." },
#endif
            new RibbonButtonItem { CommandId = "cmdSplitBeamByColumn", DisplayName = "Split\nBeam", PanelName = "Modeling", Icon = JNNIcon.Beam, ToolTip = "Ngắt dầm thành từng đoạn tại vị trí cột hoặc dầm giao cắt ngang." },
#if false
            new RibbonButtonItem { CommandId = "cmdRebarBeam", DisplayName = "Rebar\nBeam", PanelName = "Modeling", Icon = JNNIcon.Beam, ToolTip = "Bố trí thép dầm liên tục (Lớp trên, dưới, và đai theo L0/4)." },
#endif
            new RibbonButtonItem { CommandId = "cmdCreateSection", DisplayName = "Create\nSection", PanelName = "Modeling", Icon = JNNIcon.Section, ToolTip = "Duplicate hàng loạt type dầm/cột với kích thước b×h mới." },
#if false
            new RibbonButtonItem { CommandId = "cmdConnection", DisplayName = "Steel\nConnection", PanelName = "Modeling", Icon = JNNIcon.Connection, ToolTip = "Tạo liên kết thép (Steel Connection) cho dầm và cột." },
            new RibbonButtonItem { CommandId = "cmdClashDetection", DisplayName = "Clash\nDetection", PanelName = "Modeling", Icon = JNNIcon.Clash, ToolTip = "Kiểm tra va chạm giữa model và Revit Link." },
#endif
            new RibbonButtonItem { CommandId = "cmdRebarColumn", DisplayName = "Rebar\nColumn", PanelName = "Modeling", Icon = JNNIcon.Column, ToolTip = "Bố trí thép cột (thép dọc, đai, nối thép)." },
            new RibbonButtonItem { CommandId = "cmdDisallowJoinBeam", DisplayName = "Disallow\nJoin Beam", PanelName = "Modeling", Icon = JNNIcon.Join, ToolTip = "Quét chọn nhiều dầm và thực hiện Disallow Join ở 2 đầu." },
            new RibbonButtonItem { CommandId = "cmdCreateFilter", DisplayName = "Create\nFilter", PanelName = "Modeling", Icon = JNNIcon.Filter, ToolTip = "Tạo bộ lọc tự động từ danh sách phần tử và gán màu nhanh." },
            new RibbonButtonItem { CommandId = "cmdSpeedOverriderElement", DisplayName = "Speed\nOverrider", PanelName = "Modeling", Icon = JNNIcon.Palette, ToolTip = "Override hiển thị phần tử nhanh (Speed Overrider)." },
            new RibbonButtonItem { CommandId = "cmdPileLocation", DisplayName = "Pile\nLocation", PanelName = "Modeling", Icon = JNNIcon.Pile, ToolTip = "Ghi tọa độ thực tế (mm) của toàn bộ cọc trong view vào parameter." },
#if false
            new RibbonButtonItem { CommandId = "cmdCreateDimBeam", DisplayName = "Dim\nBeam", PanelName = "Modeling", Icon = JNNIcon.Dim, ToolTip = "Tự tạo Dim chiều dài tổng 2 đầu dầm trên mặt bằng." },
            new RibbonButtonItem { CommandId = "cmdExtendBeam", DisplayName = "Extend\nBeam", PanelName = "Modeling", Icon = JNNIcon.Extend, ToolTip = "Tự nhận diện gối ở 2 đầu và đưa tim dầm tới mặt gối chính xác." },
#endif

            // ───────────────── Panel: CSI x Revit ─────────────────
            new RibbonButtonItem { CommandId = "cmdImportE2K", DisplayName = "Import\nE2K", PanelName = "CSI x Revit", Icon = JNNIcon.Cad, ToolTip = "Import mô hình từ file E2K (ETABS/SAP2000) vào Revit." },
            new RibbonButtonItem { CommandId = "cmdFitScopeBox", DisplayName = "Fit\nScope Box", PanelName = "CSI x Revit", Icon = JNNIcon.ScopeBox, ToolTip = "Khóa và cắt khớp Grid & Level theo Scope Box." },
            new RibbonButtonItem { CommandId = "cmdBoundaryAlign", DisplayName = "Căn Lề\nBiên", PanelName = "CSI x Revit", Icon = JNNIcon.Align, ToolTip = "Dời dầm và cột biên vào trong ranh đất / lưới trục, tự co kéo dầm ngang kết nối." },
            new RibbonButtonItem { CommandId = "cmdAutoJoinSplit", DisplayName = "Auto\nJoin", PanelName = "CSI x Revit", Icon = JNNIcon.Join, ToolTip = "Tự động Join và đảo thứ tự ưu tiên (Column > Beam > Floor)." },
            new RibbonButtonItem { CommandId = "cmdAutoJoinActiveView", DisplayName = "Auto Join\n(View)", PanelName = "CSI x Revit", Icon = JNNIcon.Join, ToolTip = "Auto Join và đảo thứ tự ưu tiên (Column > Beam > Floor) trong Active View." },
            new RibbonButtonItem { CommandId = "cmdAutoJoinProject", DisplayName = "Auto Join\n(Project)", PanelName = "CSI x Revit", Icon = JNNIcon.Join, ToolTip = "Auto Join và đảo thứ tự ưu tiên cho toàn bộ mô hình dự án." },

            // ───────────────── Panel: Layout ─────────────────
            new RibbonButtonItem { CommandId = "cmdAutoViewSheet", DisplayName = "Auto View\n& Sheet", PanelName = "Layout", Icon = JNNIcon.ViewSheet, ToolTip = "Tự động tạo View (OVER) và dàn Sheet hàng loạt theo tầng và Zone." },
            new RibbonButtonItem { CommandId = "cmdAutoDimGrid", DisplayName = "Dim\nGrid", PanelName = "Layout", Icon = JNNIcon.Dim, ToolTip = "Dim tự động lưới trục 4 cạnh (2-3 lớp: Tường / Trục / Tổng)." },
            new RibbonButtonItem { CommandId = "cmdAutoDimColumn", DisplayName = "Dim\nCol/Wall", PanelName = "Layout", Icon = JNNIcon.Dim, ToolTip = "Ghi kích thước tiết diện Cột, Vách và định vị từ Lưới trục trên mặt bằng." },
            new RibbonButtonItem { CommandId = "cmdElementMark", DisplayName = "Element\nMark", PanelName = "Layout", Icon = JNNIcon.Mark, ToolTip = "Đánh Mark tự động cho Dầm, Cột, Sàn theo quy tắc dự án." },
#if false
            new RibbonButtonItem { CommandId = "cmdStairModel_Layout", DisplayName = "Stair\nModel", PanelName = "Layout", Icon = JNNIcon.Stair, ToolTip = "Dựng cầu thang kết cấu BTCT (Straight, U-Turn, L-Shape) kèm chiếu nghỉ." },
#endif

            // ───────────────── Panel: Stair ─────────────────
            new RibbonButtonItem { CommandId = "cmdStairModel", DisplayName = "Stair\nModel", PanelName = "Stair", Icon = JNNIcon.Stair, ToolTip = "Tự động dựng cầu thang kết cấu BTCT (Straight, U-Turn, L-Shape) kèm chiếu nghỉ." },
            new RibbonButtonItem { CommandId = "cmdStairDesigner", DisplayName = "Stair\nDesigner", PanelName = "Stair", Icon = JNNIcon.Stair, ToolTip = "Thiết kế tham số hình học cầu thang 3D và xuất Family (.rfa)." },
#if false
            new RibbonButtonItem { CommandId = "cmdStairRebarSuiteNew", DisplayName = "Stair\nRebar", PanelName = "Stair", Icon = JNNIcon.Stair, ToolTip = "Bố trí uốn thép thang đan chéo góc lõm chống bung nổ bê tông." },
#endif

            // ───────────────── Panel: Quantity ─────────────────
            new RibbonButtonItem { CommandId = "cmdQuantityTakeoff", DisplayName = "Quantity\nTakeoff", PanelName = "Quantity", Icon = JNNIcon.Quantity, ToolTip = "Bóc tách khối lượng toàn diện (Bê tông, Ván khuôn, Thép) và tạo 3D Section Box." }
        };

        private static readonly Dictionary<string, RibbonButtonItem> _buttonLookup =
            _knownButtons.ToDictionary(b => b.CommandId, b => b, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Danh sách toàn bộ nút bấm chuẩn của JNNTool.
        /// </summary>
        public static IReadOnlyList<RibbonButtonItem> GetKnownButtons() => _knownButtons;

        /// <summary>
        /// Khởi tạo ImageSource (RenderTargetBitmap) từ icon vector, màu sắc và kiểu hiển thị.
        /// Sử dụng kỹ thuật 3x Supersampling (DPI 288) giúp icon sắc nét tối đa trên mọi màn hình Full HD, 2K, 4K Retina.
        /// </summary>
        public static ImageSource CreateRibbonImage(JNNIcon icon, string colorHex, RibbonIconStyle style, int size = 32)
        {
            try
            {
                Color baseColor = ParseColor(colorHex);
                double scale = size / 32.0;

                // 3x Supersampling: Render ở độ phân giải gấp 3 lần (96x96 cho nút 32px, 48x48 cho nút 16px)
                // với DPI = 288 (96 * 3) để kích thước logic trong WPF vẫn đúng bằng size (32 hoặc 16)
                const int superSample = 3;
                int pixelSize = size * superSample;
                double dpi = 96.0 * superSample;

                var visual = new DrawingVisual();
                RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(visual, EdgeMode.Unspecified);
                RenderOptions.SetClearTypeHint(visual, ClearTypeHint.Auto);

                using (var dc = visual.RenderOpen())
                {
                    var geometry = VectorIconCatalog.GetGeometry(icon);

                    switch (style)
                    {
                        case RibbonIconStyle.Badge:
                            RenderBadgeStyle(dc, geometry, baseColor, size, scale);
                            break;

                        case RibbonIconStyle.Glyph:
                            RenderGlyphStyle(dc, geometry, baseColor, size, scale);
                            break;

                        case RibbonIconStyle.Circle:
                            RenderCircleStyle(dc, geometry, baseColor, size, scale);
                            break;

                        case RibbonIconStyle.Outline:
                            RenderOutlineStyle(dc, geometry, baseColor, size, scale);
                            break;
                    }
                }

                var rtb = new RenderTargetBitmap(pixelSize, pixelSize, dpi, dpi, PixelFormats.Pbgra32);
                rtb.Render(visual);
                if (rtb.CanFreeze) rtb.Freeze();
                return rtb;
            }
            catch
            {
                // Fallback nếu có lỗi vẽ
                return new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            }
        }

        #region Render Styles

        private static void RenderBadgeStyle(DrawingContext dc, Geometry geometry, Color baseColor, int size, double scale)
        {
            Color darkColor = Darken(baseColor, 0.85);
            var gradient = new LinearGradientBrush(baseColor, darkColor, new Point(0, 0), new Point(0, 1));
            gradient.Freeze();

            double margin = 1.0 * scale;
            double badgeSize = size - (2.0 * margin);
            // Chuẩn bo góc 5px đồng bộ của JNNTool
            double cornerRadius = 5.0 * scale;

            // Viền sáng tinh tế 0.6px tạo chiều sâu sắc nét
            var borderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            borderBrush.Freeze();
            var borderPen = new Pen(borderBrush, 0.6 * scale);
            borderPen.Freeze();

            dc.DrawRoundedRectangle(gradient, borderPen, new Rect(margin, margin, badgeSize, badgeSize), cornerRadius, cornerRadius);
            DrawCenteredGeometry(dc, geometry, Brushes.White, size, 5.5 * scale);
        }

        private static void RenderGlyphStyle(DrawingContext dc, Geometry geometry, Color baseColor, int size, double scale)
        {
            var brush = new SolidColorBrush(baseColor);
            brush.Freeze();
            DrawCenteredGeometry(dc, geometry, brush, size, 3.0 * scale);
        }

        private static void RenderCircleStyle(DrawingContext dc, Geometry geometry, Color baseColor, int size, double scale)
        {
            Color darkColor = Darken(baseColor, 0.88);
            var gradient = new LinearGradientBrush(baseColor, darkColor, new Point(0, 0), new Point(1, 1));
            gradient.Freeze();

            double center = size / 2.0;
            double radius = (size / 2.0) - (1.0 * scale);

            var borderBrush = new SolidColorBrush(Color.FromArgb(45, 255, 255, 255));
            borderBrush.Freeze();
            var borderPen = new Pen(borderBrush, 0.6 * scale);
            borderPen.Freeze();

            dc.DrawEllipse(gradient, borderPen, new Point(center, center), radius, radius);
            DrawCenteredGeometry(dc, geometry, Brushes.White, size, 6.5 * scale);
        }

        private static void RenderOutlineStyle(DrawingContext dc, Geometry geometry, Color baseColor, int size, double scale)
        {
            Color tintColor = Color.FromArgb((byte)(255 * 0.12), baseColor.R, baseColor.G, baseColor.B);
            var fillBrush = new SolidColorBrush(tintColor);
            fillBrush.Freeze();

            var strokeBrush = new SolidColorBrush(baseColor);
            strokeBrush.Freeze();
            var pen = new Pen(strokeBrush, 1.2 * scale);
            pen.Freeze();

            double margin = 1.0 * scale;
            double badgeSize = size - (2.0 * margin);
            // Chuẩn bo góc 5px đồng bộ
            double cornerRadius = 5.0 * scale;

            dc.DrawRoundedRectangle(fillBrush, pen, new Rect(margin, margin, badgeSize, badgeSize), cornerRadius, cornerRadius);
            DrawCenteredGeometry(dc, geometry, strokeBrush, size, 5.5 * scale);
        }

        private static void DrawCenteredGeometry(DrawingContext dc, Geometry geometry, Brush brush, int totalSize, double padding)
        {
            if (geometry == null) return;

            Rect bounds = geometry.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;

            double availSize = totalSize - (2.0 * padding);
            if (availSize <= 0) availSize = totalSize * 0.7;

            double scaleFactor = Math.Min(availSize / bounds.Width, availSize / bounds.Height);
            double targetWidth = bounds.Width * scaleFactor;
            double targetHeight = bounds.Height * scaleFactor;

            double offsetX = padding + ((availSize - targetWidth) / 2.0);
            double offsetY = padding + ((availSize - targetHeight) / 2.0);

            var transformGroup = new TransformGroup();
            transformGroup.Children.Add(new TranslateTransform(-bounds.X, -bounds.Y));
            transformGroup.Children.Add(new ScaleTransform(scaleFactor, scaleFactor));
            transformGroup.Children.Add(new TranslateTransform(offsetX, offsetY));

            dc.PushTransform(transformGroup);
            dc.DrawGeometry(brush, null, geometry);
            dc.Pop();
        }

        #endregion

        #region Color Utilities

        private static Color ParseColor(string hex)
        {
            if (!string.IsNullOrWhiteSpace(hex))
            {
                try
                {
                    return (Color)ColorConverter.ConvertFromString(hex.Trim());
                }
                catch { }
            }
            return (Color)ColorConverter.ConvertFromString(RibbonThemeSettings.DefaultColor);
        }

        private static Color Darken(Color c, double factor)
        {
            return Color.FromRgb(
                (byte)Math.Max(0, Math.Min(255, c.R * factor)),
                (byte)Math.Max(0, Math.Min(255, c.G * factor)),
                (byte)Math.Max(0, Math.Min(255, c.B * factor)));
        }

        #endregion

        #region Application to Revit Ribbon

        /// <summary>
        /// Áp dụng theme vào Ribbon khi Revit đang chạy thông qua UIApplication (trong ExternalEvent).
        /// </summary>
        public static int ApplyThemeToRibbon(UIApplication uiapp, RibbonThemeSettings settings)
        {
            if (uiapp == null || settings == null) return 0;

            int updated = 0;
            string[] targetTabs = { "JNNTool", "IBIM OS" };

            foreach (var tab in targetTabs)
            {
                try
                {
                    List<RibbonPanel> panels = uiapp.GetRibbonPanels(tab);
                    if (panels != null && panels.Count > 0)
                    {
                        updated += ApplyToPanels(panels, settings);
                    }
                }
                catch { }
            }

            return updated;
        }

        /// <summary>
        /// Áp dụng theme vào Ribbon trong lúc khởi động thông qua UIControlledApplication (OnStartup).
        /// </summary>
        public static int ApplyThemeToRibbon(UIControlledApplication app, RibbonThemeSettings settings)
        {
            if (app == null || settings == null) return 0;

            int updated = 0;
            string[] targetTabs = { "JNNTool", "IBIM OS" };

            foreach (var tab in targetTabs)
            {
                try
                {
                    List<RibbonPanel> panels = app.GetRibbonPanels(tab);
                    if (panels != null && panels.Count > 0)
                    {
                        updated += ApplyToPanels(panels, settings);
                    }
                }
                catch { }
            }

            return updated;
        }

        private static int ApplyToPanels(IEnumerable<RibbonPanel> panels, RibbonThemeSettings settings)
        {
            if (panels == null) return 0;

            int updatedCount = 0;
            foreach (var panel in panels)
            {
                string panelColor = settings.PrimaryColorHex;
                if (settings.PanelColors.TryGetValue(panel.Name, out var customPanelColor) && !string.IsNullOrWhiteSpace(customPanelColor))
                {
                    panelColor = customPanelColor;
                }

                foreach (var item in panel.GetItems())
                {
                    updatedCount += UpdateRibbonItem(item, panelColor, settings.IconStyle);
                }
            }

            return updatedCount;
        }

        private static int UpdateRibbonItem(RibbonItem item, string panelColor, RibbonIconStyle style)
        {
            int count = 0;

            if (item is PushButton btn)
            {
                if (TryApplyButtonImage(btn, panelColor, style))
                {
                    count++;
                }
            }
            else if (item is SplitButton split)
            {
                // Cập nhật biểu tượng cho nút chính SplitButton
                if (TryApplyButtonImage(split, panelColor, style))
                {
                    count++;
                }
                foreach (var sub in split.GetItems())
                {
                    if (sub is RibbonButton subBtn && TryApplyButtonImage(subBtn, panelColor, style))
                    {
                        count++;
                    }
                }
            }
            else if (item is PulldownButton pull)
            {
                // Cập nhật biểu tượng cho nút chính PulldownButton
                if (TryApplyButtonImage(pull, panelColor, style))
                {
                    count++;
                }
                foreach (var sub in pull.GetItems())
                {
                    if (sub is RibbonButton subBtn && TryApplyButtonImage(subBtn, panelColor, style))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        private static bool TryApplyButtonImage(RibbonButton btn, string panelColor, RibbonIconStyle style)
        {
            // Tìm metadata dựa theo Name của RibbonButton (ví dụ cmdSlabRebar)
            string btnName = btn.Name ?? string.Empty;
            // Xử lý các suffix nút nhân bản như _0, _1 nếu có
            string baseName = btnName.Contains("_") ? btnName.Split('_')[0] : btnName;

            if (_buttonLookup.TryGetValue(btnName, out var meta) || _buttonLookup.TryGetValue(baseName, out meta))
            {
                var img32 = CreateRibbonImage(meta.Icon, panelColor, style, 32);
                var img16 = CreateRibbonImage(meta.Icon, panelColor, style, 16);

                btn.LargeImage = img32;
                btn.Image = img16;
                return true;
            }

            return false;
        }

        #endregion
    }
}
