using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace JNNTool.Core.Compat
{
    public enum JnnHookOrientation
    {
        Left,
        Right
    }

    /// <summary>
    /// Helper tương thích cho việc tạo thép giữa Revit 2022-2026 và Revit 2027+ (RebarBarTerminationsData).
    /// </summary>
    public static class RebarCompat
    {
        public static Rebar CreateFromCurves(
            Document doc,
            RebarStyle style,
            RebarBarType barType,
            RebarHookType? hookType0,
            RebarHookType? hookType1,
            Element host,
            XYZ norm,
            IList<Curve> curves,
            JnnHookOrientation hookOrient0,
            JnnHookOrientation hookOrient1,
            bool useExistingShapeIfPossible,
            bool createNewShape)
        {
#if REVIT2027
            var terminations = new BarTerminationsData(doc);
            if (hookType0 != null)
            {
                terminations.HookTypeIdAtStart = hookType0.Id;
                terminations.TerminationOrientationAtStart = hookOrient0 == JnnHookOrientation.Right
                    ? RebarTerminationOrientation.Right
                    : RebarTerminationOrientation.Left;
            }
            if (hookType1 != null)
            {
                terminations.HookTypeIdAtEnd = hookType1.Id;
                terminations.TerminationOrientationAtEnd = hookOrient1 == JnnHookOrientation.Right
                    ? RebarTerminationOrientation.Right
                    : RebarTerminationOrientation.Left;
            }

            return Rebar.CreateFromCurves(
                doc,
                style,
                barType,
                host,
                norm,
                curves,
                terminations,
                useExistingShapeIfPossible,
                createNewShape);
#else
            return Rebar.CreateFromCurves(
                doc,
                style,
                barType,
                hookType0,
                hookType1,
                host,
                norm,
                curves,
                hookOrient0 == JnnHookOrientation.Right ? RebarHookOrientation.Right : RebarHookOrientation.Left,
                hookOrient1 == JnnHookOrientation.Right ? RebarHookOrientation.Right : RebarHookOrientation.Left,
                useExistingShapeIfPossible,
                createNewShape);
#endif
        }
    }
}
