using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace JNNTool.Tools.CutByFloor.Models
{
    public enum CutFloorScope
    {
        /// <summary>Tự động quét và khoét tất cả sàn to chứa sàn nhỏ trong View hiện hành.</summary>
        ActiveView,
        /// <summary>Quét chọn sàn thủ công trên màn hình (hoặc quét chọn trước).</summary>
        Selection
    }

    /// <summary>Điểm 2D trên mặt phẳng sàn (đơn vị feet theo Revit Internal Units).</summary>
    public class Pt2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Pt2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(Pt2D other) => Math.Sqrt(DistanceSqTo(other));

        public double DistanceSqTo(Pt2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return dx * dx + dy * dy;
        }

        public override string ToString() => $"({X:F3}, {Y:F3})";
    }

    /// <summary>Đoạn thẳng 2D vô hướng nối 2 đỉnh theo chỉ số trong Canonical Vertices.</summary>
    public class Seg2D
    {
        public int U { get; set; }
        public int V { get; set; }
        public Pt2D A { get; set; }
        public Pt2D B { get; set; }
    }

    /// <summary>Cạnh có hướng U -> V trên biên hình học sau phép trừ.</summary>
    public class DirectedEdge
    {
        public int U { get; set; }
        public int V { get; set; }
        public Pt2D U_Pt { get; set; }
        public Pt2D V_Pt { get; set; }

        public DirectedEdge(int u, int v, Pt2D uPt, Pt2D vPt)
        {
            U = u;
            V = v;
            U_Pt = uPt;
            V_Pt = vPt;
        }
    }

    /// <summary>Đa giác vùng sàn gồm 1 đường bao ngoài (Outer) và các lỗ mở (Holes).</summary>
    public class FloorPolyRegion
    {
        public List<Pt2D> Outer { get; set; } = new List<Pt2D>();
        public List<List<Pt2D>> Holes { get; set; } = new List<List<Pt2D>>();
    }

    /// <summary>Một mảng sàn độc lập sau khi cắt (gồm 1 biên ngoài CCW và các lỗ khoét CW bên trong).</summary>
    public class FloorIsland
    {
        public List<Pt2D> OuterLoop { get; set; } = new List<Pt2D>();
        public List<List<Pt2D>> HoleLoops { get; set; } = new List<List<Pt2D>>();
        public double Area { get; set; }
    }

    /// <summary>Thông tin trích xuất đầy đủ của một Floor phục vụ cắt & tái tạo.</summary>
    public class CutFloorInputInfo
    {
        public ElementId FloorId { get; set; }
        public ElementId FloorTypeId { get; set; }
        public string TypeName { get; set; } = "";
        public ElementId LevelId { get; set; }
        public double LevelElevation { get; set; }
        public double HeightOffset { get; set; }
        public double TopZ { get; set; }
        public int IsStructural { get; set; } = 1;
        public string Comments { get; set; }
        public string Mark { get; set; }

        public List<CurveLoop> RawLoops { get; set; } = new List<CurveLoop>();
        public FloorPolyRegion PolyRegion { get; set; } = new FloorPolyRegion();
        public bool HasCurvedEdges { get; set; }
        public double AreaApprox { get; set; }
        public BoundingBoxUV Bounds2D { get; set; }

        public List<JoinedElementRecord> JoinedElements { get; set; } = new List<JoinedElementRecord>();
    }

    public class JoinedElementRecord
    {
        public ElementId JoinedId { get; set; }
        public bool FloorWasCutting { get; set; }
    }

    /// <summary>Kết quả cắt của một Sàn To (Target Floor).</summary>
    public class CutFloorResult
    {
        public ElementId TargetFloorId { get; set; }
        public string TargetTypeName { get; set; } = "";
        public double OriginalArea { get; set; }
        public double NewArea { get; set; }
        public int CuttersCount { get; set; }
        public int HolesCreatedCount { get; set; }
        public int IslandsCreatedCount { get; set; }
        public bool Success { get; set; }
        public string Note { get; set; } = "";
        public List<ElementId> CreatedFloorIds { get; set; } = new List<ElementId>();
    }
}
