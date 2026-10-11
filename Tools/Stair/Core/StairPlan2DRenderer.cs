using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JNN.Stair.Models;

namespace JNN.Stair.Core
{
    /// <summary>
    /// Bộ vẽ vector 2D chuyên nghiệp cho Mặt Bằng Cầu Thang & Kích thước (Tab 1)
    /// </summary>
    public static class StairPlan2DRenderer
    {
        // Bảng màu chuẩn hiển thị sắc nét trên nền sáng
        private static readonly Brush ColorFlight = new SolidColorBrush(Color.FromRgb(2, 132, 199));     // Blue (#0284C7)
        private static readonly Brush ColorLanding = new SolidColorBrush(Color.FromArgb(40, 2, 132, 199)); // Xanh nhạt trong trẻo
        private static readonly Brush ColorWinder = new SolidColorBrush(Color.FromArgb(60, 147, 51, 234)); // Purple nhạt cho bậc quạt
        private static readonly Brush ColorDim = new SolidColorBrush(Color.FromRgb(217, 119, 6));       // Cam đất (#D97706)
        private static readonly Brush ColorSectionLine = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Đỏ nét đứt
        private static readonly Brush ColorSectionBadge = new SolidColorBrush(Color.FromRgb(185, 28, 28)); // Đỏ sẫm
        private static readonly Brush ColorText = new SolidColorBrush(Color.FromRgb(30, 41, 59));        // Chữ xám đen (#1E293B) sắc nét
        private static readonly Brush ColorArrow = new SolidColorBrush(Color.FromRgb(2, 132, 199));      // Mũi tên hướng đi

        public static void RenderPlan(Canvas canvas, StairLayout layout)
        {
            canvas.Children.Clear();
            if (canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            double canvasW = canvas.ActualWidth;
            double canvasH = canvas.ActualHeight;

            // Xác định bounding box của thang trong tọa độ mm
            double W = layout.StairWidthMm;
            double T = layout.TreadMm;
            double landingD = layout.LandingDepthMm;
            double gap = layout.FlightGapMm;

            var risers = layout.GetRisersPerFlight();
            int n1 = Math.Max(1, risers[0] - (layout.IsWinder ? layout.WinderStepCount / 2 : 1));
            int n2 = risers.Count > 1 ? Math.Max(1, risers[1] - (layout.IsWinder ? layout.WinderStepCount / 2 : 0)) : n1;

            double flight1Len = n1 * T;
            double flight2Len = n2 * T;

            double totalW = layout.Preset switch
            {
                StairPreset.Straight => W,
                StairPreset.LShape or StairPreset.LShapeWinder => W + landingD + flight2Len,
                StairPreset.UShape => 2 * W + gap,
                StairPreset.ThreeFlightU or StairPreset.ThreeFlightUWinder => 2 * W + gap + (risers.Count > 2 ? risers[1] * T : 2000),
                _ => 2 * W + gap
            };

            double totalH = layout.Preset switch
            {
                StairPreset.Straight => flight1Len,
                StairPreset.LShape or StairPreset.LShapeWinder => flight1Len + landingD,
                _ => Math.Max(flight1Len, flight2Len) + landingD
            };

            // Tính tỷ lệ zoom để fit vừa Canvas
            double margin = 60;
            double scale = Math.Min((canvasW - 2 * margin) / totalW, (canvasH - 2 * margin) / totalH);
            if (scale <= 0) scale = 0.1;

            // Tọa độ gốc (0,0) đặt giữa canvas
            double originX = (canvasW - totalW * scale) / 2.0;
            double originY = (canvasH + totalH * scale) / 2.0; // Hệ trục Y màn hình hướng xuống dưới

            // Vẽ theo từng dạng thang
            switch (layout.Preset)
            {
                case StairPreset.Straight:
                    DrawStraightPlan(canvas, layout, originX, originY, scale, W, T, n1);
                    break;
                case StairPreset.LShape:
                case StairPreset.LShapeWinder:
                    DrawLShapePlan(canvas, layout, originX, originY, scale, W, T, landingD, n1, n2);
                    break;
                case StairPreset.ThreeFlightU:
                case StairPreset.ThreeFlightUWinder:
                    DrawThreeFlightUPlan(canvas, layout, originX, originY, scale, W, T, landingD, gap, risers);
                    break;
                default:
                    DrawUShapePlan(canvas, layout, originX, originY, scale, W, T, landingD, gap, n1, n2);
                    break;
            }
        }

        #region Render Chi Tiết Từng Loại
        private static void DrawUShapePlan(
            Canvas canvas, StairLayout layout,
            double ox, double oy, double s,
            double W, double T, double landingD, double gap,
            int n1, int n2)
        {
            double wPx = W * s;
            double tPx = T * s;
            double landingDPx = landingD * s;
            double gapPx = gap * s;
            double totalWPx = 2 * wPx + gapPx;

            // 1. Chiếu nghỉ (Landing)
            double landingY = oy - (n1 * tPx + landingDPx);
            var landingRect = new Rectangle
            {
                Width = totalWPx,
                Height = landingDPx,
                Fill = ColorLanding,
                Stroke = ColorFlight,
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(landingRect, ox);
            Canvas.SetTop(landingRect, landingY);
            canvas.Children.Add(landingRect);

            // Text "CN: 1,200 mm"
            DrawText(canvas, $"CN: {landingD:N0} mm", ox - 55, landingY + landingDPx / 2 - 8, ColorDim, 10);

            // 2. Vế 1 (Flight 1) - Đi lên
            for (int i = 0; i < n1; i++)
            {
                double stepY = oy - (i + 1) * tPx;
                var stepRect = new Rectangle
                {
                    Width = wPx,
                    Height = tPx,
                    Stroke = ColorFlight,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(stepRect, ox);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);

                DrawText(canvas, (i + 1).ToString(), ox + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            // 3. Vế 2 (Flight 2) - Ngược chiều
            double v2X = ox + wPx + gapPx;
            for (int i = 0; i < n2; i++)
            {
                double stepY = landingY + landingDPx + i * tPx;
                var stepRect = new Rectangle
                {
                    Width = wPx,
                    Height = tPx,
                    Stroke = ColorFlight,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(stepRect, v2X);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);

                DrawText(canvas, (n1 + 1 + i).ToString(), v2X + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            // 4. Đường Dim kích thước vế 1
            DrawDimLine(canvas, ox - 25, oy, ox - 25, oy - n1 * tPx, $"{n1}x{T:0} = {n1 * T:N0} mm");
            DrawDimLine(canvas, ox, oy + 25, ox + wPx, oy + 25, $"{W:N0} mm");

            // Dim vế 2
            double v2EndY = landingY + landingDPx + n2 * tPx;
            DrawDimLine(canvas, v2X + wPx + 25, landingY + landingDPx, v2X + wPx + 25, v2EndY, $"{n2}x{T:0} = {n2 * T:N0} mm");

            // 5. Đường cắt mặt cắt 1-1, 2-2, 3-3
            DrawSectionCutLine(canvas, ox + wPx / 2, oy + 15, ox + wPx / 2, landingY - 15, "1-1");
            DrawSectionCutLine(canvas, ox - 15, landingY + landingDPx / 2, v2X + wPx + 15, landingY + landingDPx / 2, "2-2");
            DrawSectionCutLine(canvas, v2X + wPx / 2, landingY - 15, v2X + wPx / 2, v2EndY + 15, "3-3");
        }

        private static void DrawLShapePlan(
            Canvas canvas, StairLayout layout,
            double ox, double oy, double s,
            double W, double T, double landingD,
            int n1, int n2)
        {
            double wPx = W * s;
            double tPx = T * s;
            double landingDPx = landingD * s;

            // Chiếu nghỉ góc
            double landingY = oy - (n1 * tPx + landingDPx);
            var landingRect = new Rectangle
            {
                Width = landingDPx,
                Height = landingDPx,
                Fill = layout.IsWinder ? ColorWinder : ColorLanding,
                Stroke = ColorFlight,
                StrokeThickness = 1.5
            };
            Canvas.SetLeft(landingRect, ox);
            Canvas.SetTop(landingRect, landingY);
            canvas.Children.Add(landingRect);

            // Bậc rẻ quạt (nếu có winder)
            if (layout.IsWinder)
            {
                int winderCount = layout.WinderStepCount > 0 ? layout.WinderStepCount : 4;
                // Vẽ các nan quạt từ góc xoay
                Point cornerOrigin = new Point(ox + landingDPx, oy - n1 * tPx);
                for (int w = 1; w < winderCount; w++)
                {
                    double angle = (Math.PI / 2.0) * (w / (double)winderCount);
                    double xLine = ox + landingDPx - Math.Sin(angle) * landingDPx;
                    double yLine = landingY + landingDPx - Math.Cos(angle) * landingDPx;

                    var winderLine = new Line
                    {
                        X1 = cornerOrigin.X,
                        Y1 = cornerOrigin.Y,
                        X2 = xLine,
                        Y2 = yLine,
                        Stroke = ColorFlight,
                        StrokeThickness = 1.2
                    };
                    canvas.Children.Add(winderLine);
                }
            }

            // Vế 1 (thẳng đứng lên)
            for (int i = 0; i < n1; i++)
            {
                double stepY = oy - (i + 1) * tPx;
                var stepRect = new Rectangle { Width = wPx, Height = tPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, ox);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (i + 1).ToString(), ox + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            // Vế 2 (nằm ngang sang phải)
            double v2StartX = ox + landingDPx;
            for (int i = 0; i < n2; i++)
            {
                double stepX = v2StartX + i * tPx;
                var stepRect = new Rectangle { Width = tPx, Height = wPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, stepX);
                Canvas.SetTop(stepRect, landingY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (n1 + 1 + i).ToString(), stepX + tPx / 2 - 6, landingY + wPx / 2 - 6, ColorText, 10);
            }

            // Dim
            DrawDimLine(canvas, ox - 25, oy, ox - 25, oy - n1 * tPx, $"{n1}x{T:0} = {n1 * T:N0} mm");
            DrawDimLine(canvas, v2StartX, landingY - 25, v2StartX + n2 * tPx, landingY - 25, $"{n2}x{T:0} = {n2 * T:N0} mm");
            DrawDimLine(canvas, ox, oy + 25, ox + wPx, oy + 25, $"{W:N0} mm");

            // Mặt cắt 1-1 & 2-2
            DrawSectionCutLine(canvas, ox + wPx / 2, oy + 15, ox + wPx / 2, landingY - 15, "1-1");
            DrawSectionCutLine(canvas, ox - 15, landingY + wPx / 2, v2StartX + n2 * tPx + 15, landingY + wPx / 2, "2-2");
        }

        private static void DrawStraightPlan(Canvas canvas, StairLayout layout, double ox, double oy, double s, double W, double T, int n1)
        {
            double wPx = W * s;
            double tPx = T * s;

            for (int i = 0; i < n1; i++)
            {
                double stepY = oy - (i + 1) * tPx;
                var stepRect = new Rectangle { Width = wPx, Height = tPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, ox);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (i + 1).ToString(), ox + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            DrawDimLine(canvas, ox - 25, oy, ox - 25, oy - n1 * tPx, $"{n1}x{T:0} = {n1 * T:N0} mm");
            DrawDimLine(canvas, ox, oy + 25, ox + wPx, oy + 25, $"{W:N0} mm");
            DrawSectionCutLine(canvas, ox + wPx / 2, oy + 15, ox + wPx / 2, oy - n1 * tPx - 15, "1-1");
        }

        private static void DrawThreeFlightUPlan(
            Canvas canvas, StairLayout layout,
            double ox, double oy, double s,
            double W, double T, double landingD, double gap,
            IReadOnlyList<int> risers)
        {
            // Tương tự UShape nhưng có vế giữa nằm ngang nối 2 chiếu nghỉ
            int n1 = risers.Count > 0 ? risers[0] : 8;
            int n2 = risers.Count > 1 ? risers[1] : 6;
            int n3 = risers.Count > 2 ? risers[2] : 8;

            double wPx = W * s;
            double tPx = T * s;
            double landingDPx = landingD * s;
            double v2WidthPx = n2 * tPx;

            double landingY = oy - (n1 * tPx + landingDPx);

            // Chiếu nghỉ 1 (Trái) & Chiếu nghỉ 2 (Phải)
            var cn1 = new Rectangle { Width = landingDPx, Height = landingDPx, Fill = ColorLanding, Stroke = ColorFlight, StrokeThickness = 1.5 };
            Canvas.SetLeft(cn1, ox);
            Canvas.SetTop(cn1, landingY);
            canvas.Children.Add(cn1);

            double v3X = ox + landingDPx + v2WidthPx;
            var cn2 = new Rectangle { Width = landingDPx, Height = landingDPx, Fill = ColorLanding, Stroke = ColorFlight, StrokeThickness = 1.5 };
            Canvas.SetLeft(cn2, v3X);
            Canvas.SetTop(cn2, landingY);
            canvas.Children.Add(cn2);

            // Vế 1 (Đi lên)
            for (int i = 0; i < n1; i++)
            {
                double stepY = oy - (i + 1) * tPx;
                var stepRect = new Rectangle { Width = wPx, Height = tPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, ox);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (i + 1).ToString(), ox + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            // Vế 2 (Giữa, ngang)
            for (int i = 0; i < n2; i++)
            {
                double stepX = ox + landingDPx + i * tPx;
                var stepRect = new Rectangle { Width = tPx, Height = wPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, stepX);
                Canvas.SetTop(stepRect, landingY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (n1 + 1 + i).ToString(), stepX + tPx / 2 - 6, landingY + wPx / 2 - 6, ColorText, 10);
            }

            // Vế 3 (Phải, đi xuống hoặc lên tiếp)
            for (int i = 0; i < n3; i++)
            {
                double stepY = landingY + landingDPx + i * tPx;
                var stepRect = new Rectangle { Width = wPx, Height = tPx, Stroke = ColorFlight, StrokeThickness = 1 };
                Canvas.SetLeft(stepRect, v3X);
                Canvas.SetTop(stepRect, stepY);
                canvas.Children.Add(stepRect);
                DrawText(canvas, (n1 + n2 + 1 + i).ToString(), v3X + wPx / 2 - 5, stepY + tPx / 2 - 6, ColorText, 10);
            }

            // Dims
            DrawDimLine(canvas, ox - 25, oy, ox - 25, oy - n1 * tPx, $"{n1}x{T:0} = {n1 * T:N0} mm");
            DrawDimLine(canvas, ox + landingDPx, landingY - 25, v3X, landingY - 25, $"{n2}x{T:0} = {n2 * T:N0} mm");
            DrawDimLine(canvas, v3X + wPx + 25, landingY + landingDPx, v3X + wPx + 25, landingY + landingDPx + n3 * tPx, $"{n3}x{T:0} = {n3 * T:N0} mm");
        }
        #endregion

        #region Ký hiệu Đồ họa & Dim
        private static void DrawDimLine(Canvas canvas, double x1, double y1, double x2, double y2, string text)
        {
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = ColorDim, StrokeThickness = 1.2
            };
            canvas.Children.Add(line);

            // 2 vạch chặn đầu mút
            bool isVert = Math.Abs(x1 - x2) < 1e-3;
            double tick = 5;
            if (isVert)
            {
                canvas.Children.Add(new Line { X1 = x1 - tick, Y1 = y1, X2 = x1 + tick, Y2 = y1, Stroke = ColorDim, StrokeThickness = 1.2 });
                canvas.Children.Add(new Line { X1 = x2 - tick, Y1 = y2, X2 = x2 + tick, Y2 = y2, Stroke = ColorDim, StrokeThickness = 1.2 });
                // Text ở giữa
                double midY = (y1 + y2) / 2.0;
                DrawText(canvas, text, x1 - 100, midY - 6, ColorDim, 10, HorizontalAlignment.Right);
            }
            else
            {
                canvas.Children.Add(new Line { X1 = x1, Y1 = y1 - tick, X2 = x1, Y2 = y1 + tick, Stroke = ColorDim, StrokeThickness = 1.2 });
                canvas.Children.Add(new Line { X1 = x2, Y1 = y2 - tick, X2 = x2, Y2 = y2 + tick, Stroke = ColorDim, StrokeThickness = 1.2 });
                double midX = (x1 + x2) / 2.0;
                DrawText(canvas, text, midX - 40, y1 + 6, ColorDim, 10);
            }
        }

        private static void DrawSectionCutLine(Canvas canvas, double x1, double y1, double x2, double y2, string label)
        {
            var line = new Line
            {
                X1 = x1, Y1 = y1, X2 = x2, Y2 = y2,
                Stroke = ColorSectionLine,
                StrokeThickness = 1.5,
                StrokeDashArray = new DoubleCollection { 8, 4 }
            };
            canvas.Children.Add(line);

            // Huy hiệu tròn 2 đầu
            DrawSectionBadge(canvas, x1, y1, label);
            DrawSectionBadge(canvas, x2, y2, label);
        }

        private static void DrawSectionBadge(Canvas canvas, double cx, double cy, string label)
        {
            double r = 10;
            var circle = new Ellipse
            {
                Width = r * 2, Height = r * 2,
                Fill = ColorSectionBadge,
                Stroke = Brushes.White,
                StrokeThickness = 1
            };
            Canvas.SetLeft(circle, cx - r);
            Canvas.SetTop(circle, cy - r);
            canvas.Children.Add(circle);

            var text = new TextBlock
            {
                Text = label,
                Foreground = Brushes.White,
                FontSize = 9,
                FontWeight = FontWeights.Normal,
                TextAlignment = TextAlignment.Center,
                Width = r * 2
            };
            Canvas.SetLeft(text, cx - r);
            Canvas.SetTop(text, cy - 6);
            canvas.Children.Add(text);
        }

        private static void DrawText(Canvas canvas, string text, double x, double y, Brush color, double fontSize, HorizontalAlignment align = HorizontalAlignment.Left)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontSize = fontSize,
                HorizontalAlignment = align
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }
        #endregion
    }
}
