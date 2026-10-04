using System;

namespace JNNTool.RebarSuite.Common
{
    public enum StructuralElementType
    {
        Slab,
        Wall,
        Beam,
        Column,
        FoundationWithBlinding,
        FoundationWithoutBlinding
    }

    public enum ExposureEnvironment
    {
        IndoorDry,          // Trong nhà, môi trường khô ráo
        OutdoorOrMoist,     // Ngoài trời hoặc tiếp xúc đất
        Corrosive           // Môi trường xâm thực / biển
    }

    /// <summary>
    /// Tra cứu và kiểm tra lớp bê tông bảo vệ theo TCVN 5574:2018.
    /// </summary>
    public static class CoverHelper
    {
        /// <summary>
        /// Lấy chiều dày lớp bảo vệ danh định tối thiểu (mm) theo cấu kiện và môi trường.
        /// </summary>
        public static double GetMinCoverMm(StructuralElementType elementType, ExposureEnvironment environment = ExposureEnvironment.IndoorDry, double memberThicknessOrHeightMm = 150)
        {
            double baseCover;

            switch (elementType)
            {
                case StructuralElementType.Slab:
                case StructuralElementType.Wall:
                    baseCover = memberThicknessOrHeightMm <= 100 ? 10.0 : 15.0;
                    break;

                case StructuralElementType.Beam:
                    baseCover = memberThicknessOrHeightMm < 250 ? 15.0 : 20.0;
                    break;

                case StructuralElementType.Column:
                    baseCover = 20.0;
                    break;

                case StructuralElementType.FoundationWithBlinding:
                    baseCover = 35.0; // TCVN quy định tối thiểu 35, thực tế thường dùng 40-50mm
                    break;

                case StructuralElementType.FoundationWithoutBlinding:
                    baseCover = 70.0;
                    break;

                default:
                    baseCover = 20.0;
                    break;
            }

            if (environment == ExposureEnvironment.OutdoorOrMoist)
            {
                baseCover += 5.0;
            }
            else if (environment == ExposureEnvironment.Corrosive)
            {
                baseCover += 15.0;
            }

            return baseCover;
        }

        /// <summary>
        /// Kiểm tra và điều chỉnh lớp bảo vệ để đảm bảo không nhỏ hơn đường kính cốt thép (c >= d).
        /// </summary>
        public static double ValidateCover(double coverMm, int barDiameterMm)
        {
            return Math.Max(coverMm, barDiameterMm);
        }
    }
}
