using System;
using System.Security.Cryptography;
using System.Text;
using JNNTool.Tools.IFCEtabs.Core.Models;

namespace JNNTool.Tools.IFCEtabs.Core.Services
{
    /// <summary>
    /// Sinh chuỗi mã băm (Hash) dựa trên các thuộc tính hình học và thông số kỹ thuật cốt lõi.
    /// Giúp phát hiện nhanh cấu kiện đã thay đổi (UPDATED) hay giữ nguyên (UNCHANGED).
    /// </summary>
    public class ElementHashService : IElementHashService
    {
        public static ElementHashService Instance { get; } = new ElementHashService();

        public string ComputeHash(StructuralElement element)
        {
            if (element == null) return string.Empty;

            var sb = new StringBuilder();
            sb.Append(element.ElementType).Append('|');
            sb.Append(element.SourceId).Append('|');
            sb.Append(element.Story).Append('|');
            sb.Append(element.Section).Append('|');
            sb.Append(element.Material).Append('|');
            sb.Append(Math.Round(element.Rotation, 2)).Append('|');

            // Bổ sung thuộc tính hình học riêng cho từng loại cấu kiện
            switch (element)
            {
                case ETABSBeam beam:
                    AppendPoint(sb, beam.StartPoint);
                    AppendPoint(sb, beam.EndPoint);
                    sb.Append(Math.Round(beam.WidthMm, 1)).Append('|');
                    sb.Append(Math.Round(beam.DepthMm, 1)).Append('|');
                    break;

                case ETABSColumn col:
                    AppendPoint(sb, col.BasePoint);
                    AppendPoint(sb, col.TopPoint);
                    sb.Append(Math.Round(col.AngleDegrees, 1)).Append('|');
                    sb.Append(Math.Round(col.WidthMm, 1)).Append('|');
                    sb.Append(Math.Round(col.DepthMm, 1)).Append('|');
                    break;

                case ETABSLevel lvl:
                    sb.Append(Math.Round(lvl.ElevationMm, 1)).Append('|');
                    sb.Append(Math.Round(lvl.HeightMm, 1)).Append('|');
                    break;

                case ETABSGrid grid:
                    AppendPoint(sb, grid.StartPoint);
                    AppendPoint(sb, grid.EndPoint);
                    sb.Append(grid.Direction).Append('|');
                    break;

                case ETABSWall wall:
                    sb.Append(Math.Round(wall.ThicknessMm, 1)).Append('|');
                    foreach (var pt in wall.BoundaryPoints)
                    {
                        AppendPoint(sb, pt);
                    }
                    break;

                case ETABSSlab slab:
                    sb.Append(Math.Round(slab.ThicknessMm, 1)).Append('|');
                    foreach (var pt in slab.OuterBoundary)
                    {
                        AppendPoint(sb, pt);
                    }
                    break;
            }

            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
            
            // Lấy 16 ký tự hexa viết hoa để ngắn gọn và dễ so sánh
            var hashSb = new StringBuilder(16);
            for (int i = 0; i < 8; i++)
            {
                hashSb.Append(hashBytes[i].ToString("X2"));
            }
            return hashSb.ToString();
        }

        private static void AppendPoint(StringBuilder sb, Geometry.Point3D pt)
        {
            sb.Append(Math.Round(pt.X, 1)).Append(':')
              .Append(Math.Round(pt.Y, 1)).Append(':')
              .Append(Math.Round(pt.Z, 1)).Append('|');
        }
    }
}
