using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using JNNTool.Tools.QuantityTakeoff.Models;

namespace JNNTool.Tools.QuantityTakeoff.Engine
{
    public static class ExcelExportEngine
    {
        /// <summary>
        /// Xuất dữ liệu khối lượng ra file Excel (.xls - XML Spreadsheet 2003) với 3 Sheet đầy đủ định dạng.
        /// </summary>
        public static void ExportToExcelXml(
            string filePath,
            string projectName,
            List<LevelSummaryItem> levelSummaries,
            List<CategorySummaryItem> categorySummaries,
            List<TakeoffElementItem> elementDetails)
        {
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\"?>");
            sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
            sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
            sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
            sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"");
            sb.AppendLine(" xmlns:html=\"http://www.w3.org/TR/REC-html40\">");

            // --- Styles ---
            sb.AppendLine(" <Styles>");
            // Default
            sb.AppendLine("  <Style ss:ID=\"Default\" ss:Name=\"Normal\">");
            sb.AppendLine("   <Alignment ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#2C3E50\"/>");
            sb.AppendLine("  </Style>");

            // Title
            sb.AppendLine("  <Style ss:ID=\"TitleStyle\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"14\" ss:Bold=\"1\" ss:Color=\"#1E293B\"/>");
            sb.AppendLine("  </Style>");

            // Subtitle
            sb.AppendLine("  <Style ss:ID=\"SubTitleStyle\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Left\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Italic=\"1\" ss:Color=\"#64748B\"/>");
            sb.AppendLine("  </Style>");

            // Header Row (Blue corporate)
            sb.AppendLine("  <Style ss:ID=\"HeaderStyle\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\" ss:WrapText=\"1\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#DCDDE1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#DCDDE1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#DCDDE1\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#DCDDE1\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Bold=\"1\" ss:Color=\"#FFFFFF\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#2E5C9A\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("  </Style>");

            // Data Cell (Text)
            sb.AppendLine("  <Style ss:ID=\"DataText\">");
            sb.AppendLine("   <Alignment ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#2C3E50\"/>");
            sb.AppendLine("  </Style>");

            // Data Cell (Center)
            sb.AppendLine("  <Style ss:ID=\"DataCenter\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Center\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#2C3E50\"/>");
            sb.AppendLine("  </Style>");

            // Data Cell (Number 2 Decimals: m3, m2, m)
            sb.AppendLine("  <Style ss:ID=\"DataNum2\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#2C3E50\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.00\"/>");
            sb.AppendLine("  </Style>");

            // Data Cell (Number 3 Decimals: Tons)
            sb.AppendLine("  <Style ss:ID=\"DataNum3\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Left\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Right\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#E2E8F0\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Color=\"#2C3E50\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.000\"/>");
            sb.AppendLine("  </Style>");

            // Total Summary Row
            sb.AppendLine("  <Style ss:ID=\"TotalStyle\">");
            sb.AppendLine("   <Alignment ss:Horizontal=\"Right\" ss:Vertical=\"Center\"/>");
            sb.AppendLine("   <Borders>");
            sb.AppendLine("    <Border ss:Position=\"Bottom\" ss:LineStyle=\"Double\" ss:Weight=\"3\" ss:Color=\"#2E5C9A\"/>");
            sb.AppendLine("    <Border ss:Position=\"Top\" ss:LineStyle=\"Continuous\" ss:Weight=\"1\" ss:Color=\"#2E5C9A\"/>");
            sb.AppendLine("   </Borders>");
            sb.AppendLine("   <Font ss:FontName=\"Segoe UI\" ss:Size=\"10\" ss:Bold=\"1\" ss:Color=\"#2E5C9A\"/>");
            sb.AppendLine("   <Interior ss:Color=\"#EDF2F7\" ss:Pattern=\"Solid\"/>");
            sb.AppendLine("   <NumberFormat ss:Format=\"#,##0.00\"/>");
            sb.AppendLine("  </Style>");
            sb.AppendLine(" </Styles>");

            // ==================== SHEET 1: BY LEVEL ====================
            sb.AppendLine(" <Worksheet ss:Name=\"By Level (Theo Tang)\">");
            sb.AppendLine("  <Table ss:DefaultRowHeight=\"20\">");
            sb.AppendLine("   <Column ss:Width=\"140\"/>"); // Tầng
            sb.AppendLine("   <Column ss:Width=\"90\"/>");  // Cao độ (m)
            sb.AppendLine("   <Column ss:Width=\"80\"/>");  // Số lượng
            sb.AppendLine("   <Column ss:Width=\"120\"/>"); // Bê tông (m3)
            sb.AppendLine("   <Column ss:Width=\"120\"/>"); // Cốp pha (m2)
            sb.AppendLine("   <Column ss:Width=\"120\"/>"); // Thép / Rebar (Tấn)
            sb.AppendLine("   <Column ss:Width=\"120\"/>"); // Tổng thể tích (m3)

            // Header info
            sb.AppendLine($"   <Row ss:Height=\"28\"><Cell ss:StyleID=\"TitleStyle\"><Data ss:Type=\"String\">JNN TOOL — BẢNG TỔNG HỢP KHỐI LƯỢNG THEO TẦNG</Data></Cell></Row>");
            sb.AppendLine($"   <Row ss:Height=\"20\"><Cell ss:StyleID=\"SubTitleStyle\"><Data ss:Type=\"String\">Dự án: {Escape(projectName)} | Xuất lúc: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</Data></Cell></Row>");
            sb.AppendLine("   <Row ss:Height=\"10\"/>");

            // Table Header
            sb.AppendLine("   <Row ss:Height=\"24\">");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Tầng (Level)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Cao Độ (m)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Số Lượng</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Bê Tông (m³)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Cốp Pha (m²)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Thép / Rebar (Tấn)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Tổng Thể Tích (m³)</Data></Cell>");
            sb.AppendLine("   </Row>");

            double sumLvlConcrete = 0, sumLvlFormwork = 0, sumLvlSteel = 0, sumLvlVol = 0;
            int sumLvlCount = 0;

            foreach (var l in levelSummaries)
            {
                sumLvlCount += l.ElementCount;
                sumLvlConcrete += l.ConcreteVolume;
                sumLvlFormwork += l.FormworkArea;
                sumLvlSteel += l.SteelRebarWeight;
                sumLvlVol += l.TotalVolume;

                sb.AppendLine("   <Row ss:Height=\"20\">");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(l.LevelName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{l.Elevation.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"Number\">{l.ElementCount}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{l.ConcreteVolume.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{l.FormworkArea.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum3\"><Data ss:Type=\"Number\">{l.SteelRebarWeight.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{l.TotalVolume.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine("   </Row>");
            }

            // Total row
            sb.AppendLine("   <Row ss:Height=\"22\">");
            sb.AppendLine("    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"String\">TỔNG CỘNG</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"String\"></Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumLvlCount}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumLvlConcrete.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumLvlFormwork.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumLvlSteel.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumLvlVol.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine("   </Row>");

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");

            // ==================== SHEET 2: BY CATEGORY ====================
            sb.AppendLine(" <Worksheet ss:Name=\"By Category (Theo Cau Kien)\">");
            sb.AppendLine("  <Table ss:DefaultRowHeight=\"20\">");
            sb.AppendLine("   <Column ss:Width=\"180\"/>"); // Category
            sb.AppendLine("   <Column ss:Width=\"80\"/>");  // Số lượng
            sb.AppendLine("   <Column ss:Width=\"130\"/>"); // Thể tích (m3)
            sb.AppendLine("   <Column ss:Width=\"130\"/>"); // Diện tích (m2)
            sb.AppendLine("   <Column ss:Width=\"130\"/>"); // Chiều dài (m)
            sb.AppendLine("   <Column ss:Width=\"130\"/>"); // Khối lượng (Tấn)

            sb.AppendLine($"   <Row ss:Height=\"28\"><Cell ss:StyleID=\"TitleStyle\"><Data ss:Type=\"String\">JNN TOOL — BẢNG TỔNG HỢP THEO LOẠI CẤU KIỆN</Data></Cell></Row>");
            sb.AppendLine($"   <Row ss:Height=\"20\"><Cell ss:StyleID=\"SubTitleStyle\"><Data ss:Type=\"String\">Dự án: {Escape(projectName)} | Xuất lúc: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</Data></Cell></Row>");
            sb.AppendLine("   <Row ss:Height=\"10\"/>");

            sb.AppendLine("   <Row ss:Height=\"24\">");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Loại Cấu Kiện (Category)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Số Lượng</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Thể Tích (m³)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Diện Tích (m²)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Chiều Dài (m)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Trọng Lượng (Tấn)</Data></Cell>");
            sb.AppendLine("   </Row>");

            double sumCatVol = 0, sumCatArea = 0, sumCatLen = 0, sumCatWeight = 0;
            int sumCatCount = 0;

            foreach (var c in categorySummaries)
            {
                sumCatCount += c.Count;
                sumCatVol += c.TotalVolume;
                sumCatArea += c.TotalArea;
                sumCatLen += c.TotalLength;
                sumCatWeight += c.TotalWeight;

                sb.AppendLine("   <Row ss:Height=\"20\">");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(c.CategoryName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"Number\">{c.Count}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{c.TotalVolume.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{c.TotalArea.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{c.TotalLength.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum3\"><Data ss:Type=\"Number\">{c.TotalWeight.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine("   </Row>");
            }

            // Total row
            sb.AppendLine("   <Row ss:Height=\"22\">");
            sb.AppendLine("    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"String\">TỔNG CỘNG</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumCatCount}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumCatVol.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumCatArea.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumCatLen.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine($"    <Cell ss:StyleID=\"TotalStyle\"><Data ss:Type=\"Number\">{sumCatWeight.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
            sb.AppendLine("   </Row>");

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");

            // ==================== SHEET 3: ELEMENT DETAILS ====================
            sb.AppendLine(" <Worksheet ss:Name=\"Element Details (Chi Tiet)\">");
            sb.AppendLine("  <Table ss:DefaultRowHeight=\"20\">");
            sb.AppendLine("   <Column ss:Width=\"70\"/>");  // Id
            sb.AppendLine("   <Column ss:Width=\"130\"/>"); // Category
            sb.AppendLine("   <Column ss:Width=\"110\"/>"); // Level
            sb.AppendLine("   <Column ss:Width=\"140\"/>"); // Family
            sb.AppendLine("   <Column ss:Width=\"140\"/>"); // Type
            sb.AppendLine("   <Column ss:Width=\"80\"/>");  // Mark
            sb.AppendLine("   <Column ss:Width=\"120\"/>"); // Material
            sb.AppendLine("   <Column ss:Width=\"90\"/>");  // Volume (m3)
            sb.AppendLine("   <Column ss:Width=\"90\"/>");  // Area (m2)
            sb.AppendLine("   <Column ss:Width=\"90\"/>");  // Length (m)
            sb.AppendLine("   <Column ss:Width=\"90\"/>");  // Weight (Tons)

            sb.AppendLine("   <Row ss:Height=\"24\">");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">ID</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Category</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Tầng (Level)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Family</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Type Name</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Mark</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Vật Liệu</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Thể Tích (m³)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Diện Tích (m²)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Chiều Dài (m)</Data></Cell>");
            sb.AppendLine("    <Cell ss:StyleID=\"HeaderStyle\"><Data ss:Type=\"String\">Trọng Lượng (Tấn)</Data></Cell>");
            sb.AppendLine("   </Row>");

            foreach (var e in elementDetails)
            {
                sb.AppendLine("   <Row ss:Height=\"20\">");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"Number\">{e.IdValue}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(e.CategoryName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(e.LevelName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(e.FamilyName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(e.TypeName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataCenter\"><Data ss:Type=\"String\">{Escape(e.Mark)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataText\"><Data ss:Type=\"String\">{Escape(e.MaterialName)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{e.Volume.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{e.Area.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum2\"><Data ss:Type=\"Number\">{e.Length.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine($"    <Cell ss:StyleID=\"DataNum3\"><Data ss:Type=\"Number\">{e.Weight.ToString(CultureInfo.InvariantCulture)}</Data></Cell>");
                sb.AppendLine("   </Row>");
            }

            sb.AppendLine("  </Table>");
            sb.AppendLine(" </Worksheet>");

            sb.AppendLine("</Workbook>");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
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

        private static string Escape(string? val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            return SecurityElement.Escape(val);
        }
    }
}
