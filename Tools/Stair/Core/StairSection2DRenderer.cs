using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JNN.Stair.Models;

namespace JNN.Stair.Core
{
    /// <summary>
    /// Bộ vẽ vector 2D chuyên nghiệp cho Mặt Cắt Dọc & Cấu Tạo Bản Thang (Tab 2)
    /// </summary>
    public static class StairSection2DRenderer
    {
        // Bảng màu cấu tạo bản thang chuẩn hiển thị trên nền sáng
        private static readonly Brush ColorConcrete = new SolidColorBrush(Color.FromRgb(2, 132, 199));     // Blue (#0284C7)
        private static readonly Brush ColorMortar = new SolidColorBrush(Color.FromRgb(217, 119, 6));       // Cam đất (#D97706)
        private static readonly Brush ColorBrickStep = new SolidColorBrush(Color.FromRgb(124, 58, 237));   // Purple (#7C3AED)
        private static readonly Brush ColorDim = new SolidColorBrush(Color.FromRgb(217, 119, 6));         // Cam Dim
        private static readonly Brush ColorStepNumber = new SolidColorBrush(Color.FromRgb(30, 41, 59));    // Số thứ tự bậc xám đậm rõ nét

        public static void RenderSection(Canvas canvas, StairLayout layout, int sectionIndex = 1)
        {
            canvas.Children.Clear();
            if (canvas.ActualWidth <= 0 || canvas.ActualHeight <= 0) return;

            double canvasW = canvas.ActualWidth;
            double canvasH = canvas.ActualHeight;

            var risers = layout.GetRisersPerFlight();
            int n = sectionIndex switch
            {
                2 when risers.Count > 1 => risers[1],
                3 when risers.Count > 2 => risers[2],
                _ => risers[0]
            };

            double b = layout.TreadMm;
            double h = layout.ActualRiserMm;
            double waist = layout.WaistThicknessMm;
            double mortar = layout.MortarThicknessMm;
            double landingD = layout.LandingDepthMm;

            // Tổng chiều dài và chiều cao mặt cắt
            double totalLenMm = landingD + (n * b) + 800; // Chiếu nghỉ + vế + đoạn sàn tới
            double totalHeightMm = n * h + 400;

            double margin = 70;
            double scale = Math.Min((canvasW - 2 * margin) / totalLenMm, (canvasH - 2 * margin) / totalHeightMm);
            if (scale <= 0) scale = 0.1;

            double bPx = b * scale;
            double hPx = h * scale;
            double waistPx = waist * scale;
            double mortarPx = Math.Max(2, mortar * scale);
            double landingDPx = landingD * scale;

            // Tọa độ xuất phát (chân chiếu nghỉ hoặc chân vế)
            double startX = margin + 30;
            double startY = canvasH - margin - 30;

            // 1. Vẽ đoạn phẳng chân thang (sàn dưới)
            double footLenPx = 250 * scale;
            var concretePoints = new PointCollection();
            concretePoints.Add(new Point(startX, startY));
            concretePoints.Add(new Point(startX + footLenPx, startY));

            // 2. Vẽ dải vế nghiêng (Zic-zac mặt bậc)
            double currX = startX + footLenPx;
            double currY = startY;

            int startStepNumber = sectionIndex switch
            {
                2 => (risers.Count > 0 ? risers[0] : 10) + 1,
                3 => (risers.Count > 1 ? risers[0] + risers[1] : 20) + 1,
                _ => 1
            };

            for (int i = 0; i < n; i++)
            {
                double nextY = currY - hPx;
                double nextX = currX + bPx;

                // Khối bậc gạch xây (Purple)
                var brickPolygon = new Polygon
                {
                    Fill = ColorBrickStep,
                    Stroke = Brushes.Transparent
                };
                brickPolygon.Points.Add(new Point(currX, currY));
                brickPolygon.Points.Add(new Point(currX, nextY));
                brickPolygon.Points.Add(new Point(nextX, nextY));
                canvas.Children.Add(brickPolygon);

                // Số thứ tự bậc phía trên mặt bậc
                DrawText(canvas, (startStepNumber + i).ToString(), currX + bPx / 2 - 5, nextY - 14, ColorStepNumber, 9);

                currX = nextX;
                currY = nextY;
            }

            // 3. Vẽ đoạn chiếu nghỉ phẳng ở trên đỉnh
            double landingEndX = currX + landingDPx;
            var landingTopPolygon = new Polygon
            {
                Fill = ColorConcrete,
                Stroke = ColorConcrete
            };

            // Vẽ khối bê tông bản nghiêng (Cyan) dưới các bậc
            // Mặt đáy nghiêng song song cách mặt bậc một đoạn waist
            double angle = Math.Atan2(h, b);
            double waistNormalPx = waistPx / Math.Cos(angle);

            var slabPolygon = new Polygon
            {
                Fill = ColorConcrete,
                Stroke = Brushes.Transparent
            };

            // Viền trên bê tông
            slabPolygon.Points.Add(new Point(startX, startY));
            slabPolygon.Points.Add(new Point(startX + footLenPx, startY));
            for (int i = 0; i < n; i++)
            {
                double stepX = startX + footLenPx + i * bPx;
                double stepY = startY - i * hPx;
                slabPolygon.Points.Add(new Point(stepX, stepY));
                slabPolygon.Points.Add(new Point(stepX + bPx, stepY - hPx));
            }
            slabPolygon.Points.Add(new Point(landingEndX, currY));
            slabPolygon.Points.Add(new Point(landingEndX, currY + waistPx));

            // Viền dưới nghiêng
            slabPolygon.Points.Add(new Point(currX, currY + waistNormalPx));
            slabPolygon.Points.Add(new Point(startX + footLenPx, startY + waistNormalPx));
            slabPolygon.Points.Add(new Point(startX, startY + waistPx));

            canvas.Children.Add(slabPolygon);

            // 4. Lớp vữa lót (Mortar Bedding) màu cam chạy dọc theo bề mặt bê tông
            var mortarLine = new Polyline
            {
                Stroke = ColorMortar,
                StrokeThickness = 2.5
            };
            mortarLine.Points.Add(new Point(startX + footLenPx, startY));
            for (int i = 0; i < n; i++)
            {
                double stepX = startX + footLenPx + i * bPx;
                double stepY = startY - i * hPx;
                mortarLine.Points.Add(new Point(stepX, stepY));
                mortarLine.Points.Add(new Point(stepX + bPx, stepY - hPx));
            }
            mortarLine.Points.Add(new Point(landingEndX, currY));
            canvas.Children.Add(mortarLine);

            // 5. Ghi các đường Dim kích thước
            double dimStepX = startX + footLenPx;
            double dimStepY = startY;

            // Dim b = 300 mm
            DrawDimension(canvas, dimStepX, dimStepY - 15, dimStepX + bPx, dimStepY - 15, $"b = {b:0} mm", isHorizontal: true);

            // Dim h = ... mm
            DrawDimension(canvas, dimStepX - 15, dimStepY, dimStepX - 15, dimStepY - hPx, $"h = {h:F1} mm", isHorizontal: false);

            // Dim BTCT = 150 mm
            double midSlabX = startX + footLenPx + (n / 2) * bPx;
            double midSlabY = startY - (n / 2) * hPx;
            DrawDimension(canvas, midSlabX, midSlabY, midSlabX, midSlabY + waistNormalPx, $"BTCT = {waist:0} mm", isHorizontal: false);
        }

        private static void DrawDimension(Canvas canvas, double x1, double y1, double x2, double y2, string text, bool isHorizontal)
        {
            var line = new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = ColorDim, StrokeThickness = 1.2 };
            canvas.Children.Add(line);

            double tick = 4;
            if (isHorizontal)
            {
                canvas.Children.Add(new Line { X1 = x1, Y1 = y1 - tick, X2 = x1, Y2 = y1 + tick, Stroke = ColorDim, StrokeThickness = 1.2 });
                canvas.Children.Add(new Line { X1 = x2, Y1 = y2 - tick, X2 = x2, Y2 = y2 + tick, Stroke = ColorDim, StrokeThickness = 1.2 });
                DrawText(canvas, text, (x1 + x2) / 2 - 35, y1 - 16, ColorDim, 10);
            }
            else
            {
                canvas.Children.Add(new Line { X1 = x1 - tick, Y1 = y1, X2 = x1 + tick, Y2 = y1, Stroke = ColorDim, StrokeThickness = 1.2 });
                canvas.Children.Add(new Line { X1 = x2 - tick, Y1 = y2, X2 = x2 + tick, Y2 = y2, Stroke = ColorDim, StrokeThickness = 1.2 });
                DrawText(canvas, text, x1 - 85, (y1 + y2) / 2 - 6, ColorDim, 10);
            }
        }

        private static void DrawText(Canvas canvas, string text, double x, double y, Brush color, double fontSize)
        {
            var tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontSize = fontSize
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            canvas.Children.Add(tb);
        }
    }
}
