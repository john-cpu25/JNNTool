using System;
using System.Collections.Generic;

namespace JNNTool.Tools.CSIxRevit.Models
{
    /// <summary>Hộp trục chuẩn (axis-aligned), đơn vị mm.</summary>
    public sealed class BoxMm
    {
        public double MinX, MinY, MinZ, MaxX, MaxY, MaxZ;

        public BoxMm() { }
        public BoxMm(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            MinX = minX; MinY = minY; MinZ = minZ; MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        public double SizeX => MaxX - MinX;
        public double SizeY => MaxY - MinY;
        public double SizeZ => MaxZ - MinZ;
        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;
        public double CenterZ => (MinZ + MaxZ) / 2.0;
    }

    public sealed class ScopeBoxFitResult
    {
        public bool HasBuilding { get; set; }
        /// <summary>Khung cần bao (công trình + margin).</summary>
        public BoxMm Required { get; set; } = new BoxMm();
        /// <summary>Vector dời hộp mẫu để tâm trùng tâm công trình (mm).</summary>
        public double MoveX { get; set; }
        public double MoveY { get; set; }
        public double MoveZ { get; set; }
        public bool FitsPlan { get; set; }
        public bool FitsHeight { get; set; }
        /// <summary>Phần thiếu mỗi phương (mm, 0 nếu đủ) để báo người dùng kéo thêm.</summary>
        public double ShortX { get; set; }
        public double ShortY { get; set; }
        public double ShortZ { get; set; }
    }

    /// <summary>
    /// Tính vị trí đặt Scope Box. Revit API không cho tạo / đổi kích thước Scope Box,
    /// nên chỉ dời hộp có sẵn về tâm công trình và kiểm tra hộp có đủ bao hay không.
    /// </summary>
    public static class ScopeBoxPlanner
    {
        public static ScopeBoxFitResult Plan(
            IEnumerable<double> xsMm, IEnumerable<double> ysMm,
            double minZMm, double maxZMm,
            BoxMm current, double marginMm)
        {
            var result = new ScopeBoxFitResult();
            if (current == null) return result;

            double minX = double.MaxValue, maxX = double.MinValue;
            double minY = double.MaxValue, maxY = double.MinValue;
            if (xsMm != null) foreach (var x in xsMm) { if (x < minX) minX = x; if (x > maxX) maxX = x; }
            if (ysMm != null) foreach (var y in ysMm) { if (y < minY) minY = y; if (y > maxY) maxY = y; }
            if (minX > maxX || minY > maxY) return result;

            if (minZMm > maxZMm) { var t = minZMm; minZMm = maxZMm; maxZMm = t; }
            double m = Math.Max(0, marginMm);

            var req = new BoxMm(minX - m, minY - m, minZMm - m, maxX + m, maxY + m, maxZMm + m);
            result.HasBuilding = true;
            result.Required = req;

            result.MoveX = req.CenterX - current.CenterX;
            result.MoveY = req.CenterY - current.CenterY;
            result.MoveZ = req.CenterZ - current.CenterZ;

            const double tol = 1.0; // mm
            result.ShortX = Math.Max(0, req.SizeX - current.SizeX);
            result.ShortY = Math.Max(0, req.SizeY - current.SizeY);
            result.ShortZ = Math.Max(0, req.SizeZ - current.SizeZ);
            result.FitsPlan = result.ShortX <= tol && result.ShortY <= tol;
            result.FitsHeight = result.ShortZ <= tol;
            if (result.FitsPlan) { result.ShortX = 0; result.ShortY = 0; }
            if (result.FitsHeight) result.ShortZ = 0;
            return result;
        }
    }
}
