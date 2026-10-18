using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ClosedXML.Excel;
using JNNTool.Tools.QuantityTakeoff.Models;

namespace JNNTool.Tools.QuantityTakeoff.Engine
{
    public static class ExcelExportEngine
    {
        private static readonly XLColor HeaderBgColor = XLColor.FromHtml("#2E5C9A");
        private static readonly XLColor TotalRowBgColor = XLColor.FromHtml("#EDF2F7");
        private static readonly XLColor TotalTextColor = XLColor.FromHtml("#2E5C9A");
        private static readonly XLColor BorderGray = XLColor.FromHtml("#CBD5E1");

        /// <summary>
        /// Xuất dữ liệu khối lượng ra file Excel (.xlsx) chuẩn OpenXML bằng ClosedXML với 3 Sheet định dạng kỹ thuật.
        /// </summary>
        public static void ExportToExcel(
            string filePath,
            string projectName,
            List<LevelSummaryItem> levelSummaries,
            List<CategorySummaryItem> categorySummaries,
            List<TakeoffElementItem> elementDetails)
        {
            using (var wb = new XLWorkbook())
            {
                // ==================== SHEET 1: BY LEVEL ====================
                var wsLevel = wb.Worksheets.Add("Theo Tang (By Level)");
                wsLevel.ShowGridLines = true;

                // Title & Subtitle
                wsLevel.Cell(1, 1).Value = "JNNTOOL — BẢNG TỔNG HỢP KHỐI LƯỢNG THEO TẦNG";
                wsLevel.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.FromHtml("#1E293B"));

                wsLevel.Cell(2, 1).Value = $"Dự án: {projectName}  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                wsLevel.Cell(2, 1).Style.Font.SetItalic().Font.SetFontSize(10).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.FromHtml("#64748B"));

                // Headers (Row 4)
                string[] levelHeaders = { "Tầng (Level)", "Cao Độ (m)", "Số Lượng", "Bê Tông (m³)", "Cốp Pha (m²)", "Thép / Rebar (Tấn)", "Tổng Thể Tích (m³)" };
                for (int i = 0; i < levelHeaders.Length; i++)
                {
                    var cell = wsLevel.Cell(4, i + 1);
                    cell.Value = levelHeaders[i];
                    StyleHeader(cell);
                }

                int row = 5;
                foreach (var l in levelSummaries)
                {
                    wsLevel.Cell(row, 1).SetValue(l.LevelName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsLevel.Cell(row, 2).SetValue(l.Elevation).Style.NumberFormat.SetFormat("#,##0.00");
                    wsLevel.Cell(row, 3).SetValue(l.ElementCount).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsLevel.Cell(row, 4).SetValue(l.ConcreteVolume).Style.NumberFormat.SetFormat("#,##0.00");
                    wsLevel.Cell(row, 5).SetValue(l.FormworkArea).Style.NumberFormat.SetFormat("#,##0.00");
                    wsLevel.Cell(row, 6).SetValue(l.SteelRebarWeight).Style.NumberFormat.SetFormat("#,##0.000");
                    wsLevel.Cell(row, 7).SetValue(l.TotalVolume).Style.NumberFormat.SetFormat("#,##0.00");

                    for (int c = 1; c <= 7; c++)
                    {
                        StyleDataCell(wsLevel.Cell(row, c));
                    }
                    row++;
                }

                // Total Row
                wsLevel.Cell(row, 1).SetValue("TỔNG CỘNG");
                wsLevel.Cell(row, 2).SetValue("-").Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                if (levelSummaries.Count > 0)
                {
                    wsLevel.Cell(row, 3).FormulaA1 = $"SUM(C5:C{row - 1})";
                    wsLevel.Cell(row, 4).FormulaA1 = $"SUM(D5:D{row - 1})";
                    wsLevel.Cell(row, 5).FormulaA1 = $"SUM(E5:E{row - 1})";
                    wsLevel.Cell(row, 6).FormulaA1 = $"SUM(F5:F{row - 1})";
                    wsLevel.Cell(row, 7).FormulaA1 = $"SUM(G5:G{row - 1})";
                }
                wsLevel.Cell(row, 3).Style.NumberFormat.SetFormat("#,##0");
                wsLevel.Cell(row, 4).Style.NumberFormat.SetFormat("#,##0.00");
                wsLevel.Cell(row, 5).Style.NumberFormat.SetFormat("#,##0.00");
                wsLevel.Cell(row, 6).Style.NumberFormat.SetFormat("#,##0.000");
                wsLevel.Cell(row, 7).Style.NumberFormat.SetFormat("#,##0.00");

                for (int c = 1; c <= 7; c++)
                {
                    StyleTotalCell(wsLevel.Cell(row, c));
                }

                wsLevel.Columns().AdjustToContents(10.0, 35.0);

                // ==================== SHEET 2: BY CATEGORY ====================
                var wsCat = wb.Worksheets.Add("Theo Cau Kien (By Category)");
                wsCat.ShowGridLines = true;

                wsCat.Cell(1, 1).Value = "JNNTOOL — BẢNG TỔNG HỢP THEO LOẠI CẤU KIỆN";
                wsCat.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(14).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.FromHtml("#1E293B"));

                wsCat.Cell(2, 1).Value = $"Dự án: {projectName}  |  Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}";
                wsCat.Cell(2, 1).Style.Font.SetItalic().Font.SetFontSize(10).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.FromHtml("#64748B"));

                string[] catHeaders = { "Loại Cấu Kiện (Category)", "Số Lượng", "Thể Tích (m³)", "Diện Tích (m²)", "Chiều Dài (m)", "Trọng Lượng (Tấn)" };
                for (int i = 0; i < catHeaders.Length; i++)
                {
                    var cell = wsCat.Cell(4, i + 1);
                    cell.Value = catHeaders[i];
                    StyleHeader(cell);
                }

                row = 5;
                foreach (var c in categorySummaries)
                {
                    wsCat.Cell(row, 1).SetValue(c.CategoryName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsCat.Cell(row, 2).SetValue(c.Count).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsCat.Cell(row, 3).SetValue(c.TotalVolume).Style.NumberFormat.SetFormat("#,##0.00");
                    wsCat.Cell(row, 4).SetValue(c.TotalArea).Style.NumberFormat.SetFormat("#,##0.00");
                    wsCat.Cell(row, 5).SetValue(c.TotalLength).Style.NumberFormat.SetFormat("#,##0.00");
                    wsCat.Cell(row, 6).SetValue(c.TotalWeight).Style.NumberFormat.SetFormat("#,##0.000");

                    for (int col = 1; col <= 6; col++)
                    {
                        StyleDataCell(wsCat.Cell(row, col));
                    }
                    row++;
                }

                // Total Row
                wsCat.Cell(row, 1).SetValue("TỔNG CỘNG");
                if (categorySummaries.Count > 0)
                {
                    wsCat.Cell(row, 2).FormulaA1 = $"SUM(B5:B{row - 1})";
                    wsCat.Cell(row, 3).FormulaA1 = $"SUM(C5:C{row - 1})";
                    wsCat.Cell(row, 4).FormulaA1 = $"SUM(D5:D{row - 1})";
                    wsCat.Cell(row, 5).FormulaA1 = $"SUM(E5:E{row - 1})";
                    wsCat.Cell(row, 6).FormulaA1 = $"SUM(F5:F{row - 1})";
                }
                wsCat.Cell(row, 2).Style.NumberFormat.SetFormat("#,##0");
                wsCat.Cell(row, 3).Style.NumberFormat.SetFormat("#,##0.00");
                wsCat.Cell(row, 4).Style.NumberFormat.SetFormat("#,##0.00");
                wsCat.Cell(row, 5).Style.NumberFormat.SetFormat("#,##0.00");
                wsCat.Cell(row, 6).Style.NumberFormat.SetFormat("#,##0.000");

                for (int col = 1; col <= 6; col++)
                {
                    StyleTotalCell(wsCat.Cell(row, col));
                }

                wsCat.Columns().AdjustToContents(10.0, 35.0);

                // ==================== SHEET 3: ELEMENT DETAILS ====================
                var wsDet = wb.Worksheets.Add("Chi Tiet (Element Details)");
                wsDet.ShowGridLines = true;

                string[] detHeaders = { "ID", "Category", "Tầng (Level)", "Family", "Type Name", "Mark", "Vật Liệu", "Thể Tích (m³)", "Diện Tích (m²)", "Chiều Dài (m)", "Trọng Lượng (Tấn)" };
                for (int i = 0; i < detHeaders.Length; i++)
                {
                    var cell = wsDet.Cell(1, i + 1);
                    cell.Value = detHeaders[i];
                    StyleHeader(cell);
                }

                row = 2;
                foreach (var e in elementDetails)
                {
                    wsDet.Cell(row, 1).SetValue(e.IdValue).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsDet.Cell(row, 2).SetValue(e.CategoryName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsDet.Cell(row, 3).SetValue(e.LevelName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsDet.Cell(row, 4).SetValue(e.FamilyName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsDet.Cell(row, 5).SetValue(e.TypeName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsDet.Cell(row, 6).SetValue(e.Mark).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                    wsDet.Cell(row, 7).SetValue(e.MaterialName).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
                    wsDet.Cell(row, 8).SetValue(e.Volume).Style.NumberFormat.SetFormat("#,##0.00");
                    wsDet.Cell(row, 9).SetValue(e.Area).Style.NumberFormat.SetFormat("#,##0.00");
                    wsDet.Cell(row, 10).SetValue(e.Length).Style.NumberFormat.SetFormat("#,##0.00");
                    wsDet.Cell(row, 11).SetValue(e.Weight).Style.NumberFormat.SetFormat("#,##0.000");

                    for (int col = 1; col <= 11; col++)
                    {
                        StyleDataCell(wsDet.Cell(row, col));
                    }
                    row++;
                }

                wsDet.Columns().AdjustToContents(8.0, 30.0);

                // Lưu file
                wb.SaveAs(filePath);
            }
        }

        /// <summary>
        /// Xuất dữ liệu khối lượng ra định dạng CSV (UTF-8 BOM).
        /// </summary>
        public static void ExportToCsv(string filePath, List<TakeoffElementItem> elementDetails)
        {
            var sb = new StringBuilder();
            sb.AppendLine("ID,Category,Level,Family,Type,Mark,Material,Volume_m3,Area_m2,Length_m,Weight_Tons");

            foreach (var e in elementDetails)
            {
                sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0},\"{1}\",\"{2}\",\"{3}\",\"{4}\",\"{5}\",\"{6}\",{7:F2},{8:F2},{9:F2},{10:F3}",
                    e.IdValue,
                    e.CategoryName.Replace("\"", "\"\""),
                    e.LevelName.Replace("\"", "\"\""),
                    e.FamilyName.Replace("\"", "\"\""),
                    e.TypeName.Replace("\"", "\"\""),
                    e.Mark.Replace("\"", "\"\""),
                    e.MaterialName.Replace("\"", "\"\""),
                    e.Volume,
                    e.Area,
                    e.Length,
                    e.Weight));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private static void StyleHeader(IXLCell cell)
        {
            cell.Style.Font.SetBold().Font.SetFontSize(10).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.White);
            cell.Style.Fill.SetBackgroundColor(HeaderBgColor);
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(BorderGray);
        }

        private static void StyleDataCell(IXLCell cell)
        {
            cell.Style.Font.SetFontSize(10).Font.SetFontName("Segoe UI").Font.SetFontColor(XLColor.FromHtml("#2C3E50"));
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin).Border.SetOutsideBorderColor(BorderGray);
        }

        private static void StyleTotalCell(IXLCell cell)
        {
            cell.Style.Font.SetBold().Font.SetFontSize(10).Font.SetFontName("Segoe UI").Font.SetFontColor(TotalTextColor);
            cell.Style.Fill.SetBackgroundColor(TotalRowBgColor);
            cell.Style.Alignment.SetVertical(XLAlignmentVerticalValues.Center);
            cell.Style.Border.SetTopBorder(XLBorderStyleValues.Thin).Border.SetTopBorderColor(HeaderBgColor);
            cell.Style.Border.SetBottomBorder(XLBorderStyleValues.Double).Border.SetBottomBorderColor(HeaderBgColor);
            cell.Style.Border.SetLeftBorder(XLBorderStyleValues.None);
            cell.Style.Border.SetRightBorder(XLBorderStyleValues.None);
        }
    }
}
