using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using JNNTool.Tools.UIManager.Models;

namespace JNNTool.Tools.UIManager.Services
{
    /// <summary>
    /// Thư viện hình học vector (Geometry) thuần túy 50 biểu tượng công cụ của JNNTool
    /// được cập nhật từ bộ thiết kế JNN BIM Vector Icons (với nét thanh thoát 1.3px, chuẩn Clean-Room).
    /// Không phụ thuộc bất kỳ thư viện bên ngoài nào, tự co giãn mượt mà ở mọi độ phân giải DPI/4K Retina.
    /// </summary>
    public static class VectorIconCatalog
    {
        public const string Blue = "#087BC1";
        public const string Cyan = "#16C5D8";
        public const string DarkBlue = "#07519A";
        public const double DefaultStrokeWidth = 1.3;

        private static readonly Dictionary<JNNIcon, Geometry> _geometries = new Dictionary<JNNIcon, Geometry>();
        private static readonly Dictionary<JNNIcon, string> _svgMarkups = new Dictionary<JNNIcon, string>();

        static VectorIconCatalog()
        {
            // ─────────────────────────────────────────────────────────────
            // 1. General & Base Elements (34 Icons)
            // ─────────────────────────────────────────────────────────────
            Register(JNNIcon.Slab, @"<rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2""/><path d=""M3 9h18M3 15h18M9 3v18M15 3v18""/>");
            Register(JNNIcon.Beam, @"<path d=""M3 5h18v5H3zM3 14h18v5H3z""/><path d=""M7 10v4M17 10v4""/>");
            Register(JNNIcon.Column, @"<rect x=""7"" y=""2.5"" width=""10"" height=""19"" rx=""2""/><path d=""M7 7h10M7 17h10""/>");
            Register(JNNIcon.Wall, @"<path d=""M3 4h18v16H3z""/><path d=""M3 9h6v6H3M9 4v5M15 9h6M15 9v6M9 15v5""/>");
            Register(JNNIcon.Foundation, @"<path d=""M8 3h8l2 5H6zM4 9h16l2 4H2zM5 15h14l2 5H3z""/>");
            Register(JNNIcon.Pile, @"<path d=""M7 3h10v10l-5 8-5-8z""/><path d=""M7 7h10M7 11h10M9 15h6""/>");
            Register(JNNIcon.Stair, @"<path d=""M3 20h5v-4h4v-4h4V8h5""/><path d=""M3 20V4h5v4h4v4h4v4h5""/>");
            Register(JNNIcon.Floor, @"<path d=""m12 3 9 5-9 5-9-5z""/><path d=""m4 12 8 4 8-4M4 16l8 5 8-5""/>");
            Register(JNNIcon.Section, @"<circle cx=""12"" cy=""12"" r=""9""/><path d=""M4 12h16M12 4v16""/>");
            Register(JNNIcon.Cad, @"<path d=""M5 2.5h9l5 5V21H5z""/><path d=""M14 2.5V8h5M8 12h8M8 16h5""/>");
            Register(JNNIcon.ScopeBox, @"<path d=""M8 3H4v5M16 3h4v5M4 16v5h4M20 16v5h-4""/><rect x=""7"" y=""7"" width=""10"" height=""10"" rx=""1""/>");
            Register(JNNIcon.Align, @"<path d=""M12 2v20M3 5h6v5H3zM15 14h6v5h-6z""/>");
            Register(JNNIcon.Join, @"<rect x=""2.5"" y=""3"" width=""11"" height=""11"" rx=""1.5""/><rect x=""10.5"" y=""10"" width=""11"" height=""11"" rx=""1.5""/>");
            Register(JNNIcon.Connection, @"<rect x=""3"" y=""3"" width=""5"" height=""18"" rx=""1""/><path d=""M8 9h13v6H8""/><circle cx=""5.5"" cy=""6"" r="".75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""5.5"" cy=""18"" r="".75"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.Clash, @"<path d=""M12 2.5 21 6v6c0 5-3.8 8.5-9 10-5.2-1.5-9-5-9-10V6z""/><path d=""M12 7v6""/><circle cx=""12"" cy=""16.5"" r=""1""/>");
            Register(JNNIcon.Filter, @"<path d=""M3 4h18l-7 8v6l-4 2v-8z""/>");
            Register(JNNIcon.Palette, @"<path d=""M12 3a9 9 0 1 0 0 18h1.5a2 2 0 0 0 1.2-3.6 1.7 1.7 0 0 1 1-3.1H18a3 3 0 0 0 3-3c0-4.6-4-8.3-9-8.3z""/><circle cx=""7.5"" cy=""11"" r="".8"" fill=""#16C5D8""/><circle cx=""10"" cy=""7.5"" r="".8"" fill=""#16C5D8""/><circle cx=""15"" cy=""7.5"" r="".8"" fill=""#16C5D8""/>");
            Register(JNNIcon.Dim, @"<path d=""M4 4v16M20 4v16M4 12h16""/><path d=""m7 9-3 3 3 3M17 9l3 3-3 3""/>");
            Register(JNNIcon.Extend, @"<path d=""M3 9h12v6H3zM15 5l6 7-6 7z""/>");
            Register(JNNIcon.ViewSheet, @"<rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""1.5""/><path d=""M6 6h7v6H6zM15 6h3M15 9h3M6 15h12M6 18h8""/>");
            Register(JNNIcon.Mark, @"<path d=""m4 12 8-8h8v8l-8 8z""/><circle cx=""16"" cy=""8"" r=""1.2""/>");
            Register(JNNIcon.Quantity, @"<rect x=""3"" y=""13"" width=""4"" height=""8"" rx=""1""/><rect x=""10"" y=""8"" width=""4"" height=""13"" rx=""1""/><rect x=""17"" y=""3"" width=""4"" height=""18"" rx=""1""/>");
            Register(JNNIcon.Check, @"<path d=""m4 12 5 5L20 5""/>");
            Register(JNNIcon.Refresh, @"<path d=""M20 7V3l-3 3a8 8 0 1 0 2 10""/><path d=""M20 3v6h-6""/>");
            Register(JNNIcon.Settings, @"<circle cx=""12"" cy=""12"" r=""3""/><path d=""m19 13 2 1-2 4-2-1-2 1v2h-5v-2l-2-1-2 1-2-4 2-1v-2l-2-1 2-4 2 1 2-1V4h5v2l2 1 2-1 2 4-2 1z""/>");
            Register(JNNIcon.Key, @"<circle cx=""15.5"" cy=""8.5"" r=""4.5""/><path d=""m12.5 11.5-9 9v-4h3v-3h3l1-1""/><circle cx=""15.5"" cy=""8.5"" r=""1""/>");
            Register(JNNIcon.Split, @"<rect x=""2.5"" y=""8"" width=""7"" height=""8"" rx=""1""/><rect x=""14.5"" y=""8"" width=""7"" height=""8"" rx=""1""/><path d=""M12 3v18M10 6l2-3 2 3M10 18l2 3 2-3""/>");
            Register(JNNIcon.DisallowJoin, @"<rect x=""3"" y=""3"" width=""5"" height=""18"" rx=""1""/><path d=""M14 9h7v6h-7zM10 6v12""/>");
            Register(JNNIcon.Flash, @"<path d=""m13 2-9 12h7l-1 8 10-13h-7z""/>");
            Register(JNNIcon.Target, @"<circle cx=""12"" cy=""12"" r=""8""/><circle cx=""12"" cy=""12"" r=""3""/><path d=""M12 1v4M12 19v4M1 12h4M19 12h4""/>");
            Register(JNNIcon.Eye, @"<path d=""M2.5 12s3.5-6 9.5-6 9.5 6 9.5 6-3.5 6-9.5 6-9.5-6-9.5-6z""/><circle cx=""12"" cy=""12"" r=""3""/>");
            Register(JNNIcon.CommandPalette, @"<rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2""/><path d=""m7 8 4 4-4 4M13 16h4""/>");
            Register(JNNIcon.DimColumn, @"<rect x=""8"" y=""7"" width=""8"" height=""10"" rx=""1""/><path d=""M6 3h12M6 21h12M3 7v10M21 7v10M8 5 6 3 8 1M16 5l2-2-2-2""/>");
            Register(JNNIcon.StairDesigner, @"<path d=""M3 20h5v-4h4v-4h4V8h5""/><path d=""M4 4h6v4M10 4V2M16 4h4v4""/>");

            // ─────────────────────────────────────────────────────────────
            // 2. JNN Rebar Suite (12 Icons - TCVN 5574:2018)
            // ─────────────────────────────────────────────────────────────
            Register(JNNIcon.RebarBeam, @"<rect x=""2.5"" y=""5"" width=""19"" height=""14"" rx=""1.5""/><path d=""M5 9v-1h14v1M5 15v1h14v-1M8.5 8v8M12 8v8M15.5 8v8""/>");
            Register(JNNIcon.RebarColumn, @"<rect x=""3.5"" y=""3.5"" width=""17"" height=""17"" rx=""2""/><rect x=""7"" y=""7"" width=""10"" height=""10"" rx=""1""/><path d=""m7 9.5 2.5-2.5""/><circle cx=""7"" cy=""7"" r=""1"" fill=""#16C5D8"" stroke=""none""/><circle cx=""17"" cy=""7"" r=""1"" fill=""#16C5D8"" stroke=""none""/><circle cx=""7"" cy=""17"" r=""1"" fill=""#16C5D8"" stroke=""none""/><circle cx=""17"" cy=""17"" r=""1"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.RebarSlab, @"<rect x=""2.5"" y=""11"" width=""19"" height=""9.5"" rx=""1.5""/><path d=""M3 7h5v4M16 11V7h5M4.5 16.5h15""/><circle cx=""6"" cy=""14"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""12"" cy=""14"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""18"" cy=""14"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.RebarWall, @"<rect x=""6.5"" y=""2.5"" width=""11"" height=""19"" rx=""1.5""/><path d=""M9.5 5v14M14.5 5v14M9.5 7.5h5M9.5 12h5M9.5 16.5h5""/><circle cx=""9.5"" cy=""7.5"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""14.5"" cy=""7.5"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""9.5"" cy=""12"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""14.5"" cy=""12"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""9.5"" cy=""16.5"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/><circle cx=""14.5"" cy=""16.5"" r=""0.75"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.RebarFooting, @"<path d=""M7.5 4h9l3.5 8v8H4v-8z""/><path d=""M6 18v-1.5h12V18M10 4v9.5l-3 2M14 4v9.5l3 2""/>");
            Register(JNNIcon.RebarPile, @"<path d=""M7 3h10v12l-5 6-5-6z""/><path d=""M9.5 5v10l2.5 3 2.5-3V5M7 7h10M7 11h10M8.5 14.5h7""/>");
            Register(JNNIcon.RebarStair, @"<path d=""M3 19h5v-4h4v-4h4V7h5""/><path d=""M3 16h6l4-4h8M5 21h4l5-5h7""/>");
            Register(JNNIcon.RebarVolume, @"<rect x=""3"" y=""12"" width=""4.5"" height=""9"" rx=""1""/><rect x=""9.5"" y=""7"" width=""4.5"" height=""14"" rx=""1""/><rect x=""16.5"" y=""3"" width=""4.5"" height=""18"" rx=""1""/><path d=""M9.5 10.5h4.5M9.5 14h4.5M9.5 17.5h4.5M16.5 6.5h4.5M16.5 10h4.5M16.5 13.5h4.5M16.5 17h4.5""/>");
            Register(JNNIcon.RebarNumber, @"<path d=""M3 18h9l4-4""/><circle cx=""16"" cy=""8"" r=""5""/><path d=""M14.5 5.5v5M17.5 5.5v5M13.5 7h5M13.5 9h5""/>");
            Register(JNNIcon.RebarUnobscured, @"<path d=""M2.5 12s3.5-6.5 9.5-6.5 9.5 6.5 9.5 6.5-3.5 6.5-9.5 6.5-9.5-6.5-9.5-6.5z""/><circle cx=""12"" cy=""12"" r=""3.5""/><path d=""M12 8.5v7M8.5 12h7""/>");
            Register(JNNIcon.RebarIsolate, @"<path d=""M7 3H3v4M17 3h4v4M3 17v4h4M21 17v4h-4""/><path d=""M6 12h12M12 6v12""/><circle cx=""12"" cy=""12"" r=""1.5"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.RebarTypeManager, @"<circle cx=""12"" cy=""12"" r=""7""/><path d=""M7.5 16.5 16.5 7.5M12 2v3M12 19v3""/><path d=""M4 4h4M16 4h4M4 20h4M16 20h4""/>");

            // ─────────────────────────────────────────────────────────────
            // 3. Extended Modeling & CSI Analysis (4 Icons)
            // ─────────────────────────────────────────────────────────────
            Register(JNNIcon.FloorByRoom, @"<rect x=""3"" y=""3"" width=""18"" height=""18"" rx=""2"" stroke-dasharray=""3 2""/><path d=""M7.5 14V7h3.5a2.5 2.5 0 0 1 0 5H7.5M11 12l3.5 5""/><path d=""m14 17 6-4-6-4""/>");
            Register(JNNIcon.DimBeam, @"<rect x=""3"" y=""10"" width=""18"" height=""8"" rx=""1.5""/><path d=""M3 5v6M21 5v6M3 6.5h18M6 4.5 3 6.5l3 2M18 4.5l3 2-3 2""/>");
            Register(JNNIcon.EtabsConverter, @"<path d=""M4 21V5.5l8-3 8 3V21zM4 9.5h16M4 15h16M12 2.5V21""/><circle cx=""12"" cy=""9.5"" r=""1"" fill=""#16C5D8"" stroke=""none""/><circle cx=""12"" cy=""15"" r=""1"" fill=""#16C5D8"" stroke=""none""/>");
            Register(JNNIcon.PropertyViewer, @"<rect x=""3"" y=""3"" width=""13"" height=""18"" rx=""1.5""/><path d=""M6 7h7M6 11h7M6 15h4""/><circle cx=""16"" cy=""16"" r=""3.5""/><path d=""m18.5 18.5 3 3""/>");
        }

        private static void Register(JNNIcon icon, string svgMarkup)
        {
            _svgMarkups[icon] = svgMarkup;
            try
            {
                var geom = ParseSvgMarkup(svgMarkup, DefaultStrokeWidth);
                if (geom.CanFreeze) geom.Freeze();
                _geometries[icon] = geom;
            }
            catch
            {
                // Fallback nếu có lỗi phân tích hình học
            }
        }

        /// <summary>
        /// Phân tích cú pháp chuỗi SVG (gồm các thẻ path, rect, circle) thành Geometry WPF hoàn chỉnh.
        /// Các đường nét viền (stroke) tự động được chuyển thành nét viền kín (widened outline)
        /// với nét bo tròn đầu/góc tinh tế (Round linecap/linejoin) đúng tiêu chuẩn đồ họa vector.
        /// </summary>
        public static Geometry ParseSvgMarkup(string svgMarkup, double strokeWidth = DefaultStrokeWidth)
        {
            var group = new GeometryGroup();
            var strokePen = new Pen(Brushes.Black, strokeWidth)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            if (strokePen.CanFreeze) strokePen.Freeze();

            var matches = Regex.Matches(svgMarkup, @"<(?<tag>rect|circle|path)\b(?<attrs>[^>]*)/?>", RegexOptions.IgnoreCase);
            foreach (Match m in matches)
            {
                string tag = m.Groups["tag"].Value.ToLowerInvariant();
                string attrsStr = m.Groups["attrs"].Value;
                var attrs = ParseAttributes(attrsStr);

                if (tag == "path")
                {
                    if (attrs.TryGetValue("d", out string? d) && !string.IsNullOrWhiteSpace(d))
                    {
                        var pg = Geometry.Parse(d);
                        var widened = pg.GetWidenedPathGeometry(strokePen);
                        if (widened != null && !widened.IsEmpty())
                        {
                            group.Children.Add(widened);
                        }
                    }
                }
                else if (tag == "rect")
                {
                    double x = GetDouble(attrs, "x", 0);
                    double y = GetDouble(attrs, "y", 0);
                    double w = GetDouble(attrs, "width", 0);
                    double h = GetDouble(attrs, "height", 0);
                    double rx = GetDouble(attrs, "rx", 0);
                    double ry = GetDouble(attrs, "ry", rx);

                    var rg = new RectangleGeometry(new Rect(x, y, w, h), rx, ry);
                    var widened = rg.GetWidenedPathGeometry(strokePen);
                    if (widened != null && !widened.IsEmpty())
                    {
                        group.Children.Add(widened);
                    }
                }
                else if (tag == "circle")
                {
                    double cx = GetDouble(attrs, "cx", 0);
                    double cy = GetDouble(attrs, "cy", 0);
                    double r = GetDouble(attrs, "r", 0);

                    bool isFilled = attrs.ContainsKey("fill") && attrs["fill"] != "none";
                    bool isStrokeNone = attrs.TryGetValue("stroke", out var strk) && strk == "none";

                    var eg = new EllipseGeometry(new Point(cx, cy), r, r);
                    if (isFilled || isStrokeNone || r <= 1.0)
                    {
                        // Điểm tròn đặc (Solid dot) được giữ nguyên để tô đầy
                        group.Children.Add(eg);
                    }
                    else
                    {
                        // Vòng tròn rỗng được viền theo độ dày nét
                        var widened = eg.GetWidenedPathGeometry(strokePen);
                        if (widened != null && !widened.IsEmpty())
                        {
                            group.Children.Add(widened);
                        }
                    }
                }
            }

            if (group.Children.Count == 1)
            {
                var single = group.Children[0];
                if (single.CanFreeze) single.Freeze();
                return single;
            }

            if (group.CanFreeze) group.Freeze();
            return group;
        }

        private static Dictionary<string, string> ParseAttributes(string text)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var matches = Regex.Matches(text, @"([a-zA-Z0-9\-]+)=[\""\']([^\""\']*)[\""\']");
            foreach (Match m in matches)
            {
                dict[m.Groups[1].Value] = m.Groups[2].Value;
            }
            return dict;
        }

        private static double GetDouble(Dictionary<string, string> dict, string key, double defaultValue)
        {
            if (dict.TryGetValue(key, out var val) &&
                double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
            {
                return result;
            }
            return defaultValue;
        }

        /// <summary>
        /// Kiểm tra biểu tượng đã được đăng ký trong VectorIconCatalog hay chưa.
        /// </summary>
        public static bool HasGeometry(JNNIcon icon) => _geometries.ContainsKey(icon);

        /// <summary>
        /// Lấy đối tượng Geometry đã freeze theo icon tương ứng.
        /// </summary>
        public static Geometry GetGeometry(JNNIcon icon)
        {
            if (_geometries.TryGetValue(icon, out var geom))
            {
                return geom;
            }

            return _geometries.TryGetValue(JNNIcon.Settings, out var fallback) ? fallback : Geometry.Empty;
        }

        /// <summary>
        /// Lấy chuỗi mã hình học SVG gốc của biểu tượng.
        /// </summary>
        public static string GetSvgMarkup(JNNIcon icon)
        {
            return _svgMarkups.TryGetValue(icon, out var markup) ? markup : string.Empty;
        }

        /// <summary>
        /// Xuất biểu tượng thành tài liệu SVG hoàn chỉnh (viewBox 0 0 24 24 kèm gradient Blue/Cyan).
        /// </summary>
        public static string ToSvg(JNNIcon icon, int size = 32, double strokeWidth = DefaultStrokeWidth)
        {
            string inner = GetSvgMarkup(icon);
            return $@"<svg xmlns=""http://www.w3.org/2000/svg"" width=""{size}"" height=""{size}"" viewBox=""0 0 24 24"">
  <defs>
    <linearGradient id=""bim"" x1=""0"" y1=""0"" x2=""1"" y2=""1"">
      <stop offset=""0"" stop-color=""{Cyan}""/>
      <stop offset=""1"" stop-color=""{Blue}""/>
    </linearGradient>
  </defs>
  <g fill=""none"" stroke=""url(#bim)"" stroke-width=""{strokeWidth}"" stroke-linecap=""round"" stroke-linejoin=""round"">
    {inner}
  </g>
</svg>";
        }
    }
}
