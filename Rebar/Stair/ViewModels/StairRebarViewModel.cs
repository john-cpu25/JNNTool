using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.RebarSuite.Stair.Builders;
using JNNTool.RebarSuite.Stair.Engine;
using JNNTool.RebarSuite.Stair.Models;

namespace JNNTool.RebarSuite.Stair.ViewModels
{
    public class StairRebarViewModel : ObservableObject
    {
        private StairRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép bản thang";

        public StairRebarSettings Settings
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
            "D8", "D10", "D12", "D14", "D16"
        };

        public ObservableCollection<string> DistBarOptions { get; } = new ObservableCollection<string>
        {
            "D6", "D8", "D10"
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public StairRebarViewModel()
        {
            _settings = SettingsService<StairRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<StairRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var stairsList = new List<Autodesk.Revit.DB.Architecture.Stairs>();

                    foreach (var id in selectedIds)
                    {
                        var elem = doc.GetElement(id);
                        if (elem is Autodesk.Revit.DB.Architecture.Stairs st)
                        {
                            stairsList.Add(st);
                        }
                    }

                    if (stairsList.Count == 0)
                    {
                        StatusMessage = "Vui lòng chọn ít nhất một cầu thang (Stairs) trước khi tạo thép!";
                        TaskDialog.Show("JNN - Thép Thang", "Chưa có cầu thang nào được chọn.");
                        return;
                    }

                    int totalBarsCreated = 0;
                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép thang"))
                    {
                        tg.Start();

                        using (var tx = new Transaction(doc, "Tạo cốt thép bản thang"))
                        {
                            tx.Start();

                            foreach (var stair in stairsList)
                            {
                                var model = StairRebarBuilder.ExtractStairModel(doc, stair);
                                var layout = StairLayoutEngine.Calculate(model, Settings);
                                int count = StairRebarBuilder.BuildStairRebar(doc, stair, layout);
                                totalBarsCreated += count;
                            }

                            tx.Commit();
                        }

                        tg.Assimilate();
                    }

                    StatusMessage = $"Đã tạo thành công {totalBarsCreated} bộ cốt thép cho {stairsList.Count} cầu thang!";
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép thang", ex);
                    StatusMessage = $"Lỗi: {ex.Message}";
                    TaskDialog.Show("JNN - Lỗi", $"Không thể tạo thép thang: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = new StairRebarSettings();
            SettingsService<StairRebarSettings>.Save(Settings);
            StatusMessage = "Đã khôi phục cài đặt mặc định!";
        }
    }
}
