#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.Tools.ElementMark.Models;

namespace JNNTool.Tools.ElementMark.Engine
{
    /// <summary>
    /// Tính Mark cho Dầm / Cột / Sàn. Thuần C#, toạ độ mm trên hệ trục view
    /// (X tăng sang phải, Y tăng lên trên màn hình).
    /// </summary>
    public static class ElementMarkEngine
    {
        // ═══════════════════════════════════════════════════════════════
        //  Dầm
        // ═══════════════════════════════════════════════════════════════

        /// <summary>Góc ≤ 45° so với trục ngang → X, &gt; 45° → Y.</summary>
        public static BeamAxis ClassifyAxis(double dx, double dy)
        {
            double angle = Math.Atan2(Math.Abs(dy), Math.Abs(dx)) * 180.0 / Math.PI;
            return angle <= 45.0 + 1e-9 ? BeamAxis.X : BeamAxis.Y;
        }

        public static List<MarkAssignment> MarkBeams(
            IEnumerable<BeamItem> beams,
            ElementMarkSettings s,
            IDictionary<string, string>? levelCodes = null)
        {
            var result = new List<MarkAssignment>();
            if (beams == null || s == null) return result;

            foreach (var levelGroup in beams.GroupBy(b => b.LevelName ?? ""))
            {
                string levelCode = ResolveLevelCode(levelCodes, levelGroup.Key);

                foreach (BeamAxis axis in new[] { BeamAxis.X, BeamAxis.Y })
                {
                    var segments = levelGroup
                        .Where(b => ClassifyAxis(b.X2 - b.X1, b.Y2 - b.Y1) == axis)
                        .Select(b => ToSegment(b, axis))
                        .ToList();
                    if (segments.Count == 0) continue;

                    // X: hàng xếp theo Y (trên/dưới), trong hàng theo X (trái/phải).
                    // Y: "hàng" là các cột dọc xếp theo X, trong cột theo Y.
                    bool rowsDescending = axis == BeamAxis.X
                        ? s.BeamVertical == VerticalOrder.TopToBottom
                        : s.BeamHorizontal == HorizontalOrder.RightToLeft;
                    bool alongDescending = axis == BeamAxis.X
                        ? s.BeamHorizontal == HorizontalOrder.RightToLeft
                        : s.BeamVertical == VerticalOrder.TopToBottom;

                    var chains = BuildChains(segments, s.BeamRowToleranceMm, s.BeamMergeSpans ? s.BeamChainGapMm : double.NegativeInfinity,
                        rowsDescending, alongDescending);

                    string axisCode = axis == BeamAxis.X ? s.BeamAxisXCode : s.BeamAxisYCode;
                    int number = s.BeamStartNumber;

                    foreach (var chain in chains)
                    {
                        bool multiSpan = chain.Count > 1;
                        for (int i = 0; i < chain.Count; i++)
                        {
                            var tokens = new MarkTokens
                            {
                                Prefix = s.BeamPrefix,
                                Level = levelCode,
                                Axis = axisCode,
                                Number = number,
                                Span = 0
                            };
                            string mark = MarkTemplateEngine.Format(s.BeamTemplate, tokens);
                            if (multiSpan)
                            {
                                tokens.Span = i + 1;
                                mark += MarkTemplateEngine.Format(s.BeamSpanSuffix, tokens);
                            }

                            result.Add(new MarkAssignment
                            {
                                Id = chain[i].Id,
                                Category = MarkCategory.Beam,
                                LevelName = levelGroup.Key,
                                Axis = axisCode,
                                Number = number,
                                Span = multiSpan ? i + 1 : 0,
                                Mark = mark
                            });
                        }
                        number++;
                    }
                }
            }
            return result;
        }

        /// <summary>Đoạn dầm trên hệ trục cục bộ: A..B dọc trục dầm, C là toạ độ vuông góc (trung điểm).</summary>
        public class Segment
        {
            public long Id;
            public double A;
            public double B;
            public double C;
            public double Mid => (A + B) * 0.5;
        }

        private static Segment ToSegment(BeamItem b, BeamAxis axis)
        {
            double a1 = axis == BeamAxis.X ? b.X1 : b.Y1;
            double a2 = axis == BeamAxis.X ? b.X2 : b.Y2;
            double c = axis == BeamAxis.X ? (b.Y1 + b.Y2) * 0.5 : (b.X1 + b.X2) * 0.5;
            return new Segment { Id = b.Id, A = Math.Min(a1, a2), B = Math.Max(a1, a2), C = c };
        }

        /// <summary>
        /// Gom đoạn thành hàng, rồi nối các nhịp thẳng hàng có khe hở ≤ maxGap thành dầm liên tục.
        /// Trả về danh sách dầm (mỗi dầm là danh sách nhịp) đã đúng thứ tự đánh số.
        /// </summary>
        public static List<List<Segment>> BuildChains(
            IList<Segment> segments, double rowTolerance, double maxGap,
            bool rowsDescending, bool alongDescending)
        {
            var output = new List<List<Segment>>();
            var rows = Cluster(segments, x => x.C, rowTolerance, rowsDescending);

            foreach (var row in rows)
            {
                var sorted = row.OrderBy(x => x.A).ThenBy(x => x.B).ThenBy(x => x.Id).ToList();
                var chains = new List<List<Segment>>();
                List<Segment>? current = null;
                double currentEnd = double.NegativeInfinity;

                foreach (var seg in sorted)
                {
                    if (current != null && seg.A - currentEnd <= maxGap)
                    {
                        current.Add(seg);
                        currentEnd = Math.Max(currentEnd, seg.B);
                    }
                    else
                    {
                        current = new List<Segment> { seg };
                        chains.Add(current);
                        currentEnd = seg.B;
                    }
                }

                if (alongDescending)
                {
                    chains.Reverse();
                    for (int i = 0; i < chains.Count; i++)
                        chains[i] = chains[i].OrderByDescending(x => x.Mid).ThenBy(x => x.Id).ToList();
                }

                output.AddRange(chains);
            }
            return output;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Cột
        // ═══════════════════════════════════════════════════════════════

        public static List<MarkAssignment> MarkColumns(
            IEnumerable<ColumnItem> columns,
            ElementMarkSettings s,
            IDictionary<string, string>? levelCodes = null)
        {
            var result = new List<MarkAssignment>();
            if (columns == null || s == null) return result;

            var stacks = BuildColumnStacks(columns, s.ColumnStackToleranceMm);
            var ordered = OrderStacks(stacks, s);

            int number = s.ColumnStartNumber;
            foreach (var stack in ordered)
            {
                foreach (var col in stack.Items)
                {
                    var tokens = new MarkTokens
                    {
                        Prefix = s.ColumnPrefix,
                        Level = ResolveLevelCode(levelCodes, col.LevelName),
                        Number = number
                    };
                    result.Add(new MarkAssignment
                    {
                        Id = col.Id,
                        Category = MarkCategory.Column,
                        LevelName = col.LevelName,
                        Number = number,
                        Mark = MarkTemplateEngine.Format(s.ColumnTemplate, tokens)
                    });
                }
                number++;
            }
            return result;
        }

        /// <summary>Chuỗi cột cùng vị trí từ móng lên mái.</summary>
        public class ColumnStack
        {
            public List<ColumnItem> Items { get; } = new List<ColumnItem>();
            public double X => Items.Average(i => i.X);
            public double Y => Items.Average(i => i.Y);
        }

        /// <summary>
        /// Duyệt từ dưới lên, mỗi cột nối vào chuỗi có cột trên cùng gần nhất (≤ tolerance).
        /// So với cột trên cùng thay vì tâm chuỗi để chịu được cột lệch tâm dần khi thu tiết diện.
        /// </summary>
        public static List<ColumnStack> BuildColumnStacks(IEnumerable<ColumnItem> columns, double tolerance)
        {
            var stacks = new List<ColumnStack>();
            double tol2 = tolerance * tolerance;

            foreach (var col in columns.OrderBy(c => c.BaseZ).ThenBy(c => c.Id))
            {
                ColumnStack? best = null;
                double bestD2 = double.MaxValue;
                foreach (var stack in stacks)
                {
                    var top = stack.Items[stack.Items.Count - 1];
                    double dx = top.X - col.X, dy = top.Y - col.Y;
                    double d2 = dx * dx + dy * dy;
                    if (d2 <= tol2 && d2 < bestD2)
                    {
                        best = stack;
                        bestD2 = d2;
                    }
                }

                if (best == null)
                {
                    best = new ColumnStack();
                    stacks.Add(best);
                }
                best.Items.Add(col);
            }
            return stacks;
        }

        private static List<ColumnStack> OrderStacks(List<ColumnStack> stacks, ElementMarkSettings s)
        {
            bool topToBottom = s.ColumnVertical == VerticalOrder.TopToBottom;
            bool rightToLeft = s.ColumnHorizontal == HorizontalOrder.RightToLeft;
            var output = new List<ColumnStack>();

            // Tính sẵn toạ độ đại diện để không Average lại nhiều lần khi sort.
            var pts = stacks.Select(st => new { Stack = st, st.X, st.Y }).ToList();

            if (s.ColumnPriority == ColumnPriority.RowFirst)
            {
                foreach (var row in Cluster(pts, p => p.Y, s.ColumnRowToleranceMm, topToBottom))
                {
                    var inRow = rightToLeft ? row.OrderByDescending(p => p.X) : row.OrderBy(p => p.X);
                    output.AddRange(inRow.Select(p => p.Stack));
                }
            }
            else
            {
                foreach (var col in Cluster(pts, p => p.X, s.ColumnRowToleranceMm, rightToLeft))
                {
                    var inCol = topToBottom ? col.OrderByDescending(p => p.Y) : col.OrderBy(p => p.Y);
                    output.AddRange(inCol.Select(p => p.Stack));
                }
            }
            return output;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Sàn: một Mark chung cho mọi sàn của cùng Level
        // ═══════════════════════════════════════════════════════════════

        public static List<MarkAssignment> MarkFloors(
            IEnumerable<FloorItem> floors,
            ElementMarkSettings s,
            IDictionary<string, string>? levelCodes = null)
        {
            var result = new List<MarkAssignment>();
            if (floors == null || s == null) return result;

            foreach (var f in floors)
            {
                var tokens = new MarkTokens
                {
                    Prefix = s.FloorPrefix,
                    Level = ResolveLevelCode(levelCodes, f.LevelName),
                    Number = 1
                };
                result.Add(new MarkAssignment
                {
                    Id = f.Id,
                    Category = MarkCategory.Floor,
                    LevelName = f.LevelName,
                    Number = 1,
                    Mark = MarkTemplateEngine.Format(s.FloorTemplate, tokens)
                });
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════

        public static string ResolveLevelCode(IDictionary<string, string>? levelCodes, string levelName)
        {
            if (levelCodes != null && levelName != null &&
                levelCodes.TryGetValue(levelName, out var code) && !string.IsNullOrWhiteSpace(code))
            {
                return code.Trim();
            }
            return LevelCodeResolver.Suggest(levelName ?? "");
        }

        /// <summary>
        /// Gom phần tử theo một toạ độ: phần tử mới thuộc nhóm hiện tại nếu lệch so với
        /// trung bình nhóm ≤ tolerance. So với trung bình (không phải phần tử liền trước)
        /// để một dãy lệch nhỏ liên tiếp không bị trôi thành một hàng dài.
        /// </summary>
        public static List<List<T>> Cluster<T>(IEnumerable<T> items, Func<T, double> key, double tolerance, bool descending)
        {
            var clusters = new List<List<T>>();
            List<T>? current = null;
            double sum = 0;

            foreach (var item in items.OrderBy(key))
            {
                double v = key(item);
                if (current != null && v - sum / current.Count <= tolerance)
                {
                    current.Add(item);
                    sum += v;
                }
                else
                {
                    current = new List<T> { item };
                    clusters.Add(current);
                    sum = v;
                }
            }

            if (descending) clusters.Reverse();
            return clusters;
        }
    }
}
