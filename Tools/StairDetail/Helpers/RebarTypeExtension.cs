using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;

namespace JNNTool.Tools.StairDetail
{
    public static class RebarTypeExtension
    {
        public static double GetBarModelDiameter(this RebarBarType barType)
        {
#if REVIT2022_OR_GREATER
            return barType.BarModelDiameter;
#else
            return barType.BarModelDiameter;
#endif
        }
    }
}

