using Autodesk.Revit.DB;

namespace JNNTool.Core.Compat
{
    /// <summary>
    /// Cung cấp các phương thức mở rộng tương thích ngược giữa các phiên bản Revit (2022-2024 dùng IntegerValue, 2025-2027 dùng Value).
    /// </summary>
    public static class ElementIdCompat
    {
        public static long GetIdValue(this ElementId id)
        {
            if (id == null) return -1;
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }
}
