using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
using JNNTool.RebarSuite.Slab.Builders;
using JNNTool.RebarSuite.Slab.Engine;
using JNNTool.RebarSuite.Slab.Models;

namespace JNNTool.RebarSuite.Slab.ViewModels
{
    public class SlabRebarViewModel : ObservableObject
    {
        private SlabRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép sàn";

        public SlabRebarSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> BarOptions { get; } = new ObservableCollection<string>
        {
            "D6", "D8", "D10", "D12", "D14", "D16", "D18", "D20", "D22", "D25"
        };

        public ObservableCollection<HookAngle> HookOptions { get; } = new ObservableCollection<HookAngle>
        {
            HookAngle.None, HookAngle.Deg90, HookAngle.Deg135, HookAngle.Deg180
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public SlabRebarViewModel()
        {
            _settings = SettingsService<SlabRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";

            // Lưu cài đặt hiện tại
            SettingsService<SlabRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    // Lấy danh sách sàn được chọn từ Revit Selection
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var floors = new List<Floor>();

                    foreach (var id in selectedIds)
                    {
                        if (doc.GetElement(id) is Floor floor)
                        {
                            floors.Add(floor);
                        }
                    }

                    // Nếu chưa chọn sàn nào, nhắc người dùng chọn
                    if (floors.Count == 0)
                    {
                        TaskDialog.Show("JNN Rebar", "Vui lòng chọn ít nhất một Sàn kết cấu (Floor) trong mô hình!");
                        return;
                    }

                    int totalCreated = 0;

                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép sàn"))
                    {
                        tg.Start();

                        foreach (var floor in floors)
                        {
                            using (var t = new Transaction(doc, $"Tạo thép sàn {floor.Id.GetIdValue()}"))
                            {
                                t.Start();

                                var slabModel = SlabRebarBuilder.ExtractSlabModel(floor);
                                var layoutResult = SlabLayoutEngine.Calculate(slabModel, Settings);
                                int count = SlabRebarBuilder.BuildRebar(doc, floor, layoutResult);

                                t.Commit();
                                totalCreated += count;
                            }
                        }

                        tg.Assimilate();
                    }

                    StatusMessage = $"Hoàn thành: Đã tạo {totalCreated} bộ thép cho {floors.Count} sàn.";
                    TaskDialog.Show("JNN Rebar", $"Đã bố trí thành công {totalCreated} bộ thép cho {floors.Count} sàn!");
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép sàn", ex);
                    TaskDialog.Show("Lỗi", $"Không thể tạo thép sàn: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = SettingsService<SlabRebarSettings>.Reset();
            StatusMessage = "Đã đặt lại thông số mặc định.";
        }
    }
}
