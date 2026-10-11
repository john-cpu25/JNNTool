using System;
using System.Collections.Generic;
using JNN.Stair.Models;

namespace JNN.Stair.Core
{
    /// <summary>
    /// Thuật toán uốn và sinh đường sinh cốt thép cầu thang (Clean-room Rebar Engine)
    /// Đảm bảo tuyệt đối nguyên tắc kết cấu: Đan chéo cốt thép chịu kéo tại góc lõm chống bung bê tông.
    /// </summary>
    public static class StairRebarCurveBuilder
    {
        /// <summary>
        /// Tạo các thanh thép dọc lớp dưới (Bottom Bars) men theo đáy bản thang
        /// </summary>
        /// <param name="bottomSoffitPoints">Các điểm đáy bản thang (đã offset lớp bảo vệ)</param>
        /// <param name="barDiameterMm">Đường kính thép</param>
        /// <param name="anchorLengthMm">Chiều dài neo tối thiểu (30d ~ 40d)</param>
        /// <param name="coverMm">Lớp bảo vệ bê tông</param>
        /// <returns>Danh sách các thanh thép dọc hoàn chỉnh</returns>
        public static List<StairRebarData> BuildBottomRebars(
            IReadOnlyList<StairPoint3D> bottomSoffitPoints,
            double barDiameterMm,
            double anchorLengthMm,
            double coverMm)
        {
            var result = new List<StairRebarData>();
            if (bottomSoffitPoints == null || bottomSoffitPoints.Count < 2) return result;

            // Duyệt qua các góc gập
            // Nếu phát hiện góc lõm (Concave): Cắt đôi và kéo dài đan chéo sang nhau
            var currentPoints = new List<StairPoint3D> { bottomSoffitPoints[0] };

            for (int i = 1; i < bottomSoffitPoints.Count - 1; i++)
            {
                StairPoint3D prev = bottomSoffitPoints[i - 1];
                StairPoint3D curr = bottomSoffitPoints[i];
                StairPoint3D next = bottomSoffitPoints[i + 1];

                CornerType corner = DetectCornerType(prev, curr, next);

                if (corner == CornerType.Concave)
                {
                    // ⚠️ GÓC LÕM: Thanh hiện tại tiếp tục đâm xuyên qua góc theo hướng cũ một đoạn anchor
                    StairPoint3D dirPrev = Normalize(curr - prev);
                    StairPoint3D extendedPoint = curr + dirPrev * anchorLengthMm;

                    currentPoints.Add(curr);
                    currentPoints.Add(extendedPoint);

                    // Đóng gói thanh thứ nhất
                    result.Add(CreateRebar(currentPoints, StairRebarLayer.BottomMain, barDiameterMm, "Thép lớp dưới - Vế dưới"));

                    // Bắt đầu thanh thứ hai: Lấy lùi về sau góc theo hướng vế kế tiếp một đoạn anchor
                    StairPoint3D dirNext = Normalize(next - curr);
                    StairPoint3D startOverlappedPoint = curr - dirNext * anchorLengthMm;

                    currentPoints = new List<StairPoint3D>
                    {
                        startOverlappedPoint,
                        curr
                    };
                }
                else
                {
                    // Góc lồi hoặc phẳng: Thép uốn liên tục bình thường
                    currentPoints.Add(curr);
                }
            }

            // Thêm điểm cuối cùng
            currentPoints.Add(bottomSoffitPoints[bottomSoffitPoints.Count - 1]);
            result.Add(CreateRebar(currentPoints, StairRebarLayer.BottomMain, barDiameterMm, "Thép lớp dưới - Vế trên"));

            return result;
        }

        /// <summary>
        /// Tạo thép mũ gối (Top Rebar / Hat Rebar) tại vùng mô-men âm
        /// </summary>
        public static List<StairRebarData> BuildTopHatRebars(
            StairPoint3D supportCenter,
            StairPoint3D spanDirection,
            double spanLengthMm,
            double ratioL, // Mặc định L/4 = 0.25
            double barDiameterMm,
            double hookDownMm = 120.0)
        {
            var result = new List<StairRebarData>();

            double reach = spanLengthMm * ratioL;
            reach = Math.Ceiling(reach / 50.0) * 50.0; // Làm tròn bội số 50mm

            StairPoint3D pStart = supportCenter;
            StairPoint3D pEnd = supportCenter + Normalize(spanDirection) * reach;

            // Bẻ giò gà cụp xuống ở đầu tự do
            StairPoint3D hookPoint = pEnd - new StairPoint3D(0, 0, hookDownMm);

            var rebar = new StairRebarData
            {
                Name = $"Thép mũ gối L/{1 / ratioL:0}",
                Layer = StairRebarLayer.TopMain,
                BarDiameterMm = barDiameterMm,
                CenterlinePoints = new List<StairPoint3D> { pStart, pEnd, hookPoint },
                HasEndAnchor = true,
                EndAnchorLengthMm = hookDownMm
            };

            result.Add(rebar);
            return result;
        }

        /// <summary>
        /// Sinh các điểm đặt thép phân bố (Distribution Bars) vuông góc với vế thang
        /// </summary>
        public static List<StairPoint3D> CalculateDistributionBarLocations(
            IReadOnlyList<StairPoint3D> rebarPath,
            double spacingMm,
            double offsetFromMainBarMm)
        {
            var locs = new List<StairPoint3D>();
            if (rebarPath == null || rebarPath.Count < 2) return locs;

            for (int i = 0; i < rebarPath.Count - 1; i++)
            {
                StairPoint3D p1 = rebarPath[i];
                StairPoint3D p2 = rebarPath[i + 1];
                double segLen = p1.DistanceTo(p2);
                if (segLen < spacingMm) continue;

                int count = (int)(segLen / spacingMm);
                StairPoint3D dir = Normalize(p2 - p1);

                for (int j = 1; j <= count; j++)
                {
                    StairPoint3D pt = p1 + dir * (j * spacingMm);
                    // Dịch lên trên thanh thép chính một khoảng offset
                    pt.Z += offsetFromMainBarMm;
                    locs.Add(pt);
                }
            }

            return locs;
        }

        #region Helper Methods
        private static CornerType DetectCornerType(StairPoint3D p1, StairPoint3D p2, StairPoint3D p3)
        {
            // Vector 2D trên mặt cắt đứng (X, Z)
            double dx1 = p2.X - p1.X;
            double dz1 = p2.Z - p1.Z;
            double dx2 = p3.X - p2.X;
            double dz2 = p3.Z - p2.Z;

            // Tích có hướng 2D: Cross Product = dx1 * dz2 - dz1 * dx2
            double cross = dx1 * dz2 - dz1 * dx2;

            if (Math.Abs(cross) < 1e-4) return CornerType.Flat;

            // Nếu hướng uốn ngược với chiều dốc -> góc lõm
            return cross > 0 ? CornerType.Concave : CornerType.Convex;
        }

        private static StairPoint3D Normalize(StairPoint3D v)
        {
            double len = Math.Sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z);
            return len < 1e-6 ? new StairPoint3D(0, 0, 0) : new StairPoint3D(v.X / len, v.Y / len, v.Z / len);
        }

        private static StairRebarData CreateRebar(List<StairPoint3D> points, StairRebarLayer layer, double diam, string name)
        {
            return new StairRebarData
            {
                Name = name,
                Layer = layer,
                BarDiameterMm = diam,
                CenterlinePoints = new List<StairPoint3D>(points)
            };
        }
        #endregion
    }
}
