using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Core.Compat
{
    /// <summary>
    /// Helper tương thích hàm Intersect của Curve giữa Revit 2022-2026 (IntersectionResultArray) và Revit 2027+ (CurveIntersectResult).
    /// </summary>
    public static class CurveCompat
    {
        public static List<XYZ> IntersectPoints(Curve cv1, Curve cv2)
        {
            var points = new List<XYZ>();
            if (cv1 == null || cv2 == null) return points;

#if REVIT2027
            var res = cv1.Intersect(cv2, CurveIntersectResultOption.Detailed);
            if (res != null && res.Result == SetComparisonResult.Overlap)
            {
                var overlaps = res.GetOverlaps();
                if (overlaps != null)
                {
                    foreach (var ov in overlaps)
                    {
                        if (ov?.Point != null)
                        {
                            points.Add(ov.Point);
                        }
                    }
                }
            }
#else
            IntersectionResultArray results;
            SetComparisonResult res = cv1.Intersect(cv2, out results);
            if (res == SetComparisonResult.Overlap && results != null && !results.IsEmpty)
            {
                for (int i = 0; i < results.Size; i++)
                {
                    var item = results.get_Item(i);
                    if (item?.XYZPoint != null)
                    {
                        points.Add(item.XYZPoint);
                    }
                }
            }
#endif
            return points;
        }

        public static XYZ? IntersectFirstPoint(Curve cv1, Curve cv2)
        {
            var pts = IntersectPoints(cv1, cv2);
            return pts.Count > 0 ? pts[0] : null;
        }
    }
}
