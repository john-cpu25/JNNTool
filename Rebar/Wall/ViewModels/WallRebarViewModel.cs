using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Core.Settings;
using JNNTool.RebarSuite.Wall.Builders;
using JNNTool.RebarSuite.Wall.Engine;
using JNNTool.RebarSuite.Wall.Models;

namespace JNNTool.RebarSuite.Wall.ViewModels
{
    public class WallRebarViewModel : ObservableObject
    {
        private WallRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép vách 2 lớp";

        public WallRebarSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> VerticalBarOptions { get; } = new ObservableCollection<string>
        {
            "D10", "D12", "D14", "D16", "D18", "D20"
        };

        public ObservableCollection<string> HorizontalBarOptions { get; } = new ObservableCollection<string>
        {
            "D8", "D10", "D12", "D14"
        };

        public ObservableCollection<string> BoundaryTieOptions { get; } = new ObservableCollection<string>
        {
            "D6", "D8", "D10"
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public WallRebarViewModel()
        {
            _settings = SettingsService<WallRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<WallRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var walls = new List<Autodesk.Revit.DB.Wall>();

                    foreach (var id in selectedIds)
                    {
                        var elem = doc.GetElement(id);
                        if (elem is Autodesk.Revit.DB.Wall wall && wall.WallType.Kind == WallKind.Basic)
                        {
                            walls.Add(wall);
                        }
                    }

                    if (walls.Count == 0)
                    {
                        StatusMessage = "Vui lòng chọn ít nhất một bức vách (Wall) trước khi tạo thép!";
                        TaskDialog.Show("JNN - Thép Vách", "Chưa có vách nào được chọn. Vui lòng chọn vách trong Revit trước.");
                        return;
                    }

                    int totalBarsCreated = 0;
                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép vách"))
                    {
                        tg.Start();

                        using (var tx = new Transaction(doc, "Tạo thép vách 2 lớp"))
                        {
                            tx.Start();

                            foreach (var wall in walls)
                            {
                                var wallModel = WallRebarBuilder.ExtractWallModel(doc, wall);
                                var layout = WallLayoutEngine.Calculate(wallModel, Settings);
                                int count = WallRebarBuilder.BuildWallRebar(doc, wall, layout);
                                totalBarsCreated += count;

                                WallSectionBuilder.CreateWallSection(doc, wallModel, Settings);
                            }

                            tx.Commit();
                        }

                        tg.Assimilate();
                    }

                    StatusMessage = $"Đã tạo thành công {totalBarsCreated} bộ cốt thép cho {walls.Count} vách!";
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép vách", ex);
                    StatusMessage = $"Lỗi: {ex.Message}";
                    TaskDialog.Show("JNN - Lỗi", $"Không thể tạo thép vách: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = new WallRebarSettings();
            SettingsService<WallRebarSettings>.Save(Settings);
            StatusMessage = "Đã khôi phục cài đặt mặc định!";
        }
    }
}
