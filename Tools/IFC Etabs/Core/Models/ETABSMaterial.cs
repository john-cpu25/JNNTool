namespace JNNTool.Tools.IFCEtabs.Core.Models
{
    /// <summary>
    /// Định nghĩa vật liệu từ ETABS hoặc IFC.
    /// </summary>
    public class ETABSMaterial
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "CONCRETE"; // CONCRETE, STEEL, REBAR, OTHER
        public string Grade { get; set; } = string.Empty;

        // Cơ lý tính
        public double ElasticModulusMpa { get; set; }  // E (MPa)
        public double PoissonRatio { get; set; }       // U
        public double ThermalCoefficient { get; set; } // A
        public double WeightDensityKnM3 { get; set; }  // Dung trọng (kN/m3)
        public double MassDensityKgM3 { get; set; }    // Khối lượng riêng (kg/m3)

        // Cường độ vật liệu
        public double FcMpa { get; set; } // Bê tông nén (MPa)
        public double FyMpa { get; set; } // Thép chảy (MPa)

        public override string ToString() => $"Material: {Name} [{Type}] (E={ElasticModulusMpa:F0} MPa)";
    }
}
