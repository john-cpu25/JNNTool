using System.Collections.Generic;

namespace JNN.Stair.Models
{
    /// <summary>
    /// Thông số hình học trích xuất từ mặt cắt (Section View) cắt qua cầu thang
    /// </summary>
    public class StairSectionData
    {
        public string ViewName { get; set; } = string.Empty;
        public int SectionIndex { get; set; }

        // Bề rộng dải vế thang (phương vuông góc với mặt cắt)
        public double WidthMm { get; set; } = 1000.0;
        public double ConcreteCoverMm { get; set; } = 15.0;

        // Điểm pick bắt đầu và kết thúc của mặt cắt
        public StairPoint3D PickedStartPoint { get; set; }
        public StairPoint3D PickedEndPoint { get; set; }

        // Vector hệ trục tọa độ của mặt cắt
        public StairPoint3D ViewNormal { get; set; }
        public StairPoint3D ViewRight { get; set; }
        public StairPoint3D ViewUp { get; set; }

        // Danh sách các điểm đa giác đường bao bê tông bản thang
        public List<StairPoint3D> ConcreteOutline { get; set; } = new();

        // Danh sách đa giác đường bao dầm chiếu nghỉ / dầm chân gối
        public List<List<StairPoint3D>> SupportBeamOutlines { get; set; } = new();

        // Các thanh thép tính toán thuộc mặt cắt này
        public List<StairRebarData> Rebars { get; set; } = new();
    }
}
