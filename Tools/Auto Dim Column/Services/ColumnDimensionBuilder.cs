#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.AutoDimColumn.Engine;
using JNNTool.Tools.AutoDimColumn.Models;

namespace JNNTool.Tools.AutoDimColumn.Services
{
    public class ColumnDimExecutionReport
    {
        public int TotalColumns { get; set; }
        public int SuccessDims { get; set; }
        public int SkippedDims { get; set; }
        public int FailedCount { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }

    /// <summary>
    /// Thực thi tạo Dimension trong Revit Document theo các tính toán của Engine.
    /// </summary>
    public static class ColumnDimensionBuilder
    {
        public static ColumnDimExecutionReport CreateDimensions(
            Document doc,
            View activeView,
            List<ColumnGeometryInfo> columns,
            AutoDimColumnSettings settings)
        {
            var report = new ColumnDimExecutionReport
            {
                TotalColumns = columns.Count
            };

            // Tìm DimensionType phù hợp
            DimensionType? dimType = ResolveDimensionType(doc, settings.SelectedDimensionTypeId);

            // Thu thập các Reference đã tồn tại nếu bật chế độ Skip Existing
            var existingSignatures = new HashSet<string>();
            if (settings.SkipExisting)
            {
                CollectExistingDimSignatures(doc, activeView, existingSignatures);
            }

            double z = activeView.Origin != null ? activeView.Origin.Z : ((activeView as ViewPlan)?.GenLevel?.Elevation ?? 0.0);
            double baseOffsetFt = AutoDimColumnEngine.MmToFt(settings.BaseOffsetMm);
            double chainSpacingFt = AutoDimColumnEngine.MmToFt(settings.ChainSpacingMm);

            foreach (var col in columns)
            {
                if (!col.IsSupported)
                {
                    report.SkippedDims++;
                    report.Messages.Add($"• Cột {col.Mark} (Id {GetIdValue(col.ColumnId)}): Bỏ qua ({col.StatusMessage})");
                    continue;
                }

                XYZ center = col.Center;

                // ─────────────────────────────────────────────────────────────
                // 1. Dimension Bề Rộng (Width) theo phương U
                // ─────────────────────────────────────────────────────────────
                if (settings.DimWidth && col.WidthPair != null && col.WidthPair.IsValid)
                {
                    try
                    {
                        XYZ measureDir = col.WidthPair.Normal;
                        // Phương dịch chuyển offset (vuông góc với phương đo)
                        XYZ offsetDir = col.LengthPair != null ? col.LengthPair.Normal : new XYZ(-measureDir.Y, measureDir.X, 0);
                        offsetDir = OrientOffsetDir(offsetDir, activeView, settings.PlacementSide);

                        double halfOtherFt = col.BoundingBox != null
                            ? Math.Abs(col.BoundingBox.Max.Y - col.BoundingBox.Min.Y) * 0.5
                            : (col.LengthMm > 0 ? AutoDimColumnEngine.MmToFt(col.LengthMm / 2.0) : 0.5);
                        double totalOffset = halfOtherFt + baseOffsetFt;

                        double halfExtent = col.BoundingBox != null
                            ? Math.Abs(col.BoundingBox.Max.X - col.BoundingBox.Min.X) * 0.5
                            : col.WidthPair.DistanceFt / 2.0;
                        var (sx, sy, ex, ey) = AutoDimColumnEngine.CalculateDimLine2D(
                            center.X, center.Y,
                            measureDir.X, measureDir.Y,
                            offsetDir.X, offsetDir.Y,
                            totalOffset,
                            -halfExtent, halfExtent,
                            margin: 0.3
                        );

                        var refArr = new ReferenceArray();
                        refArr.Append(col.WidthPair.Ref1);
                        refArr.Append(col.WidthPair.Ref2);

                        string sig = AutoDimColumnEngine.BuildDimSignature(
                            new[] { col.WidthPair.Ref1!.ConvertToStableRepresentation(doc), col.WidthPair.Ref2!.ConvertToStableRepresentation(doc) }, 0);

                        if (settings.SkipExisting && existingSignatures.Contains(sig))
                        {
                            report.SkippedDims++;
                        }
                        else
                        {
                            Line line = Line.CreateBound(new XYZ(sx, sy, z), new XYZ(ex, ey, z));
                            Dimension? dim = dimType != null
                                ? doc.Create.NewDimension(activeView, line, refArr, dimType)
                                : doc.Create.NewDimension(activeView, line, refArr);

                            if (dim != null)
                            {
                                report.SuccessDims++;
                                existingSignatures.Add(sig);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report.FailedCount++;
                        report.Messages.Add($"• Cột {col.Mark} [Dim Width]: {ex.Message}");
                    }
                }

                // ─────────────────────────────────────────────────────────────
                // 2. Dimension Chiều Dài (Length) theo phương V
                // ─────────────────────────────────────────────────────────────
                if (settings.DimLength && col.LengthPair != null && col.LengthPair.IsValid)
                {
                    try
                    {
                        XYZ measureDir = col.LengthPair.Normal;
                        XYZ offsetDir = col.WidthPair != null ? col.WidthPair.Normal : new XYZ(-measureDir.Y, measureDir.X, 0);
                        offsetDir = OrientOffsetDir(offsetDir, activeView, settings.PlacementSide);

                        double halfOtherFt = col.BoundingBox != null
                            ? Math.Abs(col.BoundingBox.Max.X - col.BoundingBox.Min.X) * 0.5
                            : (col.WidthMm > 0 ? AutoDimColumnEngine.MmToFt(col.WidthMm / 2.0) : 0.5);
                        double totalOffset = halfOtherFt + baseOffsetFt;

                        double halfExtent = col.BoundingBox != null
                            ? Math.Abs(col.BoundingBox.Max.Y - col.BoundingBox.Min.Y) * 0.5
                            : col.LengthPair.DistanceFt / 2.0;
                        var (sx, sy, ex, ey) = AutoDimColumnEngine.CalculateDimLine2D(
                            center.X, center.Y,
                            measureDir.X, measureDir.Y,
                            offsetDir.X, offsetDir.Y,
                            totalOffset,
                            -halfExtent, halfExtent,
                            margin: 0.3
                        );

                        var refArr = new ReferenceArray();
                        refArr.Append(col.LengthPair.Ref1);
                        refArr.Append(col.LengthPair.Ref2);

                        string sig = AutoDimColumnEngine.BuildDimSignature(
                            new[] { col.LengthPair.Ref1!.ConvertToStableRepresentation(doc), col.LengthPair.Ref2!.ConvertToStableRepresentation(doc) }, 1);

                        if (settings.SkipExisting && existingSignatures.Contains(sig))
                        {
                            report.SkippedDims++;
                        }
                        else
                        {
                            Line line = Line.CreateBound(new XYZ(sx, sy, z), new XYZ(ex, ey, z));
                            Dimension? dim = dimType != null
                                ? doc.Create.NewDimension(activeView, line, refArr, dimType)
                                : doc.Create.NewDimension(activeView, line, refArr);

                            if (dim != null)
                            {
                                report.SuccessDims++;
                                existingSignatures.Add(sig);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report.FailedCount++;
                        report.Messages.Add($"• Cột {col.Mark} [Dim Length]: {ex.Message}");
                    }
                }

                // ─────────────────────────────────────────────────────────────
                // 3. Dimension từ Grid đến Cột (Grid-to-Column)
                // ─────────────────────────────────────────────────────────────
                if (settings.DimGridToColumn && col.AssociatedGrids.Count > 0)
                {
                    foreach (var grid in col.AssociatedGrids)
                    {
                        if (grid.GridReference == null) continue;

                        try
                        {
                            ColumnFacePair? targetPair = grid.TargetDirection == 0 ? col.WidthPair : col.LengthPair;
                            if (targetPair == null || !targetPair.IsValid) continue;

                            XYZ measureDir = targetPair.Normal;
                            XYZ offsetDir = grid.TargetDirection == 0
                                ? (col.LengthPair != null ? col.LengthPair.Normal : new XYZ(-measureDir.Y, measureDir.X, 0))
                                : (col.WidthPair != null ? col.WidthPair.Normal : new XYZ(-measureDir.Y, measureDir.X, 0));
                            offsetDir = OrientOffsetDir(offsetDir, activeView, settings.PlacementSide);

                            // Lớp chuỗi dim định vị nằm bên ngoài lớp dim tiết diện một khoảng chainSpacing
                            double halfOtherFt = col.BoundingBox != null
                                ? (grid.TargetDirection == 0 ? Math.Abs(col.BoundingBox.Max.Y - col.BoundingBox.Min.Y) * 0.5 : Math.Abs(col.BoundingBox.Max.X - col.BoundingBox.Min.X) * 0.5)
                                : ((grid.TargetDirection == 0 ? col.LengthMm : col.WidthMm) > 0
                                    ? AutoDimColumnEngine.MmToFt((grid.TargetDirection == 0 ? col.LengthMm : col.WidthMm) / 2.0)
                                    : 0.5);
                            double totalOffset = halfOtherFt + baseOffsetFt + chainSpacingFt;

                            // Sắp xếp mặt gần Grid nhất và mặt xa Grid nhất
                            Reference nearRef = targetPair.Ref1!;
                            Reference farRef = targetPair.Ref2!;

                            var refList = new List<Reference> { grid.GridReference, nearRef };
                            if (settings.IncludeOppositeFace)
                            {
                                refList.Add(farRef);
                            }

                            var refArr = new ReferenceArray();
                            foreach (var r in refList) refArr.Append(r);

                            string sig = AutoDimColumnEngine.BuildDimSignature(
                                refList.Select(r => r.ConvertToStableRepresentation(doc)),
                                10 + grid.TargetDirection);

                            if (settings.SkipExisting && existingSignatures.Contains(sig))
                            {
                                report.SkippedDims++;
                            }
                            else
                            {
                                double spanExtent = grid.PerpendicularDistanceFt + targetPair.DistanceFt;
                                var (sx, sy, ex, ey) = AutoDimColumnEngine.CalculateDimLine2D(
                                    center.X, center.Y,
                                    measureDir.X, measureDir.Y,
                                    offsetDir.X, offsetDir.Y,
                                    totalOffset,
                                    -spanExtent, spanExtent,
                                    margin: 0.5
                                );

                                Line line = Line.CreateBound(new XYZ(sx, sy, z), new XYZ(ex, ey, z));
                                Dimension? dim = dimType != null
                                    ? doc.Create.NewDimension(activeView, line, refArr, dimType)
                                    : doc.Create.NewDimension(activeView, line, refArr);

                                if (dim != null)
                                {
                                    report.SuccessDims++;
                                    existingSignatures.Add(sig);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            report.FailedCount++;
                            report.Messages.Add($"• Cột {col.Mark} [Grid {grid.GridName}]: {ex.Message}");
                        }
                    }
                }
            }

            return report;
        }

        private static XYZ OrientOffsetDir(XYZ dir, View view, ColumnPlacementSide side)
        {
            XYZ d = new XYZ(dir.X, dir.Y, 0).Normalize();
            double up = d.DotProduct(view.UpDirection);
            double right = d.DotProduct(view.RightDirection);

            switch (side)
            {
                case ColumnPlacementSide.TopLeft:
                    if (up < -1e-4 || (Math.Abs(up) <= 1e-4 && right > 1e-4))
                        return d.Negate();
                    return d;

                case ColumnPlacementSide.BottomRight:
                    if (up > 1e-4 || (Math.Abs(up) <= 1e-4 && right < -1e-4))
                        return d.Negate();
                    return d;

                case ColumnPlacementSide.Auto:
                default:
                    // Mặc định ưu tiên hướng lên trên hoặc sang phải
                    if (up < -1e-4 || (Math.Abs(up) <= 1e-4 && right < -1e-4))
                        return d.Negate();
                    return d;
            }
        }

        private static DimensionType? ResolveDimensionType(Document doc, string selectedTypeId)
        {
            if (!string.IsNullOrEmpty(selectedTypeId) && long.TryParse(selectedTypeId, out long idVal))
            {
#if NET48
                var targetId = new ElementId((int)idVal);
#else
                var targetId = new ElementId(idVal);
#endif
                if (doc.GetElement(targetId) is DimensionType dt)
                {
                    return dt;
                }
            }

            return new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .FirstOrDefault(d => d.StyleType == DimensionStyleType.Linear);
        }

        private static void CollectExistingDimSignatures(Document doc, View view, HashSet<string> signatures)
        {
            var dims = new FilteredElementCollector(doc, view.Id)
                .OfClass(typeof(Dimension))
                .Cast<Dimension>();

            foreach (var d in dims)
            {
                try
                {
                    var refs = d.References;
                    if (refs == null || refs.Size < 2) continue;

                    var stableKeys = new List<string>();
                    foreach (Reference r in refs)
                    {
                        stableKeys.Add(r.ConvertToStableRepresentation(doc));
                    }

                    signatures.Add(AutoDimColumnEngine.BuildDimSignature(stableKeys, 0));
                    signatures.Add(AutoDimColumnEngine.BuildDimSignature(stableKeys, 1));
                    signatures.Add(AutoDimColumnEngine.BuildDimSignature(stableKeys, 10));
                    signatures.Add(AutoDimColumnEngine.BuildDimSignature(stableKeys, 11));
                }
                catch
                {
                    // Bỏ qua dimension không đọc được reference
                }
            }
        }

        private static long GetIdValue(ElementId id)
        {
#if NET48
            return id.IntegerValue;
#else
            return id.Value;
#endif
        }
    }
}
