namespace JNNTool.Tools.ElementMark.Models
{
    // Toạ độ đã chiếu lên hệ trục của view (X = Right, Y = Up), đơn vị mm.
    // Engine chỉ làm việc với các POCO này, không chạm Revit API.

    public class BeamItem
    {
        public long Id { get; set; }
        public string LevelName { get; set; } = "";
        public double X1 { get; set; }
        public double Y1 { get; set; }
        public double X2 { get; set; }
        public double Y2 { get; set; }

        public BeamItem() { }

        public BeamItem(long id, string levelName, double x1, double y1, double x2, double y2)
        {
            Id = id;
            LevelName = levelName ?? "";
            X1 = x1; Y1 = y1; X2 = x2; Y2 = y2;
        }
    }

    public class ColumnItem
    {
        public long Id { get; set; }
        public string LevelName { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        /// <summary>Cao độ chân cột (mm), dùng để gom chuỗi từ móng lên mái.</summary>
        public double BaseZ { get; set; }

        public ColumnItem() { }

        public ColumnItem(long id, string levelName, double x, double y, double baseZ)
        {
            Id = id;
            LevelName = levelName ?? "";
            X = x; Y = y; BaseZ = baseZ;
        }
    }

    public class FloorItem
    {
        public long Id { get; set; }
        public string LevelName { get; set; } = "";

        public FloorItem() { }

        public FloorItem(long id, string levelName)
        {
            Id = id;
            LevelName = levelName ?? "";
        }
    }

    public class LevelInfo
    {
        public string Name { get; set; } = "";
        public double ElevationMm { get; set; }

        public LevelInfo() { }

        public LevelInfo(string name, double elevationMm)
        {
            Name = name ?? "";
            ElevationMm = elevationMm;
        }
    }

    /// <summary>Kết quả tính Mark cho một phần tử.</summary>
    public class MarkAssignment
    {
        public long Id { get; set; }
        public MarkCategory Category { get; set; }
        public string LevelName { get; set; } = "";
        /// <summary>Ký hiệu phương (X/Y) đã áp dụng, rỗng với cột và sàn.</summary>
        public string Axis { get; set; } = "";
        public int Number { get; set; }
        /// <summary>Số thứ tự nhịp trong dầm liên tục, 0 nếu dầm một nhịp.</summary>
        public int Span { get; set; }
        public string Mark { get; set; } = "";
    }
}
