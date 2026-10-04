using System;
using System.Collections.Generic;
using JNNTool.RebarSuite.Beam.Models;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Beam.Engine
{
    /// <summary>
    /// Thuật toán thuần C# tính toán phân bổ cốt thép dầm liên tục (đơn vị mm, không gọi Revit API).
    /// </summary>
    public static class BeamLayoutEngine
    {
        public static BeamLayoutResult Calculate(ContinuousBeamModel beamModel, BeamRebarSettings settings)
        {
            var result = new BeamLayoutResult(beamModel, settings);
            if (beamModel.Spans == null || beamModel.Spans.Count == 0)
                return result;

            double cover = settings.CoverMm;
            var barMainTop = BarCatalog.Parse(settings.TopMainDiameter) ?? new BarInfo(20);
            var barMainBot = BarCatalog.Parse(settings.BottomMainDiameter) ?? new BarInfo(20);
            var barStirrup = BarCatalog.Parse(settings.StirrupDiameter) ?? new BarInfo(8);

            // ==============================================================
            // 1. Tính toán cốt đai cho từng nhịp dầm (Vùng dày L/4 - Vùng thưa)
            // ==============================================================
            for (int i = 0; i < beamModel.Spans.Count; i++)
            {
                var span = beamModel.Spans[i];
                CalculateSpanStirrups(span, settings, barStirrup, result);

                // Thép giá (cấu tạo thành dầm khi H >= 600mm)
                if (settings.AutoSideRebar && span.HeightMm >= settings.MinBeamHeightForSideRebar)
                {
                    CalculateSideRebars(span, settings, result);
                }

                // Thép gia cường nhịp (Bottom Add)
                if (settings.EnableBottomAdd && settings.BottomAddCount > 0)
                {
                    CalculateBottomAdd(span, settings, result);
                }
            }

            // ==============================================================
            // 2. Thép chủ trên và dưới chạy suốt chuỗi dầm
            // ==============================================================
            CalculateContinuousMainBars(beamModel, settings, barMainTop, barMainBot, result);

            // ==============================================================
            // 3. Thép gia cường gối (Top Add Rebar)
            // ==============================================================
            if (settings.EnableTopAdd && settings.TopAddCount > 0)
            {
                CalculateTopAddRebars(beamModel, settings, result);
            }

            return result;
        }

        /// <summary>
        /// Tính toán phân bố đai vùng dày (L/4) và vùng thưa giữa nhịp.
        /// </summary>
        public static void CalculateSpanStirrups(
            BeamSpanModel span,
            BeamRebarSettings settings,
            BarInfo barStirrup,
            BeamLayoutResult result)
        {
            double clearL = span.ClearSpanLengthMm;
            double cover = settings.CoverMm;

            // Chiều dài vùng đai dày gần gối (mặc định L/4)
            double denseLen = GeometryHelper.RoundUpToMultiple(clearL * settings.StirrupDenseRangeRatio, 50);
            double midLen = Math.Max(clearL - 2 * denseLen, 0);

            // 1. Đai dày gối trái
            GeometryHelper.CalculateSpacing(denseLen, 50, 0, settings.StirrupSpacingEnd, out int countDenseLeft, out double actualSpacingLeft);
            var stirrupLeft = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.StirrupDense,
                Diameter = barStirrup.Name,
                BarCount = countDenseLeft,
                SpacingMm = actualSpacingLeft,
                RangeLengthMm = denseLen,
                Description = $"Cốt đai dày gối trái (Nhịp {span.SpanIndex + 1})"
            };
            AddStirrupProfilePoints(stirrupLeft, span, cover);
            result.RebarSets.Add(stirrupLeft);

            // 2. Đai thưa giữa nhịp
            if (midLen > 100)
            {
                GeometryHelper.CalculateSpacing(midLen, 0, 0, settings.StirrupSpacingMid, out int countMid, out double actualSpacingMid);
                var stirrupMid = new BeamRebarSetLayoutInfo
                {
                    Role = BeamRebarRole.StirrupSparse,
                    Diameter = barStirrup.Name,
                    BarCount = countMid,
                    SpacingMm = actualSpacingMid,
                    RangeLengthMm = midLen,
                    Description = $"Cốt đai thưa giữa nhịp (Nhịp {span.SpanIndex + 1})"
                };
                AddStirrupProfilePoints(stirrupMid, span, cover);
                result.RebarSets.Add(stirrupMid);
            }

            // 3. Đai dày gối phải
            GeometryHelper.CalculateSpacing(denseLen, 0, 50, settings.StirrupSpacingEnd, out int countDenseRight, out double actualSpacingRight);
            var stirrupRight = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.StirrupDense,
                Diameter = barStirrup.Name,
                BarCount = countDenseRight,
                SpacingMm = actualSpacingRight,
                RangeLengthMm = denseLen,
                Description = $"Cốt đai dày gối phải (Nhịp {span.SpanIndex + 1})"
            };
            AddStirrupProfilePoints(stirrupRight, span, cover);
            result.RebarSets.Add(stirrupRight);
        }

        private static void AddStirrupProfilePoints(BeamRebarSetLayoutInfo set, BeamSpanModel span, double cover)
        {
            double halfW = span.WidthMm / 2.0 - cover;
            double halfH = span.HeightMm / 2.0 - cover;

            // Mặt cắt đai dạng hình chữ nhật khép kín (Y, Z cục bộ)
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, 0));
            set.CurvePoints.Add(new Point3D(halfW, -halfH, 0));
            set.CurvePoints.Add(new Point3D(halfW, halfH, 0));
            set.CurvePoints.Add(new Point3D(-halfW, halfH, 0));
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, 0));
        }

        /// <summary>
        /// Tính toán thép chủ trên và dưới chạy suốt chuỗi dầm liên tục.
        /// </summary>
        private static void CalculateContinuousMainBars(
            ContinuousBeamModel beamModel,
            BeamRebarSettings settings,
            BarInfo barTop,
            BarInfo barBot,
            BeamLayoutResult result)
        {
            double totalLen = beamModel.TotalLengthMm;
            double anchorLenTop = settings.AnchorLengthMultiplier * barTop.DiameterMm;
            double anchorLenBot = settings.AnchorLengthMultiplier * barBot.DiameterMm;

            // Thép chủ trên
            var mainTop = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.MainTop,
                Diameter = barTop.Name,
                BarCount = settings.TopMainCount,
                Description = $"Thép chủ trên chạy suốt {settings.TopMainCount}{barTop.Name}"
            };
            // Bẻ neo 2 đầu cắm xuống gối
            mainTop.CurvePoints.Add(new Point3D(0, 0, -anchorLenTop));
            mainTop.CurvePoints.Add(new Point3D(0, 0, 0));
            mainTop.CurvePoints.Add(new Point3D(totalLen, 0, 0));
            mainTop.CurvePoints.Add(new Point3D(totalLen, 0, -anchorLenTop));
            result.RebarSets.Add(mainTop);

            // Thép chủ dưới
            var mainBot = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.MainBottom,
                Diameter = barBot.Name,
                BarCount = settings.BottomMainCount,
                Description = $"Thép chủ dưới chạy suốt {settings.BottomMainCount}{barBot.Name}"
            };
            // Bẻ neo 2 đầu móc lên
            mainBot.CurvePoints.Add(new Point3D(0, 0, anchorLenBot));
            mainBot.CurvePoints.Add(new Point3D(0, 0, 0));
            mainBot.CurvePoints.Add(new Point3D(totalLen, 0, 0));
            mainBot.CurvePoints.Add(new Point3D(totalLen, 0, anchorLenBot));
            result.RebarSets.Add(mainBot);
        }

        /// <summary>
        /// Tính toán thép gia cường gối (Top Add Rebar) tại các vị trí gối tựa.
        /// </summary>
        private static void CalculateTopAddRebars(
            ContinuousBeamModel beamModel,
            BeamRebarSettings settings,
            BeamLayoutResult result)
        {
            var barTopAdd = BarCatalog.Parse(settings.TopAddDiameter) ?? new BarInfo(20);

            // Duyệt qua các gối giữa các nhịp
            double currentDist = 0;
            for (int i = 0; i < beamModel.Spans.Count - 1; i++)
            {
                currentDist += beamModel.Spans[i].LengthMm;

                double spanLeft = beamModel.Spans[i].ClearSpanLengthMm;
                double spanRight = beamModel.Spans[i + 1].ClearSpanLengthMm;
                double maxSpan = Math.Max(spanLeft, spanRight);

                // Chiều dài vươn = max(L1, L2) * 0.25 (L/4)
                double cantileverL = GeometryHelper.RoundUpToMultiple(maxSpan * settings.TopAddSpanRatio, 50);

                var topAdd = new BeamRebarSetLayoutInfo
                {
                    Role = BeamRebarRole.TopAddSupport,
                    Diameter = barTopAdd.Name,
                    BarCount = settings.TopAddCount,
                    Description = $"Thép gia cường gối {i + 1} ({settings.TopAddCount}{barTopAdd.Name} L={cantileverL * 2})"
                };

                topAdd.CurvePoints.Add(new Point3D(currentDist - cantileverL, 0, 0));
                topAdd.CurvePoints.Add(new Point3D(currentDist + cantileverL, 0, 0));
                result.RebarSets.Add(topAdd);
            }
        }

        /// <summary>
        /// Tính toán thép gia cường nhịp (Bottom Add Rebar) tại giữa nhịp.
        /// </summary>
        private static void CalculateBottomAdd(
            BeamSpanModel span,
            BeamRebarSettings settings,
            BeamLayoutResult result)
        {
            var barBotAdd = BarCatalog.Parse(settings.BottomAddDiameter) ?? new BarInfo(20);
            double clearL = span.ClearSpanLengthMm;

            double offset = GeometryHelper.RoundUpToMultiple(clearL * settings.BottomAddOffsetRatio, 50);
            double addLen = clearL - 2 * offset;

            if (addLen > 300)
            {
                var botAdd = new BeamRebarSetLayoutInfo
                {
                    Role = BeamRebarRole.BottomAddMidSpan,
                    Diameter = barBotAdd.Name,
                    BarCount = settings.BottomAddCount,
                    Description = $"Thép gia cường nhịp {span.SpanIndex + 1} ({settings.BottomAddCount}{barBotAdd.Name} L={addLen})"
                };

                botAdd.CurvePoints.Add(new Point3D(offset, 0, 0));
                botAdd.CurvePoints.Add(new Point3D(offset + addLen, 0, 0));
                result.RebarSets.Add(botAdd);
            }
        }

        /// <summary>
        /// Tính toán thép giá (cấu tạo thành dầm) khi H >= 600mm.
        /// </summary>
        private static void CalculateSideRebars(
            BeamSpanModel span,
            BeamRebarSettings settings,
            BeamLayoutResult result)
        {
            var barSide = BarCatalog.Parse(settings.SideRebarDiameter) ?? new BarInfo(12);
            double innerH = span.HeightMm - 2 * settings.CoverMm;

            // Số tầng thép giá theo chiều cao dầm
            int layerCount = (int)Math.Floor(innerH / settings.MaxSideRebarSpacing);
            if (layerCount < 1) layerCount = 1;

            var sideRebar = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.SideRebar,
                Diameter = barSide.Name,
                BarCount = layerCount * 2, // Mỗi tầng 2 thanh ở 2 bên thành
                Description = $"Thép giá cấu tạo thành dầm {layerCount * 2}{barSide.Name}"
            };

            sideRebar.CurvePoints.Add(new Point3D(0, 0, 0));
            sideRebar.CurvePoints.Add(new Point3D(span.LengthMm, 0, 0));
            result.RebarSets.Add(sideRebar);
        }
    }
}
