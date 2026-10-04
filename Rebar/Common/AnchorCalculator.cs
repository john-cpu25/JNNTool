using System;

namespace JNNTool.RebarSuite.Common
{
    /// <summary>
    /// Các cấp độ bền bê tông phổ biến theo TCVN 5574:2018.
    /// </summary>
    public enum ConcreteGrade
    {
        B15,
        B20,
        B25,
        B30,
        B35,
        B40
    }

    /// <summary>
    /// Các nhóm cốt thép thông dụng theo TCVN 1651:2018 / TCVN 5574:2018.
    /// </summary>
    public enum RebarSteelGrade
    {
        CB240_T, // Thép cuộn tròn trơn
        CB300_V, // Thép thanh vằn
        CB400_V, // Thép thanh vằn
        CB500_V  // Thép thanh vằn
    }

    /// <summary>
    /// Bộ tính toán chiều dài neo cốt thép theo TCVN 5574:2018 và quy chuẩn thực hành thiết kế.
    /// </summary>
    public static class AnchorCalculator
    {
        /// <summary>
        /// Lấy cường độ chịu kéo tính toán của cốt thép Rs (MPa) theo TCVN 5574:2018.
        /// </summary>
        public static double GetRs(RebarSteelGrade steelGrade)
        {
            switch (steelGrade)
            {
                case RebarSteelGrade.CB240_T: return 210.0;
                case RebarSteelGrade.CB300_V: return 260.0;
                case RebarSteelGrade.CB400_V: return 350.0;
                case RebarSteelGrade.CB500_V: return 435.0;
                default: return 350.0;
            }
        }

        /// <summary>
        /// Lấy cường độ chịu kéo dọc trục tính toán của bê tông Rbt (MPa).
        /// </summary>
        public static double GetRbt(ConcreteGrade concreteGrade)
        {
            switch (concreteGrade)
            {
                case ConcreteGrade.B15: return 0.75;
                case ConcreteGrade.B20: return 0.90;
                case ConcreteGrade.B25: return 1.05;
                case ConcreteGrade.B30: return 1.15;
                case ConcreteGrade.B35: return 1.30;
                case ConcreteGrade.B40: return 1.40;
                default: return 1.05;
            }
        }

        /// <summary>
        /// Cường độ bám dính tính toán Rbond (MPa) giữa cốt thép và bê tông.
        /// Rbond = eta1 * eta2 * Rbt
        /// </summary>
        public static double GetRbond(ConcreteGrade concreteGrade, RebarSteelGrade steelGrade, int diameterMm)
        {
            double rbt = GetRbt(concreteGrade);
            // eta1: hệ số xét đến bề mặt cốt thép (1.5 cho tròn trơn, 2.5 cho thanh vằn)
            double eta1 = (steelGrade == RebarSteelGrade.CB240_T) ? 1.5 : 2.5;
            // eta2: hệ số xét đến đường kính thanh (1.0 nếu d <= 32mm)
            double eta2 = diameterMm <= 32 ? 1.0 : 0.9;

            return eta1 * eta2 * rbt;
        }

        /// <summary>
        /// Tính chiều dài neo cơ sở l0,an (mm) theo công thức TCVN 5574:2018 (mục 10.3.8.1).
        /// l0,an = (Rs * d) / (4 * Rbond)
        /// </summary>
        public static double CalculateBaseAnchorLengthMm(int diameterMm, ConcreteGrade concreteGrade, RebarSteelGrade steelGrade)
        {
            double rs = GetRs(steelGrade);
            double rbond = GetRbond(concreteGrade, steelGrade, diameterMm);
            double l0_an = (rs * diameterMm) / (4.0 * rbond);

            // Giới hạn chiều dài neo tối thiểu không nhỏ hơn 15d và 200 mm
            double minLength = Math.Max(15.0 * diameterMm, 200.0);
            return Math.Max(l0_an, minLength);
        }

        /// <summary>
        /// Tính chiều dài neo thực tế làm tròn lên bội số (mặc định bội số 50 mm).
        /// </summary>
        public static double CalculateAnchorLengthMm(int diameterMm, ConcreteGrade concreteGrade, RebarSteelGrade steelGrade, bool isCompression = false, int roundMultipleMm = 50)
        {
            double l0 = CalculateBaseAnchorLengthMm(diameterMm, concreteGrade, steelGrade);
            // Trong vùng nén, hệ số alpha = 0.75
            double alpha = isCompression ? 0.75 : 1.0;
            double len = l0 * alpha;

            return GeometryHelper.RoundUpToMultiple(len, roundMultipleMm);
        }

        /// <summary>
        /// Tính nhanh chiều dài neo theo hệ số bội số đường kính (ví dụ 30d, 40d) làm tròn 50mm.
        /// </summary>
        public static double CalculateByMultiplier(int diameterMm, double multiplier = 35.0, int roundMultipleMm = 50)
        {
            double len = Math.Max(diameterMm * multiplier, 200.0);
            return GeometryHelper.RoundUpToMultiple(len, roundMultipleMm);
        }
    }
}
