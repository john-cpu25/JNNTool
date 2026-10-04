using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Utilities.Engine;

namespace JNNTool.RebarSuite.Utilities.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RebarVolumeCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uidoc = commandData.Application.ActiveUIDocument;
            if (uidoc == null) return Result.Cancelled;
            var doc = uidoc.Document;

            try
            {
                var rebars = new FilteredElementCollector(doc)
                    .OfClass(typeof(Autodesk.Revit.DB.Structure.Rebar))
                    .WhereElementIsNotElementType()
                    .Cast<Autodesk.Revit.DB.Structure.Rebar>()
                    .ToList();

                if (rebars.Count == 0)
                {
                    TaskDialog.Show("JNN - Khối Lượng Thép", "Không tìm thấy cốt thép nào trong dự án.");
                    return Result.Succeeded;
                }

                var rawInputs = new List<RawRebarInput>();

                foreach (var rb in rebars)
                {
                    try
                    {
                        var barType = doc.GetElement(rb.GetTypeId()) as RebarBarType;
                        string diaStr = barType?.Name ?? "D10";

                        int qty = rb.Quantity;
                        if (qty <= 0) qty = 1;

                        double lenFeet = rb.TotalLength / qty;
                        double lenMm = GeometryHelper.FeetToMm(lenFeet);

                        rawInputs.Add(new RawRebarInput
                        {
                            Diameter = diaStr,
                            Quantity = qty,
                            LengthMm = lenMm
                        });
                    }
                    catch { }
                }

                var summary = RebarVolumeCalculator.CalculateByDiameter(rawInputs);
                double totalTon = RebarVolumeCalculator.GetTotalWeightTon(summary);

                var sb = new StringBuilder();
                sb.AppendLine("=== BẢNG THỐNG KÊ KHỐI LƯỢNG THÉP (TCVN 5574:2018) ===");
                sb.AppendLine($"Tổng số lượng thanh thép: {rawInputs.Sum(r => r.Quantity):N0} thanh");
                sb.AppendLine($"Tổng khối lượng: {totalTon:F3} Tấn\n");
                sb.AppendLine("ĐK\tSố Thanh\tTổng Dài (m)\tKhối Lượng (kg)");

                foreach (var item in summary)
                {
                    sb.AppendLine($"{item.Diameter}\t{item.TotalBars}\t{item.TotalLengthM:F1}\t{item.TotalWeightKg:F1}");
                }

                // Xuất file CSV ra Desktop
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string csvPath = Path.Combine(desktop, $"JNN_ThongKeThep_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                var csvSb = new StringBuilder();
                csvSb.AppendLine("DuongKinh,SoThanh,TongChieuDai_m,KhoiLuong_kg,KhoiLuong_Tan");
                foreach (var item in summary)
                {
                    csvSb.AppendLine($"{item.Diameter},{item.TotalBars},{item.TotalLengthM:F2},{item.TotalWeightKg:F2},{item.TotalWeightTon:F3}");
                }
                csvSb.AppendLine($"TONG_CONG,{summary.Sum(i => i.TotalBars)},{summary.Sum(i => i.TotalLengthM):F2},{summary.Sum(i => i.TotalWeightKg):F2},{totalTon:F3}");

                File.WriteAllText(csvPath, csvSb.ToString(), Encoding.UTF8);

                sb.AppendLine($"\nĐã xuất file CSV chi tiết tại:\n{csvPath}");
                TaskDialog.Show("JNN - Thống Kê Khối Lượng Thép", sb.ToString());

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi thống kê khối lượng thép", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
