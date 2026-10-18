using System;
using System.Collections.Generic;
using System.Linq;
using JNNTool.Tools.CutByFloor.Models;

namespace JNNTool.Tools.CutByFloor.Engine
{
    /// <summary>
    /// Engine tính toán phép trừ đa giác phẳng 2D (Boolean Difference: Target \ Cutters).
    /// Hoàn toàn thuần C#, không tham chiếu Revit API -> Unit test độc lập được.
    /// Độ chính xác sub-millimeter, tự động xử lý T-junction, hàn đỉnh sai số, và gộp cạnh thẳng hàng.
    /// </summary>
    public static class FloorDifferenceEngine
    {
        public const double DefaultSnapTolFt = 8.0 / 304.8;        // ~8 mm
        public const double DefaultCollinearTolFt = 2.0 / 304.8;   // ~2 mm
        public const double DefaultMinEdgeLenFt = 3.0 / 304.8;     // ~3 mm

        /// <summary>
        /// Thực hiện phép trừ: Sàn To (Target) trừ đi danh sách các Sàn Nhỏ (Cutters).
        /// Trả về danh sách các đảo sàn độc lập (FloorIsland), mỗi đảo gồm 1 Outer Loop (CCW) và các Hole Loops (CW).
        /// </summary>
        public static List<FloorIsland> Difference(
            FloorPolyRegion target,
            List<FloorPolyRegion> cutters,
            double snapTol = DefaultSnapTolFt,
            double collinearTol = DefaultCollinearTolFt,
            double minEdgeLen = DefaultMinEdgeLenFt)
        {
            if (target?.Outer == null || target.Outer.Count < 3)
                return new List<FloorIsland>();

            // Chuẩn hóa Target: Outer là CCW (Area > 0), Holes là CW (Area < 0)
            target = NormalizeRegion(target);
            if (target == null) return new List<FloorIsland>();

            // Lọc các cutter thực sự chồng lấn hoặc chạm vào BoundingBox của Target
            var validCutters = new List<FloorPolyRegion>();
            if (cutters != null)
            {
                var targetBox = ComputeBounds(target.Outer);
                foreach (var c in cutters)
                {
                    if (c?.Outer == null || c.Outer.Count < 3) continue;
                    var cNorm = NormalizeRegion(c);
                    if (cNorm == null) continue;

                    var cBox = ComputeBounds(cNorm.Outer);
                    if (BoundsOverlap(targetBox, cBox, snapTol * 2.0))
                    {
                        validCutters.Add(cNorm);
                    }
                }
            }

            // Nếu không có cutter nào chạm sàn to -> giữ nguyên sàn to ban đầu
            if (validCutters.Count == 0)
            {
                return new List<FloorIsland>
                {
                    new FloorIsland
                    {
                        OuterLoop = new List<Pt2D>(target.Outer),
                        HoleLoops = target.Holes.Select(h => new List<Pt2D>(h)).ToList(),
                        Area = Math.Abs(ComputeSignedArea(target.Outer))
                    }
                };
            }

            // Bước 1: Thu thập toàn bộ đỉnh thô để gom cụm Canonical Vertices
            var rawVertices = new List<Pt2D>();
            rawVertices.AddRange(target.Outer);
            foreach (var h in target.Holes) rawVertices.AddRange(h);
            foreach (var c in validCutters)
            {
                rawVertices.AddRange(c.Outer);
                foreach (var h in c.Holes) rawVertices.AddRange(h);
            }

            List<Pt2D> canonicalVerts = BuildCanonicalVertices(rawVertices, snapTol);

            // Bước 2: Snap các đa giác vào Canonical Vertices
            target.Outer = SnapLoopToCanonical(target.Outer, canonicalVerts, snapTol);
            for (int i = target.Holes.Count - 1; i >= 0; i--)
            {
                target.Holes[i] = SnapLoopToCanonical(target.Holes[i], canonicalVerts, snapTol);
                if (target.Holes[i].Count < 3) target.Holes.RemoveAt(i);
            }

            foreach (var c in validCutters)
            {
                c.Outer = SnapLoopToCanonical(c.Outer, canonicalVerts, snapTol);
                for (int i = c.Holes.Count - 1; i >= 0; i--)
                {
                    c.Holes[i] = SnapLoopToCanonical(c.Holes[i], canonicalVerts, snapTol);
                    if (c.Holes[i].Count < 3) c.Holes.RemoveAt(i);
                }
            }

            // Bước 3: Chiếu điểm T-junction vào các cạnh đi ngang qua nó
            ProjectTJunctions(target, validCutters, canonicalVerts, snapTol);

            target.Outer = InjectTJunctionVertices(target.Outer, canonicalVerts, snapTol);
            for (int i = 0; i < target.Holes.Count; i++)
                target.Holes[i] = InjectTJunctionVertices(target.Holes[i], canonicalVerts, snapTol);

            foreach (var c in validCutters)
            {
                c.Outer = InjectTJunctionVertices(c.Outer, canonicalVerts, snapTol);
                for (int i = 0; i < c.Holes.Count; i++)
                    c.Holes[i] = InjectTJunctionVertices(c.Holes[i], canonicalVerts, snapTol);
            }

            // Bước 4: Thu thập toàn bộ đoạn thẳng và chia nhỏ tại mọi giao điểm thực (X-intersections)
            var allSegments = new List<Seg2D>();
            AddLoopSegments(target.Outer, allSegments);
            foreach (var h in target.Holes) AddLoopSegments(h, allSegments);

            foreach (var c in validCutters)
            {
                AddLoopSegments(c.Outer, allSegments);
                foreach (var h in c.Holes) AddLoopSegments(h, allSegments);
            }

            List<Seg2D> elementarySegments = SubdivideSegmentsAtIntersections(allSegments, canonicalVerts, 1e-4);
            if (elementarySegments.Count < 3)
                return new List<FloorIsland>();

            // Bước 5: Lọc các cạnh biên của hiệu đa giác (Boolean Difference Half-Edges)
            // Một đoạn thẳng là cạnh biên khi: Một bên thuộc Hiệu (inTarget && !inAnyCutter) và một bên NẰM NGOÀI Hiệu!
            var boundaryHalfEdges = new List<DirectedEdge>();

            foreach (Seg2D seg in elementarySegments)
            {
                Pt2D u = canonicalVerts[seg.U];
                Pt2D v = canonicalVerts[seg.V];
                double dx = v.X - u.X;
                double dy = v.Y - u.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len < 1e-4) continue;

                Pt2D mid = new Pt2D(0.5 * (u.X + v.X), 0.5 * (u.Y + v.Y));
                double nx = -dy / len; // Vector pháp tuyến bên trái
                double ny = dx / len;

                double minDistOther = MinDistanceToOtherSegments(mid, seg, elementarySegments, canonicalVerts);
                double probeDist = Math.Min(0.002, 0.25 * Math.Min(len, minDistOther));
                if (probeDist < 1e-6) probeDist = 1e-5;

                Pt2D probeLeft = new Pt2D(mid.X + nx * probeDist, mid.Y + ny * probeDist);
                Pt2D probeRight = new Pt2D(mid.X - nx * probeDist, mid.Y - ny * probeDist);

                bool leftInTarget = IsPointInRegion(probeLeft, target);
                bool leftInCutter = validCutters.Any(c => IsPointInRegion(probeLeft, c));
                bool leftInDiff = leftInTarget && !leftInCutter;

                bool rightInTarget = IsPointInRegion(probeRight, target);
                bool rightInCutter = validCutters.Any(c => IsPointInRegion(probeRight, c));
                bool rightInDiff = rightInTarget && !rightInCutter;

                if (leftInDiff && !rightInDiff)
                {
                    // Miền hiệu nằm bên trái -> giữ hướng U -> V
                    boundaryHalfEdges.Add(new DirectedEdge(seg.U, seg.V, u, v));
                }
                else if (!leftInDiff && rightInDiff)
                {
                    // Miền hiệu nằm bên phải -> giữ hướng V -> U (để miền hiệu luôn nằm bên trái)
                    boundaryHalfEdges.Add(new DirectedEdge(seg.V, seg.U, v, u));
                }
            }

            if (boundaryHalfEdges.Count < 3)
                return new List<FloorIsland>();

            // Bước 6: Lần theo các cạnh có hướng để tạo các chu trình khép kín (Outer CCW & Hole CW)
            List<List<Pt2D>> rawCycles = TraceClosedCycles(boundaryHalfEdges, canonicalVerts);
            var outerLoops = new List<List<Pt2D>>();
            var holeLoops = new List<List<Pt2D>>();

            foreach (List<Pt2D> cycle in rawCycles)
            {
                List<Pt2D> simplified = SimplifyCollinearPolygon(cycle, collinearTol, minEdgeLen);
                if (simplified.Count < 3) continue;

                double signedArea = ComputeSignedArea(simplified);
                if (Math.Abs(signedArea) < 0.01) // Bỏ qua mảnh vụn < ~9 cm2
                    continue;

                if (signedArea > 0)
                    outerLoops.Add(simplified);
                else
                    holeLoops.Add(simplified);
            }

            if (outerLoops.Count == 0)
                return new List<FloorIsland>();

            // Bước 7: Phân bổ các lỗ mở (Hole CW) vào đúng mảng sàn bao quanh nó (Outer CCW)
            var islands = new List<FloorIsland>();
            foreach (List<Pt2D> outer in outerLoops)
            {
                islands.Add(new FloorIsland
                {
                    OuterLoop = outer,
                    HoleLoops = new List<List<Pt2D>>(),
                    Area = Math.Abs(ComputeSignedArea(outer))
                });
            }

            foreach (List<Pt2D> hole in holeLoops)
            {
                int bestIslandIdx = -1;
                double bestOuterArea = double.MaxValue;

                for (int i = 0; i < islands.Count; i++)
                {
                    if (IsPointInPolygon2D(hole[0], islands[i].OuterLoop))
                    {
                        double area = Math.Abs(ComputeSignedArea(islands[i].OuterLoop));
                        if (area < bestOuterArea)
                        {
                            bestOuterArea = area;
                            bestIslandIdx = i;
                        }
                    }
                }

                if (bestIslandIdx >= 0)
                {
                    islands[bestIslandIdx].HoleLoops.Add(hole);
                }
            }

            return islands;
        }

        // ═════════════════════════════════════════════════════════════════
        //  Geometry Helpers
        // ═════════════════════════════════════════════════════════════════

        public static double ComputeSignedArea(List<Pt2D> pts)
        {
            if (pts == null || pts.Count < 3) return 0.0;
            double area = 0.0;
            int n = pts.Count;
            for (int i = 0; i < n; i++)
            {
                Pt2D p0 = pts[i];
                Pt2D p1 = pts[(i + 1) % n];
                area += (p0.X * p1.Y - p1.X * p0.Y);
            }
            return 0.5 * area;
        }

        public static bool IsPointInPolygon2D(Pt2D pt, List<Pt2D> poly)
        {
            if (poly == null || poly.Count < 3) return false;
            bool inside = false;
            int n = poly.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                Pt2D pi = poly[i];
                Pt2D pj = poly[j];

                if (((pi.Y > pt.Y) != (pj.Y > pt.Y)) &&
                    (pt.X < (pj.X - pi.X) * (pt.Y - pi.Y) / (pj.Y - pi.Y + 1e-12) + pi.X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        public static bool IsPointInRegion(Pt2D pt, FloorPolyRegion reg)
        {
            if (reg?.Outer == null || !IsPointInPolygon2D(pt, reg.Outer))
                return false;

            if (reg.Holes != null)
            {
                foreach (var h in reg.Holes)
                {
                    if (IsPointInPolygon2D(pt, h))
                        return false;
                }
            }

            return true;
        }

        private static FloorPolyRegion NormalizeRegion(FloorPolyRegion reg)
        {
            if (reg?.Outer == null || reg.Outer.Count < 3) return null;

            var result = new FloorPolyRegion();
            var outer = new List<Pt2D>(reg.Outer);
            double outerArea = ComputeSignedArea(outer);
            if (Math.Abs(outerArea) < 1e-4) return null;

            if (outerArea < 0) outer.Reverse(); // Outer phải là CCW (Area > 0)
            result.Outer = outer;

            if (reg.Holes != null)
            {
                foreach (var h in reg.Holes)
                {
                    if (h == null || h.Count < 3) continue;
                    var hole = new List<Pt2D>(h);
                    double hArea = ComputeSignedArea(hole);
                    if (Math.Abs(hArea) < 1e-4) continue;

                    if (hArea > 0) hole.Reverse(); // Hole phải là CW (Area < 0)
                    result.Holes.Add(hole);
                }
            }

            return result;
        }

        private static (Pt2D Min, Pt2D Max) ComputeBounds(List<Pt2D> pts)
        {
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;
            foreach (var p in pts)
            {
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Y < minY) minY = p.Y;
                if (p.Y > maxY) maxY = p.Y;
            }
            return (new Pt2D(minX, minY), new Pt2D(maxX, maxY));
        }

        private static bool BoundsOverlap((Pt2D Min, Pt2D Max) a, (Pt2D Min, Pt2D Max) b, double tol)
        {
            if (a.Max.X + tol < b.Min.X || b.Max.X + tol < a.Min.X) return false;
            if (a.Max.Y + tol < b.Min.Y || b.Max.Y + tol < a.Min.Y) return false;
            return true;
        }

        private static List<Pt2D> BuildCanonicalVertices(List<Pt2D> rawVerts, double snapTol)
        {
            var canonical = new List<Pt2D>();
            double snapTolSq = snapTol * snapTol;

            foreach (var p in rawVerts)
            {
                int match = -1;
                double bestDist = snapTolSq;
                for (int i = 0; i < canonical.Count; i++)
                {
                    double d = p.DistanceSqTo(canonical[i]);
                    if (d <= bestDist)
                    {
                        bestDist = d;
                        match = i;
                    }
                }

                if (match < 0)
                {
                    canonical.Add(new Pt2D(p.X, p.Y));
                }
            }

            return canonical;
        }

        private static List<Pt2D> SnapLoopToCanonical(List<Pt2D> loop, List<Pt2D> canonical, double snapTol)
        {
            if (loop == null) return new List<Pt2D>();
            var result = new List<Pt2D>();
            double snapTolSq = snapTol * snapTol;

            foreach (var p in loop)
            {
                Pt2D best = p;
                double bestDist = snapTolSq;
                foreach (var c in canonical)
                {
                    double d = p.DistanceSqTo(c);
                    if (d <= bestDist)
                    {
                        bestDist = d;
                        best = c;
                    }
                }
                if (result.Count == 0 || result[result.Count - 1].DistanceSqTo(best) > 1e-8)
                    result.Add(best);
            }

            if (result.Count >= 2 && result[0].DistanceSqTo(result[result.Count - 1]) <= 1e-8)
                result.RemoveAt(result.Count - 1);

            return result;
        }

        private static void ProjectTJunctions(
            FloorPolyRegion target,
            List<FloorPolyRegion> cutters,
            List<Pt2D> canonical,
            double snapTol)
        {
            var allLoops = new List<List<Pt2D>> { target.Outer };
            allLoops.AddRange(target.Holes);
            foreach (var c in cutters)
            {
                allLoops.Add(c.Outer);
                allLoops.AddRange(c.Holes);
            }

            for (int vIdx = 0; vIdx < canonical.Count; vIdx++)
            {
                Pt2D v = canonical[vIdx];
                foreach (var loop in allLoops)
                {
                    int n = loop.Count;
                    for (int i = 0; i < n; i++)
                    {
                        Pt2D a = loop[i];
                        Pt2D b = loop[(i + 1) % n];

                        if (v.DistanceSqTo(a) < 1e-6 || v.DistanceSqTo(b) < 1e-6)
                            continue;

                        double d = DistancePointToSegment(v, a, b);
                        if (d > 1e-5 && d <= snapTol)
                        {
                            Pt2D proj = ProjectPointOnSegment(v, a, b);
                            v.X = proj.X;
                            v.Y = proj.Y;
                        }
                    }
                }
            }
        }

        private static List<Pt2D> InjectTJunctionVertices(List<Pt2D> loop, List<Pt2D> canonical, double snapTol)
        {
            if (loop == null || loop.Count < 3) return loop;
            var result = new List<Pt2D>();
            int n = loop.Count;

            for (int i = 0; i < n; i++)
            {
                Pt2D a = loop[i];
                Pt2D b = loop[(i + 1) % n];
                result.Add(a);

                double abx = b.X - a.X;
                double aby = b.Y - a.Y;
                double lenSq = abx * abx + aby * aby;
                if (lenSq < 1e-8) continue;

                var onEdge = new List<(double t, Pt2D p)>();
                foreach (var c in canonical)
                {
                    if (c.DistanceSqTo(a) < 1e-6 || c.DistanceSqTo(b) < 1e-6)
                        continue;

                    double d = DistancePointToSegment(c, a, b);
                    if (d <= snapTol)
                    {
                        double t = ((c.X - a.X) * abx + (c.Y - a.Y) * aby) / lenSq;
                        if (t > 1e-4 && t < 1.0 - 1e-4)
                        {
                            onEdge.Add((t, c));
                        }
                    }
                }

                foreach (var item in onEdge.OrderBy(x => x.t))
                {
                    result.Add(item.p);
                }
            }

            return result;
        }

        private static void AddLoopSegments(List<Pt2D> loop, List<Seg2D> segments)
        {
            if (loop == null || loop.Count < 3) return;
            int n = loop.Count;
            for (int i = 0; i < n; i++)
            {
                segments.Add(new Seg2D
                {
                    A = loop[i],
                    B = loop[(i + 1) % n]
                });
            }
        }

        private static List<Seg2D> SubdivideSegmentsAtIntersections(
            List<Seg2D> rawSegs,
            List<Pt2D> canonical,
            double tol)
        {
            int n = rawSegs.Count;
            var splitPointsPerSeg = new List<HashSet<Pt2D>>();
            for (int i = 0; i < n; i++)
            {
                var set = new HashSet<Pt2D>(new Pt2DComparer(tol))
                {
                    rawSegs[i].A,
                    rawSegs[i].B
                };
                splitPointsPerSeg.Add(set);
            }

            for (int i = 0; i < n; i++)
            {
                Pt2D a1 = rawSegs[i].A;
                Pt2D a2 = rawSegs[i].B;

                for (int j = i + 1; j < n; j++)
                {
                    Pt2D b1 = rawSegs[j].A;
                    Pt2D b2 = rawSegs[j].B;

                    if (SegmentIntersection2D(a1, a2, b1, b2, out Pt2D inter))
                    {
                        splitPointsPerSeg[i].Add(inter);
                        splitPointsPerSeg[j].Add(inter);
                    }
                }
            }

            var uniqueUndirected = new Dictionary<long, Seg2D>();

            for (int i = 0; i < n; i++)
            {
                Pt2D a = rawSegs[i].A;
                Pt2D b = rawSegs[i].B;
                double abx = b.X - a.X;
                double aby = b.Y - a.Y;
                double lenSq = abx * abx + aby * aby;
                if (lenSq < 1e-8) continue;

                var ordered = splitPointsPerSeg[i]
                    .OrderBy(p => ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq)
                    .ToList();

                for (int k = 0; k < ordered.Count - 1; k++)
                {
                    int u = FindOrAddCanonicalVertex(ordered[k], canonical, tol);
                    int v = FindOrAddCanonicalVertex(ordered[k + 1], canonical, tol);
                    if (u == v) continue;

                    int minV = Math.Min(u, v);
                    int maxV = Math.Max(u, v);
                    long key = ((long)minV << 32) | (uint)maxV;
                    if (!uniqueUndirected.ContainsKey(key))
                    {
                        uniqueUndirected[key] = new Seg2D
                        {
                            U = minV,
                            V = maxV,
                            A = canonical[minV],
                            B = canonical[maxV]
                        };
                    }
                }
            }

            return uniqueUndirected.Values.ToList();
        }

        private static int FindOrAddCanonicalVertex(Pt2D p, List<Pt2D> canonical, double tol)
        {
            double tolSq = tol * tol;
            for (int i = 0; i < canonical.Count; i++)
            {
                if (p.DistanceSqTo(canonical[i]) <= tolSq)
                    return i;
            }
            canonical.Add(new Pt2D(p.X, p.Y));
            return canonical.Count - 1;
        }

        private static double MinDistanceToOtherSegments(
            Pt2D mid,
            Seg2D current,
            List<Seg2D> allSegs,
            List<Pt2D> canonical)
        {
            double minDist = 1.0;
            foreach (Seg2D other in allSegs)
            {
                if (other.U == current.U && other.V == current.V) continue;
                double d = DistancePointToSegment(mid, canonical[other.U], canonical[other.V]);
                if (d > 1e-6 && d < minDist)
                    minDist = d;
            }
            return minDist;
        }

        private static List<List<Pt2D>> TraceClosedCycles(List<DirectedEdge> edges, List<Pt2D> canonical)
        {
            var adj = new Dictionary<int, List<int>>();
            for (int i = 0; i < edges.Count; i++)
            {
                int u = edges[i].U;
                if (!adj.TryGetValue(u, out var list))
                {
                    list = new List<int>();
                    adj[u] = list;
                }
                list.Add(i);
            }

            var used = new bool[edges.Count];
            var cycles = new List<List<Pt2D>>();

            for (int i = 0; i < edges.Count; i++)
            {
                if (used[i]) continue;

                var cycleIndices = new List<int>();
                int curEdgeIdx = i;

                while (!used[curEdgeIdx])
                {
                    used[curEdgeIdx] = true;
                    DirectedEdge e = edges[curEdgeIdx];
                    cycleIndices.Add(e.U);

                    int nextV = e.V;
                    if (!adj.TryGetValue(nextV, out var outList))
                        break;

                    int bestNext = -1;
                    double bestAngle = double.MaxValue;
                    double inAngle = Math.Atan2(e.U_Pt.Y - e.V_Pt.Y, e.U_Pt.X - e.V_Pt.X);

                    foreach (int candIdx in outList)
                    {
                        if (used[candIdx]) continue;
                        DirectedEdge cand = edges[candIdx];
                        double outAngle = Math.Atan2(cand.V_Pt.Y - cand.U_Pt.Y, cand.V_Pt.X - cand.U_Pt.X);
                        double ccwDelta = outAngle - inAngle;
                        while (ccwDelta <= 1e-7) ccwDelta += 2.0 * Math.PI;

                        if (ccwDelta < bestAngle)
                        {
                            bestAngle = ccwDelta;
                            bestNext = candIdx;
                        }
                    }

                    if (bestNext < 0) break;
                    curEdgeIdx = bestNext;
                }

                if (cycleIndices.Count >= 3)
                {
                    cycles.Add(cycleIndices.Select(idx => canonical[idx]).ToList());
                }
            }

            return cycles;
        }

        private static List<Pt2D> SimplifyCollinearPolygon(List<Pt2D> pts, double colTol, double minEdgeLen)
        {
            if (pts == null || pts.Count < 3) return new List<Pt2D>();

            // Lọc các điểm trùng cạnh liên tiếp
            var work = new List<Pt2D>();
            for (int i = 0; i < pts.Count; i++)
            {
                Pt2D p = pts[i];
                if (work.Count == 0 || p.DistanceTo(work[work.Count - 1]) >= minEdgeLen)
                    work.Add(p);
            }

            if (work.Count >= 2 && work[0].DistanceTo(work[work.Count - 1]) < minEdgeLen)
                work.RemoveAt(work.Count - 1);

            if (work.Count < 3) return new List<Pt2D>();

            bool changed = true;
            while (changed && work.Count >= 3)
            {
                changed = false;
                for (int i = 0; i < work.Count; i++)
                {
                    int prevIdx = (i - 1 + work.Count) % work.Count;
                    int nextIdx = (i + 1) % work.Count;
                    Pt2D a = work[prevIdx];
                    Pt2D p = work[i];
                    Pt2D b = work[nextIdx];

                    double d = DistancePointToSegment(p, a, b);
                    if (d <= colTol)
                    {
                        work.RemoveAt(i);
                        changed = true;
                        break;
                    }
                }
            }

            return work;
        }

        public static double DistancePointToSegment(Pt2D p, Pt2D a, Pt2D b)
        {
            double abx = b.X - a.X;
            double aby = b.Y - a.Y;
            double lenSq = abx * abx + aby * aby;
            if (lenSq < 1e-12) return p.DistanceTo(a);

            double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
            if (t <= 0.0) return p.DistanceTo(a);
            if (t >= 1.0) return p.DistanceTo(b);

            Pt2D proj = new Pt2D(a.X + t * abx, a.Y + t * aby);
            return p.DistanceTo(proj);
        }

        public static Pt2D ProjectPointOnSegment(Pt2D p, Pt2D a, Pt2D b)
        {
            double abx = b.X - a.X;
            double aby = b.Y - a.Y;
            double lenSq = abx * abx + aby * aby;
            if (lenSq < 1e-12) return new Pt2D(a.X, a.Y);

            double t = ((p.X - a.X) * abx + (p.Y - a.Y) * aby) / lenSq;
            t = Math.Max(0.0, Math.Min(1.0, t));
            return new Pt2D(a.X + t * abx, a.Y + t * aby);
        }

        public static bool SegmentIntersection2D(Pt2D p1, Pt2D p2, Pt2D p3, Pt2D p4, out Pt2D inter)
        {
            inter = null;
            double d1x = p2.X - p1.X;
            double d1y = p2.Y - p1.Y;
            double d2x = p4.X - p3.X;
            double d2y = p4.Y - p3.Y;

            double denom = d1x * d2y - d1y * d2x;
            if (Math.Abs(denom) < 1e-10) return false;

            double dx = p3.X - p1.X;
            double dy = p3.Y - p1.Y;

            double t = (dx * d2y - dy * d2x) / denom;
            double u = (dx * d1y - dy * d1x) / denom;

            if (t > 1e-5 && t < 1.0 - 1e-5 && u > 1e-5 && u < 1.0 - 1e-5)
            {
                inter = new Pt2D(p1.X + t * d1x, p1.Y + t * d1y);
                return true;
            }

            return false;
        }

        private sealed class Pt2DComparer : IEqualityComparer<Pt2D>
        {
            private readonly double _tol;
            public Pt2DComparer(double tol) => _tol = tol;
            public bool Equals(Pt2D a, Pt2D b) => a != null && b != null && a.DistanceTo(b) <= _tol;
            public int GetHashCode(Pt2D obj) => 0; // Bucket all for exact distance check
        }
    }
}
