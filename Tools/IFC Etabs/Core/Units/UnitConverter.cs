namespace JNNTool.Tools.IFCEtabs.Core.Units
{
    /// <summary>
    /// Bộ chuyển đổi đơn vị tập trung.
    /// Không bao giờ hardcode tỷ lệ chuyển đổi rải rác trong code.
    /// </summary>
    public static class UnitConverter
    {
        public const double MmPerFoot = 304.8;
        public const double FeetPerMm = 1.0 / MmPerFoot;

        public const double MmPerInch = 25.4;
        public const double InchesPerMm = 1.0 / MmPerInch;

        public const double MmPerMeter = 1000.0;
        public const double MetersPerMm = 0.001;

        public const double MmPerCm = 10.0;
        public const double CmPerMm = 0.1;

        // Chiều dài
        public static double MmToFeet(double mm) => mm * FeetPerMm;
        public static double FeetToMm(double feet) => feet * MmPerFoot;

        public static double MetersToFeet(double meters) => (meters * MmPerMeter) * FeetPerMm;
        public static double FeetToMeters(double feet) => (feet * MmPerFoot) * MetersPerMm;

        public static double MetersToMm(double meters) => meters * MmPerMeter;
        public static double MmToMeters(double mm) => mm * MetersPerMm;

        public static double CmToMm(double cm) => cm * MmPerCm;
        public static double MmToCm(double mm) => mm * CmPerMm;

        public static double InchesToMm(double inches) => inches * MmPerInch;
        public static double MmToInches(double mm) => mm * InchesPerMm;

        // Lực và ứng suất (ETABS thường dùng kN-m, N-mm)
        public static double KnMToNmm(double knM) => knM * 1000000.0;
        public static double NmmToKnM(double nMm) => nMm / 1000000.0;

        public static double MpaToKpa(double mpa) => mpa * 1000.0;
        public static double KpaToMpa(double kpa) => kpa / 1000.0;
    }
}
