using System;

namespace JNNTool.RebarSuite.Common
{
    public enum HookAngle
    {
        None = 0,
        Deg90 = 90,
        Deg135 = 135,
        Deg180 = 180
    }

    /// <summary>
    /// Các hàm tính toán thông số uốn móc cốt thép tiêu chuẩn TCVN / BS.
    /// </summary>
    public static class HookHelper
    {
        /// <summary>
        /// Đường kính trục uốn tối thiểu (Mandrel Diameter) theo đường kính cốt thép.
        /// </summary>
        public static double GetMinMandrelDiameterMm(int diameterMm, bool isStirrup = false)
        {
            if (isStirrup)
            {
                return diameterMm < 20 ? 2.5 * diameterMm : 4.0 * diameterMm;
            }
            return diameterMm < 20 ? 4.0 * diameterMm : 5.0 * diameterMm;
        }

        /// <summary>
        /// Chiều dài đoạn thẳng sau móc uốn theo góc móc.
        /// </summary>
        public static double GetHookExtensionLengthMm(int diameterMm, HookAngle angle, bool isStirrup = false)
        {
            switch (angle)
            {
                case HookAngle.Deg90:
                    // Thường là 10d đến 12d, tối thiểu 100mm
                    return Math.Max(10.0 * diameterMm, 100.0);

                case HookAngle.Deg135:
                    // Đai móc 135 độ: 10d (kháng chấn) hoặc 6d, tối thiểu 75mm
                    return Math.Max(10.0 * diameterMm, 75.0);

                case HookAngle.Deg180:
                    // Móc 180 độ: 5d hoặc tối thiểu 65mm
                    return Math.Max(5.0 * diameterMm, 65.0);

                case HookAngle.None:
                default:
                    return 0.0;
            }
        }
    }
}
