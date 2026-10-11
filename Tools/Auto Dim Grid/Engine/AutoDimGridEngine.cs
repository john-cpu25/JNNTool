using System;
using System.Collections.Generic;
using System.Linq;

namespace JNNTool.Tools.AutoDimGrid.Engine
{
    /// <summary>
    /// Phần tính toán thuần C# (không gọi Revit API) của Auto Dim Grid để unit test được.
    /// Đơn vị tùy ý nhưng phải nhất quán giữa các tham số.
    /// </summary>
    public static class AutoDimGridEngine
    {
        /// <summary>
        /// Trả về chỉ số đã sắp tăng dần theo hình chiếu, bỏ các điểm trùng (|Δ| &lt; tolerance)
        /// vì 2 tham chiếu trùng vị trí sẽ tạo đoạn Dim 0 và làm Revit từ chối cả chuỗi.
        /// </summary>
        public static List<int> SortAndDedup(IReadOnlyList<double> projections, double tolerance)
        {
            var result = new List<int>();
            if (projections == null || projections.Count == 0) return result;

            var order = Enumerable.Range(0, projections.Count).OrderBy(i => projections[i]);
            foreach (int i in order)
            {
                if (result.Count == 0 || projections[i] - projections[result[result.Count - 1]] >= tolerance)
                    result.Add(i);
            }
            return result;
        }

        /// <summary>Khoảng cách trên bản in (mm) → khoảng cách thật trong mô hình (mm).</summary>
        public static double PaperToModelMm(double paperMm, int viewScale)
        {
            return paperMm * Math.Max(1, viewScale);
        }

        /// <summary>
        /// Vị trí từng lớp Dim dọc theo phương trục, thứ tự từ trong ra ngoài (lớp 0 gần công trình nhất).
        /// </summary>
        /// <param name="anchor">Mốc: trục vuông góc ngoài cùng, hoặc đầu trục.</param>
        /// <param name="sideSign">+1 phía max, -1 phía min.</param>
        /// <param name="insideFromEndpoint">
        /// true: mốc là đầu trục, lớp ngoài cùng nằm vào trong mốc một khoảng firstOffset.
        /// false: lớp trong cùng nằm ra ngoài mốc một khoảng firstOffset.
        /// </param>
        public static double[] LayerPositions(double anchor, int layerCount, double firstOffset,
            double betweenOffset, int sideSign, bool insideFromEndpoint)
        {
            if (layerCount <= 0) return new double[0];

            int s = sideSign >= 0 ? 1 : -1;
            var pos = new double[layerCount];
            for (int k = 0; k < layerCount; k++)
            {
                pos[k] = insideFromEndpoint
                    ? anchor - s * (firstOffset + (layerCount - 1 - k) * betweenOffset)
                    : anchor + s * (firstOffset + k * betweenOffset);
            }
            return pos;
        }
    }
}
