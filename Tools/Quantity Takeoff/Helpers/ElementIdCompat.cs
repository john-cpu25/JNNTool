using System;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.QuantityTakeoff.Helpers
{
    public static class ElementIdCompat
    {
        /// <summary>
        /// Lấy giá trị định danh 64-bit của ElementId tương thích cả Revit <= 2024 (.NET 4.8) và Revit 2025+ (.NET 8).
        /// </summary>
        public static long GetIdValue(this ElementId id)
        {
            if (id == null || id == ElementId.InvalidElementId) return -1;
#if NET8_0_OR_GREATER
            return id.Value;
#else
            return id.IntegerValue;
#endif
        }
    }

    public static class QtoLogger
    {
        public static void Error(string message, Exception ex = null)
        {
            System.Diagnostics.Debug.WriteLine($"[JNN - Quantity Takeoff ERROR] {message}: {ex?.Message}\n{ex?.StackTrace}");
        }

        public static void Info(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[JNN - Quantity Takeoff INFO] {message}");
        }
    }
}
