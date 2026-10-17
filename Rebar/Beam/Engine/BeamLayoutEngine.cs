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

            // Tính vị trí đầu nhịp dọc theo chuỗi dầm
            double spanStartX = 0;
            for (int idx = 0; idx < result.BeamModel.Spans.Count; idx++)
            {
                if (ReferenceEquals(result.BeamModel.Spans[idx], span) || result.BeamModel.Spans[idx].SpanIndex == span.SpanIndex)
                    break;
                spanStartX += result.BeamModel.Spans[idx].LengthMm;
            }
            double clearStartX = spanStartX + span.LeftSupportWidthMm / 2.0;

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
            AddStirrupProfilePoints(stirrupLeft, span, cover, clearStartX + 50.0);
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
                AddStirrupProfilePoints(stirrupMid, span, cover, clearStartX + denseLen);
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
            AddStirrupProfilePoints(stirrupRight, span, cover, clearStartX + clearL - denseLen);
            result.RebarSets.Add(stirrupRight);
        }

        private static void AddStirrupProfilePoints(BeamRebarSetLayoutInfo set, BeamSpanModel span, double cover, double longitudinalOffsetMm = 0)
        {
            double halfW = span.WidthMm / 2.0 - cover;
            double halfH = span.HeightMm / 2.0 - cover;

            // Mặt cắt đai dạng hình chữ nhật khép kín (X: bề rộng dầm, Y: chiều cao dầm, Z: vị trí dọc trục dầm)
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, longitudinalOffsetMm));
            set.CurvePoints.Add(new Point3D(halfW, -halfH, longitudinalOffsetMm));
            set.CurvePoints.Add(new Point3D(halfW, halfH, longitudinalOffsetMm));
            set.CurvePoints.Add(new Point3D(-halfW, halfH, longitudinalOffsetMm));
            set.CurvePoints.Add(new Point3D(-halfW, -halfH, longitudinalOffsetMm));
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
            double firstSpanH = beamModel.Spans.Count > 0 ? beamModel.Spans[0].HeightMm : 500.0;
            double firstSpanW = beamModel.Spans.Count > 0 ? beamModel.Spans[0].WidthMm : 300.0;
            double zTop = firstSpanH / 2.0 - settings.CoverMm - barTop.DiameterMm / 2.0;
            double zBot = -firstSpanH / 2.0 + settings.CoverMm + barBot.DiameterMm / 2.0;
            double yStart = -firstSpanW / 2.0 + settings.CoverMm + barTop.DiameterMm / 2.0;
            double usableW = Math.Max(firstSpanW - 2 * settings.CoverMm - barTop.DiameterMm, 0);

            // Thép chủ trên
            var mainTop = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.MainTop,
                Diameter = barTop.Name,
                BarCount = settings.TopMainCount,
                SpacingMm = settings.TopMainCount > 1 ? usableW / (settings.TopMainCount - 1) : 0,
                Description = $"Thép chủ trên chạy suốt {settings.TopMainCount}{barTop.Name}"
            };
            // Bẻ neo 2 đầu cắm xuống gối
            mainTop.CurvePoints.Add(new Point3D(settings.CoverMm, yStart, zTop - anchorLenTop));
            mainTop.CurvePoints.Add(new Point3D(settings.CoverMm, yStart, zTop));
            mainTop.CurvePoints.Add(new Point3D(totalLen - settings.CoverMm, yStart, zTop));
            mainTop.CurvePoints.Add(new Point3D(totalLen - settings.CoverMm, yStart, zTop - anchorLenTop));
            result.RebarSets.Add(mainTop);

            // Thép chủ dưới
            var mainBot = new BeamRebarSetLayoutInfo
            {
                Role = BeamRebarRole.MainBottom,
                Diameter = barBot.Name,
                BarCount = settings.BottomMainCount,
                SpacingMm = settings.BottomMainCount > 1 ? usableW / (settings.BottomMainCount - 1) : 0,
                Description = $"Thép chủ dưới chạy suốt {settings.BottomMainCount}{barBot.Name}"
            };
            // Bẻ neo 2 đầu móc lên
            mainBot.CurvePoints.Add(new Point3D(settings.CoverMm, yStart, zBot + anchorLenBot));
            mainBot.CurvePoints.Add(new Point3D(settings.CoverMm, yStart, zBot));
            mainBot.CurvePoints.Add(new Point3D(totalLen - settings.CoverMm, yStart, zBot));
            mainBot.CurvePoints.Add(new Point3D(totalLen - settings.CoverMm, yStart, zBot + anchorLenBot));
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
                double zTopAdd = beamModel.Spans[i].HeightMm / 2.0 - settings.CoverMm - barTopAdd.DiameterMm * 1.5;
                double yStart = -beamModel.Spans[i].WidthMm / 2.0 + settings.CoverMm + barTopAdd.DiameterMm / 2.0;
                double usableW = Math.Max(beamModel.Spans[i].WidthMm - 2 * settings.CoverMm - barTopAdd.DiameterMm, 0);

                // Chiều dài vươn = max(L1, L2) * 0.25 (L/4)
                double cantileverL = GeometryHelper.RoundUpToMultiple(maxSpan * settings.TopAddSpanRatio, 50);

                var topAdd = new BeamRebarSetLayoutInfo
                {
                    Role = BeamRebarRole.TopAddSupport,
                    Diameter = barTopAdd.Name,
                    BarCount = settings.TopAddCount,
                    SpacingMm = settings.TopAddCount > 1 ? usableW / (settings.TopAddCount - 1) : 0,
                    Description = $"Thép gia cường gối {i + 1} ({settings.TopAddCount}{barTopAdd.Name} L={cantileverL * 2})"
                };

                topAdd.CurvePoints.Add(new Point3D(currentDist - cantileverL, yStart, zTopAdd));
                topAdd.CurvePoints.Add(new Point3D(currentDist + cantileverL, yStart, zTopAdd));
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

            double spanStartX = 0;
            for (int idx = 0; idx < result.BeamModel.Spans.Count; idx++)
            {
                if (ReferenceEquals(result.BeamModel.Spans[idx], span) || result.BeamModel.Spans[idx].SpanIndex == span.SpanIndex)
                    break;
                spanStartX += result.BeamModel.Spans[idx].LengthMm;
            }

            double offset = GeometryHelper.RoundUpToMultiple(clearL * settings.BottomAddOffsetRatio, 50);
            double addLen = clearL - 2 * offset;

            if (addLen > 300)
            {
                double zBotAdd = -span.HeightMm / 2.0 + settings.CoverMm + barBotAdd.DiameterMm * 1.5;
                double yStart = -span.WidthMm / 2.0 + settings.CoverMm + barBotAdd.DiameterMm / 2.0;
                double usableW = Math.Max(span.WidthMm - 2 * settings.CoverMm - barBotAdd.DiameterMm, 0);

                var botAdd = new BeamRebarSetLayoutInfo
                {
                    Role = BeamRebarRole.BottomAddMidSpan,
                    Diameter = barBotAdd.Name,
                    BarCount = settings.BottomAddCount,
                    SpacingMm = settings.BottomAddCount > 1 ? usableW / (settings.BottomAddCount - 1) : 0,
                    Description = $"Thép gia cường nhịp {span.SpanIndex + 1} ({settings.BottomAddCount}{barBotAdd.Name} L={addLen})"
                };

                double startX = spanStartX + span.LeftSupportWidthMm / 2.0 + offset;
                botAdd.CurvePoints.Add(new Point3D(startX, yStart, zBotAdd));
                botAdd.CurvePoints.Add(new Point3D(startX + addLen, yStart, zBotAdd));
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
