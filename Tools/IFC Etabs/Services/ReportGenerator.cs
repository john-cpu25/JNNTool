using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using JNNTool.Core.Compat;
using JNNTool.Tools.IFCEtabs.Core.Models;
using JNNTool.Tools.IFCEtabs.Revit.Tracking;

namespace JNNTool.Tools.IFCEtabs.Services
{
    /// <summary>
    /// Bộ máy xuất báo cáo nghiệm thu và đối soát chất lượng (QA Reports) định dạng HTML, CSV, JSON.
    /// Thiết kế Decoupled: Có thể sinh báo cáo độc lập (thuần C#) hoặc từ Revit Document trực tiếp.
    /// </summary>
    public class ReportGenerator
    {
        public static string GenerateHtmlReport(
            Document? doc,
            StructuralModel model,
            ElementTracker? tracker,
            ValidationSummary validation,
            ConversionReportResult result,
            string outputPath)
        {
            Dictionary<string, string>? idMap = null;
            if (tracker != null)
            {
                idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in tracker.TrackedElements)
                {
                    idMap[kvp.Key] = kvp.Value.Id.GetIdValue().ToString();
                }
            }
            return GenerateHtmlReport(doc?.Title, model, idMap, validation, result, outputPath);
        }

        public static string GenerateCsvReport(
            Document? doc,
            StructuralModel model,
            ElementTracker? tracker,
            ConversionReportResult result,
            string outputPath)
        {
            Dictionary<string, string>? idMap = null;
            if (tracker != null)
            {
                idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in tracker.TrackedElements)
                {
                    idMap[kvp.Key] = kvp.Value.Id.GetIdValue().ToString();
                }
            }
            return GenerateCsvReport(model, idMap, result, outputPath);
        }

        public static string GenerateHtmlReport(
            string? docTitle,
            StructuralModel model,
            IReadOnlyDictionary<string, string>? elementIdMap,
            ValidationSummary validation,
            ConversionReportResult result,
            string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"vi\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <title>Báo Cáo Nghiệm Thu Chuyển Đổi - JNN ETABS Converter</title>");
            sb.AppendLine("  <style>");
            sb.AppendLine("    body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background: #181818; color: #E0E0E0; margin: 0; padding: 24px; line-height: 1.5; }");
            sb.AppendLine("    .container { max-width: 1100px; margin: 0 auto; background: #242424; border-radius: 8px; padding: 24px; box-shadow: 0 4px 20px rgba(0,0,0,0.5); }");
            sb.AppendLine("    h1 { color: #00AEEF; font-size: 22px; margin-top: 0; border-bottom: 2px solid #333; padding-bottom: 12px; }");
            sb.AppendLine("    h2 { color: #00FFCC; font-size: 16px; margin-top: 24px; border-left: 4px solid #00AEEF; padding-left: 8px; }");
            sb.AppendLine("    .kpi-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 12px; margin: 16px 0; }");
            sb.AppendLine("    .kpi-card { background: #2E2E2E; border-radius: 6px; padding: 12px; text-align: center; border: 1px solid #3A3A3A; }");
            sb.AppendLine("    .kpi-num { font-size: 24px; font-weight: bold; color: #FFFFFF; }");
            sb.AppendLine("    .kpi-label { font-size: 11px; color: #888888; text-transform: uppercase; margin-top: 4px; }");
            sb.AppendLine("    table { width: 100%; border-collapse: collapse; margin-top: 10px; font-size: 13px; }");
            sb.AppendLine("    th, td { padding: 10px 12px; text-align: left; border-bottom: 1px solid #3A3A3A; }");
            sb.AppendLine("    th { background: #2E2E2E; color: #00AEEF; font-weight: 600; }");
            sb.AppendLine("    tr:hover { background: #2A2A2A; }");
            sb.AppendLine("    .badge { display: inline-block; padding: 3px 8px; border-radius: 4px; font-size: 11px; font-weight: bold; }");
            sb.AppendLine("    .badge-pass { background: #1B5E20; color: #81C784; }");
            sb.AppendLine("    .badge-warn { background: #E65100; color: #FFB74D; }");
            sb.AppendLine("    .badge-fail { background: #B71C1C; color: #E57373; }");
            sb.AppendLine("    .footer { text-align: center; margin-top: 30px; font-size: 11px; color: #666; }");
            sb.AppendLine("  </style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("  <div class=\"container\">");
            sb.AppendLine("    <h1>BÁO CÁO NGHIỆM THU CHUYỂN ĐỔI: JNN ETABS → REVIT</h1>");
            string title = !string.IsNullOrWhiteSpace(docTitle) ? docTitle : "Dự án Revit";
            sb.AppendLine($"    <p><strong>Mô hình nguồn:</strong> {model.ModelName} ({model.SourceUnits}) | <strong>Dự án Revit:</strong> {title} | <strong>Ngày tạo:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");

            // KPI Grid
            sb.AppendLine("    <div class=\"kpi-grid\">");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\">{model.TotalElements}</div><div class=\"kpi-label\">Tổng cấu kiện nguồn</div></div>");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\" style=\"color:#81C784;\">+{result.ColumnsCreated + result.BeamsCreated + result.WallsCreated + result.SlabsCreated}</div><div class=\"kpi-label\">Tạo mới</div></div>");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\" style=\"color:#64B5F6;\">~{result.ColumnsUpdated + result.BeamsUpdated}</div><div class=\"kpi-label\">Cập nhật</div></div>");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\" style=\"color:#FFB74D;\">{validation.WarningCount}</div><div class=\"kpi-label\">Cảnh báo</div></div>");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\" style=\"color:#E57373;\">{result.ErrorsCount + validation.FailedCount}</div><div class=\"kpi-label\">Lỗi</div></div>");
            sb.AppendLine($"      <div class=\"kpi-card\"><div class=\"kpi-num\" style=\"color:#00FFCC;\">{validation.PassRate:F1}%</div><div class=\"kpi-label\">Độ khớp chuẩn</div></div>");
            sb.AppendLine("    </div>");

            // Element Statistics
            sb.AppendLine("    <h2>1. THỐNG KÊ SỐ LƯỢNG CẤU KIỆN</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Loại cấu kiện</th><th>Số lượng nguồn</th><th>Tạo mới</th><th>Cập nhật</th><th>Trạng thái</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            AppendStatRow(sb, "Cột kết cấu (Columns)", model.Columns.Count, result.ColumnsCreated, result.ColumnsUpdated);
            AppendStatRow(sb, "Dầm kết cấu (Beams)", model.Beams.Count, result.BeamsCreated, result.BeamsUpdated);
            AppendStatRow(sb, "Vách kết cấu (Walls)", model.Walls.Count, result.WallsCreated, 0);
            AppendStatRow(sb, "Sàn kết cấu (Slabs)", model.Slabs.Count, result.SlabsCreated, 0);
            AppendStatRow(sb, "Tầng (Levels)", model.Levels.Count, result.LevelsCreated, 0);
            AppendStatRow(sb, "Lưới trục (Grids)", model.Grids.Count, result.GridsCreated, 0);
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");

            // Validation Details
            sb.AppendLine("    <h2>2. KẾT QUẢ ĐỐI SOÁT HÌNH HỌC & DUNG SAI (±2mm)</h2>");
            sb.AppendLine("    <table>");
            sb.AppendLine("      <thead><tr><th>Hạng mục</th><th>Đối tượng</th><th>Giá trị nguồn</th><th>Giá trị Revit</th><th>Sai lệch</th><th>Đánh giá</th></tr></thead>");
            sb.AppendLine("      <tbody>");
            foreach (var item in validation.Items)
            {
                string badgeClass = item.Status switch
                {
                    ValidationStatus.Pass => "badge-pass",
                    ValidationStatus.Warning => "badge-warn",
                    _ => "badge-fail"
                };

                sb.AppendLine($"        <tr><td>{item.Category}</td><td><strong>{item.ElementId}</strong></td><td>{item.ExpectedValue}</td><td>{item.ActualValue}</td><td>{item.Difference:F1} mm</td><td><span class=\"badge {badgeClass}\">{item.Status.ToString().ToUpperInvariant()}</span> {item.Message}</td></tr>");
            }
            sb.AppendLine("      </tbody>");
            sb.AppendLine("    </table>");

            sb.AppendLine("    <div class=\"footer\">Báo cáo được khởi tạo tự động bởi JNN ETABS Converter — JNN Structural BIM Suite</div>");
            sb.AppendLine("  </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
            return outputPath;
        }

        private static void AppendStatRow(StringBuilder sb, string name, int sourceCount, int created, int updated)
        {
            string status = (created + updated >= sourceCount && sourceCount > 0)
                ? "<span class=\"badge badge-pass\">HOÀN TẤT</span>"
                : (sourceCount == 0 ? "<span class=\"badge\" style=\"background:#444;\">TRỐNG</span>" : "<span class=\"badge badge-warn\">MỘT PHẦN</span>");

            sb.AppendLine($"        <tr><td><strong>{name}</strong></td><td>{sourceCount}</td><td>+{created}</td><td>~{updated}</td><td>{status}</td></tr>");
        }

        public static string GenerateCsvReport(
            StructuralModel model,
            IReadOnlyDictionary<string, string>? elementIdMap,
            ConversionReportResult result,
            string outputPath)
        {
            var sb = new StringBuilder();
            // Header
            sb.AppendLine("Nguon,MaID_Nguon,LoaiCauKien,RevitElementId,TietDien,Tang,TrangThai,GhiChu");

            void AddElement(StructuralElement el)
            {
                string revitId = "-";
                if (elementIdMap != null && elementIdMap.TryGetValue(el.SourceId, out var id))
                {
                    revitId = id;
                }

                sb.AppendLine($"\"{el.Source}\",\"{el.SourceId}\",\"{el.ElementType}\",\"{revitId}\",\"{el.Section}\",\"{el.Story}\",\"{el.Status}\",\"\"");
            }

            foreach (var col in model.Columns) AddElement(col);
            foreach (var bm in model.Beams) AddElement(bm);
            foreach (var wl in model.Walls) AddElement(wl);
            foreach (var sbItem in model.Slabs) AddElement(sbItem);
            foreach (var lv in model.Levels) AddElement(lv);

            // Ghi file với UTF8 with BOM để mở Excel tiếng Việt không bị lỗi font
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(true));
            return outputPath;
        }
    }
}
