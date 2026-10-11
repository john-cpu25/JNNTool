#nullable enable
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using JNNTool.Tools.StairModel.Engine;

namespace JNNTool.Tools.StairModel.Views.Controls
{
    /// <summary>
    /// Canvas vẽ đồ họa mặt bằng 2D tương tác trực quan thời gian thực cho thang kết cấu bê tông.
    /// Tự động căn giữa, phóng to/thu nhỏ (auto-scale) theo kích thước khung nhìn.
    /// </summary>
    public class StairPreviewControl : Canvas
    {
        public StairPreviewControl()
        {
            Background = new SolidColorBrush(Color.FromRgb(250, 251, 253));
            ClipToBounds = true;
            SizeChanged += (s, e) => InvalidateVisual();
        }

        public void RenderPreview(StairGeometryResult? result)
        {
            Children.Clear();

            if (result == null || !result.Success || (result.Flights.Count == 0 && result.Landings.Count == 0))
            {
                var textBlock = new TextBlock
                {
                    Text = result?.ErrorMessage ?? "Chưa có dữ liệu xem trước",
                    Foreground = new SolidColorBrush(Color.FromRgb(140, 150, 160)),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                SetLeft(textBlock, Math.Max((ActualWidth - 180) / 2.0, 10));
                SetTop(textBlock, Math.Max((ActualHeight - 20) / 2.0, 10));
                Children.Add(textBlock);
                return;
            }

            double width = ActualWidth > 20 ? ActualWidth : 380;
            double height = ActualHeight > 20 ? ActualHeight : 380;

            // 1. Tính toán Bounding Box của tất cả các đỉnh
            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;

            void UpdateBounds(StairPoint3D pt)
            {
                if (pt.X < minX) minX = pt.X;
                if (pt.X > maxX) maxX = pt.X;
                if (pt.Y < minY) minY = pt.Y;
                if (pt.Y > maxY) maxY = pt.Y;
            }

            foreach (var flight in result.Flights)
            {
                foreach (var pt in flight.ProfilePoints) UpdateBounds(pt);
                UpdateBounds(flight.SlopeArrowStart);
                UpdateBounds(flight.SlopeArrowEnd);
            }

            foreach (var landing in result.Landings)
            {
                foreach (var pt in landing.ProfilePoints) UpdateBounds(pt);
            }

            foreach (var beam in result.Beams)
            {
                UpdateBounds(beam.StartPoint);
                UpdateBounds(beam.EndPoint);
            }

            double rangeX = Math.Max(maxX - minX, 100.0);
            double rangeY = Math.Max(maxY - minY, 100.0);

            // Tỉ lệ scale fit khung Canvas có margin 40px
            double margin = 45.0;
            double availW = Math.Max(width - 2 * margin, 50.0);
            double availH = Math.Max(height - 2 * margin, 50.0);
            double scale = Math.Min(availW / rangeX, availH / rangeY);

            // Tọa độ tâm
            double midModelX = (minX + maxX) / 2.0;
            double midModelY = (minY + maxY) / 2.0;
            double midCanvasX = width / 2.0;
            double midCanvasY = height / 2.0;

            Point ToCanvas(StairPoint3D pt)
            {
                double cx = midCanvasX + (pt.X - midModelX) * scale;
                // Đảo chiều Y để trục Y hướng lên trên theo hệ tọa độ chuẩn
                double cy = midCanvasY - (pt.Y - midModelY) * scale;
                return new Point(cx, cy);
            }

            // 2. Vẽ Landings (Chiếu nghỉ) trước (nằm phía dưới)
            foreach (var landing in result.Landings)
            {
                var poly = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromArgb(50, 46, 117, 182)),
                    Stroke = new SolidColorBrush(Color.FromRgb(46, 117, 182)),
                    StrokeThickness = 1.5
                };
                foreach (var pt in landing.ProfilePoints)
                {
                    poly.Points.Add(ToCanvas(pt));
                }
                Children.Add(poly);

                // Label chiếu nghỉ
                if (landing.ProfilePoints.Count >= 4)
                {
                    var p0 = ToCanvas(landing.ProfilePoints[0]);
                    var p2 = ToCanvas(landing.ProfilePoints[2]);
                    var lbl = new TextBlock
                    {
                        Text = $"{landing.Name}\n{landing.LengthMm:F0} × {landing.WidthMm:F0}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.FromRgb(41, 128, 185)),
                        TextAlignment = TextAlignment.Center
                    };
                    SetLeft(lbl, (p0.X + p2.X) / 2.0 - 50);
                    SetTop(lbl, (p0.Y + p2.Y) / 2.0 - 15);
                    Children.Add(lbl);
                }
            }

            // 3. Vẽ Flights (Vế thang)
            foreach (var flight in result.Flights)
            {
                var poly = new Polygon
                {
                    Fill = new SolidColorBrush(Color.FromArgb(40, 52, 73, 94)),
                    Stroke = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                    StrokeThickness = 1.5
                };
                foreach (var pt in flight.ProfilePoints)
                {
                    poly.Points.Add(ToCanvas(pt));
                }
                Children.Add(poly);

                // Vẽ các vạch chia mặt bậc (Tread Riser lines)
                if (flight.ProfilePoints.Count >= 4 && flight.RiserCount > 1)
                {
                    var p0 = ToCanvas(flight.ProfilePoints[0]);
                    var p1 = ToCanvas(flight.ProfilePoints[1]);
                    var p2 = ToCanvas(flight.ProfilePoints[2]);
                    var p3 = ToCanvas(flight.ProfilePoints[3]);

                    int steps = flight.RiserCount - 1;
                    for (int s = 1; s <= steps; s++)
                    {
                        double frac = (double)s / flight.RiserCount;
                        double xA = p0.X + (p1.X - p0.X) * frac;
                        double yA = p0.Y + (p1.Y - p0.Y) * frac;
                        double xB = p3.X + (p2.X - p3.X) * frac;
                        double yB = p3.Y + (p2.Y - p3.Y) * frac;

                        var stepLine = new Line
                        {
                            X1 = xA,
                            Y1 = yA,
                            X2 = xB,
                            Y2 = yB,
                            Stroke = new SolidColorBrush(Color.FromArgb(100, 44, 62, 80)),
                            StrokeThickness = 0.8
                        };
                        Children.Add(stepLine);
                    }
                }

                // 4. Vẽ Slope Arrow (Mũi tên dốc chỉ hướng LÊN)
                var startPt = ToCanvas(flight.SlopeArrowStart);
                var endPt = ToCanvas(flight.SlopeArrowEnd);

                if (Point.Subtract(endPt, startPt).Length > 10)
                {
                    var arrowLine = new Line
                    {
                        X1 = startPt.X,
                        Y1 = startPt.Y,
                        X2 = endPt.X,
                        Y2 = endPt.Y,
                        Stroke = new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                        StrokeThickness = 2.0,
                        StrokeDashArray = new DoubleCollection { 4, 2 }
                    };
                    Children.Add(arrowLine);

                    // Mũi tên nhọn ở đỉnh (head)
                    Vector dir = endPt - startPt;
                    dir.Normalize();
                    Vector normal = new Vector(-dir.Y, dir.X);

                    double arrowLen = 10.0;
                    double arrowW = 5.0;
                    Point tip = endPt;
                    Point base1 = endPt - dir * arrowLen + normal * arrowW;
                    Point base2 = endPt - dir * arrowLen - normal * arrowW;

                    var arrowHead = new Polygon
                    {
                        Fill = new SolidColorBrush(Color.FromRgb(230, 126, 34)),
                        Points = new PointCollection { tip, base1, base2 }
                    };
                    Children.Add(arrowHead);

                    // Label UP / LÊN
                    var upLbl = new TextBlock
                    {
                        Text = $"UP ({flight.RiserCount}B)",
                        FontSize = 10,
                        FontWeight = FontWeights.Normal,
                        Foreground = new SolidColorBrush(Color.FromRgb(211, 84, 0))
                    };
                    SetLeft(upLbl, (startPt.X + endPt.X) / 2.0 + normal.X * 12 - 15);
                    SetTop(upLbl, (startPt.Y + endPt.Y) / 2.0 + normal.Y * 12 - 8);
                    Children.Add(upLbl);
                }
            }

            // 5. Vẽ Support Beams (Dầm đỡ kết cấu nét đứt màu đỏ cam)
            foreach (var beam in result.Beams)
            {
                var bp1 = ToCanvas(beam.StartPoint);
                var bp2 = ToCanvas(beam.EndPoint);

                var beamLine = new Line
                {
                    X1 = bp1.X,
                    Y1 = bp1.Y,
                    X2 = bp2.X,
                    Y2 = bp2.Y,
                    Stroke = new SolidColorBrush(Color.FromRgb(192, 57, 43)),
                    StrokeThickness = 3.0,
                    StrokeDashArray = new DoubleCollection { 3, 2 }
                };
                Children.Add(beamLine);

                // Điểm nút tròn ở 2 đầu dầm
                var dot1 = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(192, 57, 43))
                };
                SetLeft(dot1, bp1.X - 3);
                SetTop(dot1, bp1.Y - 3);
                Children.Add(dot1);

                var dot2 = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = new SolidColorBrush(Color.FromRgb(192, 57, 43))
                };
                SetLeft(dot2, bp2.X - 3);
                SetTop(dot2, bp2.Y - 3);
                Children.Add(dot2);
            }

            // 6. Chú giải kích thước tổng thể
            var dimText = new TextBlock
            {
                Text = $"L={rangeX:F0} mm | W={rangeY:F0} mm | H={result.TotalRiseMm:F0} mm",
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(127, 140, 141))
            };
            SetLeft(dimText, 10);
            SetTop(dimText, height - 20);
            Children.Add(dimText);
        }
    }
}
