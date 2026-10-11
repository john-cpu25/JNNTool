using System;
using System.Collections.Generic;
using System.Linq;

namespace JNNTool.Tools.AutoDimColumn.Engine
{
    /// <summary>
    /// Thuần C# Engine tính toán hình học, khoảng cách, góc xoay, xếp hạng Grid và tọa độ đường Dim.
    /// Không phụ thuộc trực tiếp vào Revit API nên có thể unit test 100%.
    /// </summary>
    public static class AutoDimColumnEngine
    {
        public const double MmPerFoot = 304.8;
        public const double RadToDeg = 180.0 / Math.PI;
        public const double DegToRad = Math.PI / 180.0;

        public static double MmToFt(double mm) => mm / MmPerFoot;
        public static double FtToMm(double ft) => ft * MmPerFoot;

        // ─────────────────────────────────────────────────────────────
        // 2D Vector Math
        // ─────────────────────────────────────────────────────────────
        public static double DotProduct2D(double ax, double ay, double bx, double by)
            => (ax * bx) + (ay * by);

        public static double CrossProduct2D(double ax, double ay, double bx, double by)
            => (ax * by) - (ay * bx);

        public static (double x, double y) Normalize2D(double x, double y)
        {
            double len = Math.Sqrt((x * x) + (y * y));
            if (len < 1e-9) return (1.0, 0.0);
            return (x / len, y / len);
        }

        public static double AngleBetweenDeg(double ax, double ay, double bx, double by)
        {
            var (nax, nay) = Normalize2D(ax, ay);
            var (nbx, nby) = Normalize2D(bx, by);
            double dot = Math.Max(-1.0, Math.Min(1.0, DotProduct2D(nax, nay, nbx, nby)));
            return Math.Acos(dot) * RadToDeg;
        }

        /// <summary>
        /// Kiểm tra hai vector có song song (hoặc đối song song) trong khoảng dung sai góc cho phép.
        /// </summary>
        public static bool IsParallel(double ax, double ay, double bx, double by, double toleranceDeg = 5.0)
        {
            double angle = AngleBetweenDeg(ax, ay, bx, by);
            return angle <= toleranceDeg || Math.Abs(angle - 180.0) <= toleranceDeg;
        }

        /// <summary>
        /// Tính khoảng cách vuông góc từ một điểm (px, py) tới đường thẳng đi qua (x0, y0) với vector chỉ phương (dx, dy).
        /// </summary>
        public static double PerpendicularDistance(double px, double py, double x0, double y0, double dx, double dy)
        {
            var (ndx, ndy) = Normalize2D(dx, dy);
            double vx = px - x0;
            double vy = py - y0;
            // Khoảng cách là độ lớn tích có hướng 2D với vector đơn vị chỉ phương
            return Math.Abs(CrossProduct2D(vx, vy, ndx, ndy));
        }

        /// <summary>
        /// Chiếu điểm (px, py) lên đường thẳng đi qua (x0, y0) với vector chỉ phương (dx, dy).
        /// Trả về khoảng cách có dấu dọc theo vector (dx, dy).
        /// </summary>
        public static double ProjectAlong(double px, double py, double x0, double y0, double dx, double dy)
        {
            var (ndx, ndy) = Normalize2D(dx, dy);
            return DotProduct2D(px - x0, py - y0, ndx, ndy);
        }

        // ─────────────────────────────────────────────────────────────
        // Dimension Placement Calculations
        // ─────────────────────────────────────────────────────────────
        /// <summary>
        /// Tính tọa độ 2 đầu của Dimension Line (Line.CreateBound).
        /// Đường dimension line có hướng song song với vector đo lường (measureDirX, measureDirY),
        /// và bị dịch chuyển ra ngoài theo vector offsetDir (offsetDirX, offsetDirY) một khoảng offsetDist.
        /// </summary>
        public static (double startX, double startY, double endX, double endY) CalculateDimLine2D(
            double centerX, double centerY,
            double measureDirX, double measureDirY,
            double offsetDirX, double offsetDirY,
            double offsetDist,
            double minExtent, double maxExtent,
            double margin = 0.5)
        {
            var (mdx, mdy) = Normalize2D(measureDirX, measureDirY);
            var (odx, ody) = Normalize2D(offsetDirX, offsetDirY);

            // Điểm neo trên đường dim
            double anchorX = centerX + (odx * offsetDist);
            double anchorY = centerY + (ody * offsetDist);

            // Đảm bảo đường dim bao phủ từ minExtent - margin đến maxExtent + margin
            double p1 = minExtent - margin;
            double p2 = maxExtent + margin;

            double sx = anchorX + (mdx * p1);
            double sy = anchorY + (mdy * p1);

            double ex = anchorX + (mdx * p2);
            double ey = anchorY + (mdy * p2);

            return (sx, sy, ex, ey);
        }

        // ─────────────────────────────────────────────────────────────
        // Grid Candidate Ranking
        // ─────────────────────────────────────────────────────────────
        public class GridCandidateDto
        {
            public string Name { get; set; } = string.Empty;
            public double DistanceMm { get; set; }
            public double AngleDiffDeg { get; set; }
            public bool IsAccepted { get; set; }
            public double Score { get; set; }
        }

        /// <summary>
        /// Lọc và xếp hạng danh sách Grid theo mức độ phù hợp với mặt cột.
        /// Ưu tiên Grid song song gần nhất và nằm trong phạm vi tìm kiếm.
        /// </summary>
        public static List<GridCandidateDto> RankGridCandidates(
            IEnumerable<(string name, double distMm, double angleDiffDeg)> rawGrids,
            double searchRangeMm = 2500.0,
            double angleTolDeg = 5.0)
        {
            var results = new List<GridCandidateDto>();

            foreach (var g in rawGrids)
            {
                bool isParallel = g.angleDiffDeg <= angleTolDeg || Math.Abs(g.angleDiffDeg - 180.0) <= angleTolDeg;
                bool inRange = g.distMm <= searchRangeMm;

                double score = double.MaxValue;
                if (isParallel && inRange)
                {
                    // Điểm càng thấp càng ưu tiên (khoảng cách + phạt góc lệch)
                    score = g.distMm + (Math.Min(g.angleDiffDeg, Math.Abs(g.angleDiffDeg - 180.0)) * 50.0);
                }

                results.Add(new GridCandidateDto
                {
                    Name = g.name,
                    DistanceMm = g.distMm,
                    AngleDiffDeg = g.angleDiffDeg,
                    IsAccepted = isParallel && inRange,
                    Score = score
                });
            }

            return results
                .Where(r => r.IsAccepted)
                .OrderBy(r => r.Score)
                .ToList();
        }

        /// <summary>
        /// Tạo chữ ký duy nhất cho tập hợp References để so sánh chống trùng.
        /// </summary>
        public static string BuildDimSignature(IEnumerable<string> refStableKeys, int orientationFlag)
        {
            var sorted = refStableKeys.OrderBy(k => k).ToList();
            return $"{orientationFlag}:{string.Join("|", sorted)}";
        }
    }
}
