using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.Compat;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.RebarSuite.Common;
using JNNTool.RebarSuite.Pile.Builders;
using JNNTool.RebarSuite.Pile.Engine;
using JNNTool.RebarSuite.Pile.Models;

namespace JNNTool.RebarSuite.Pile.ViewModels
{
    public class PileRebarViewModel : ObservableObject
    {
        private PileRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép cọc và xuất tọa độ";

        public PileRebarSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> MainBarOptions { get; } = new ObservableCollection<string>
        {
            "D16", "D18", "D20", "D22", "D25", "D28"
        };

        public ObservableCollection<string> StirrupOptions { get; } = new ObservableCollection<string>
        {
            "D8", "D10", "D12"
        };

        public ObservableCollection<string> StiffenerOptions { get; } = new ObservableCollection<string>
        {
            "D12", "D14", "D16"
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ExportCoordinatesCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public PileRebarViewModel()
        {
            _settings = SettingsService<PileRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ExportCoordinatesCommand = new RelayCommand(OnExportCoordinates);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<PileRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var piles = new List<FamilyInstance>();

                    foreach (var id in selectedIds)
                    {
                        var elem = doc.GetElement(id);
                        if (elem is FamilyInstance fi && fi.Category != null &&
                            fi.Category.Id.GetIdValue() == (long)BuiltInCategory.OST_StructuralFoundation)
                        {
                            piles.Add(fi);
                        }
                    }

                    if (piles.Count == 0)
                    {
                        StatusMessage = "Vui lòng chọn ít nhất một cọc trong Revit trước khi tạo thép!";
                        TaskDialog.Show("JNN - Thép Cọc", "Chưa có cọc nào được chọn.");
                        return;
                    }

                    int totalBarsCreated = 0;
                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép cọc"))
                    {
                        tg.Start();

                        using (var tx = new Transaction(doc, "Tạo thép lồng cọc"))
                        {
                            tx.Start();

                            foreach (var pile in piles)
                            {
                                var model = PileRebarBuilder.ExtractPileModel(doc, pile);
                                model.Type = Settings.Type;
                                var layout = PileLayoutEngine.Calculate(model, Settings);
                                int count = PileRebarBuilder.BuildPileRebar(doc, pile, layout);
                                totalBarsCreated += count;
                            }

                            tx.Commit();
                        }

                        tg.Assimilate();
                    }

                    StatusMessage = $"Đã tạo thành công {totalBarsCreated} bộ cốt thép cho {piles.Count} cọc!";
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép cọc", ex);
                    StatusMessage = $"Lỗi: {ex.Message}";
                    TaskDialog.Show("JNN - Lỗi", $"Không thể tạo thép cọc: {ex.Message}");
                }
            });
        }

        private void OnExportCoordinates()
        {
            StatusMessage = "Đang xuất tọa độ cọc...";

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var piles = new List<FamilyInstance>();

                    foreach (var id in selectedIds)
                    {
                        var elem = doc.GetElement(id);
                        if (elem is FamilyInstance fi && fi.Category != null &&
                            fi.Category.Id.GetIdValue() == (long)BuiltInCategory.OST_StructuralFoundation)
                        {
                            piles.Add(fi);
                        }
                    }

                    if (piles.Count == 0)
                    {
                        // Quét tất cả cọc trong dự án nếu không chọn trước
                        var collector = new FilteredElementCollector(doc)
                            .OfCategory(BuiltInCategory.OST_StructuralFoundation)
                            .WhereElementIsNotElementType()
                            .OfClass(typeof(FamilyInstance))
                            .Cast<FamilyInstance>()
                            .ToList();
                        piles.AddRange(collector);
                    }

                    if (piles.Count == 0)
                    {
                        TaskDialog.Show("JNN - Xuất Tọa Độ", "Không tìm thấy cấu kiện cọc nào trong dự án.");
                        return;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine("STT,TenCoc,ToaDoX_mm,ToaDoY_mm,CaoDoDinh_mm,ChieuDai_mm");

                    int idx = 1;
                    foreach (var p in piles)
                    {
                        var model = PileRebarBuilder.ExtractPileModel(doc, p);
                        sb.AppendLine($"{idx++},{model.PileName},{model.CenterPoint.X:F1},{model.CenterPoint.Y:F1},{model.TopElevationMm:F1},{model.LengthMm:F1}");
                    }

                    string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string filePath = Path.Combine(desktopPath, $"JNN_ToaDoCoc_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
                    File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                    StatusMessage = $"Đã xuất tọa độ {piles.Count} cọc ra Desktop!";
                    TaskDialog.Show("JNN - Xuất Tọa Độ Thành Công", $"Đã lưu file CSV chứa tọa độ {piles.Count} cọc tại:\n{filePath}");
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi xuất tọa độ cọc", ex);
                    StatusMessage = $"Lỗi: {ex.Message}";
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = new PileRebarSettings();
            SettingsService<PileRebarSettings>.Save(Settings);
            StatusMessage = "Đã khôi phục cài đặt mặc định!";
        }
    }
}
