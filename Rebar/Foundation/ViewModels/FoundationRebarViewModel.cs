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
using JNNTool.RebarSuite.Foundation.Builders;
using JNNTool.RebarSuite.Foundation.Engine;
using JNNTool.RebarSuite.Foundation.Models;

namespace JNNTool.RebarSuite.Foundation.ViewModels
{
    public class FoundationRebarViewModel : ObservableObject
    {
        private FoundationRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép móng / đài cọc";

        public FoundationRebarSettings Settings
        {
            get => _settings;
            set => SetProperty(ref _settings, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ObservableCollection<string> BottomBarOptions { get; } = new ObservableCollection<string>
        {
            "D12", "D14", "D16", "D18", "D20", "D22", "D25"
        };

        public ObservableCollection<string> TopBarOptions { get; } = new ObservableCollection<string>
        {
            "D10", "D12", "D14", "D16", "D18"
        };

        public ObservableCollection<string> DowelBarOptions { get; } = new ObservableCollection<string>
        {
            "D16", "D18", "D20", "D22", "D25", "D28"
        };

        public ObservableCollection<string> DowelTieOptions { get; } = new ObservableCollection<string>
        {
            "D6", "D8", "D10"
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public FoundationRebarViewModel()
        {
            _settings = SettingsService<FoundationRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<FoundationRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var footings = new List<FamilyInstance>();

                    foreach (var id in selectedIds)
                    {
                        var elem = doc.GetElement(id);
                        if (elem is FamilyInstance fi && fi.Category != null &&
                            fi.Category.Id.GetIdValue() == (long)BuiltInCategory.OST_StructuralFoundation)
                        {
                            footings.Add(fi);
                        }
                    }

                    if (footings.Count == 0)
                    {
                        StatusMessage = "Vui lòng chọn ít nhất một móng/đài cọc (Structural Foundation) trước khi tạo thép!";
                        TaskDialog.Show("JNN - Thép Móng", "Chưa có móng nào được chọn. Vui lòng chọn móng trong Revit trước.");
                        return;
                    }

                    int totalBarsCreated = 0;
                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép móng"))
                    {
                        tg.Start();

                        using (var tx = new Transaction(doc, "Tạo thép móng / đài cọc"))
                        {
                            tx.Start();

                            foreach (var footing in footings)
                            {
                                var model = FoundationRebarBuilder.ExtractFoundationModel(doc, footing);
                                var layout = FoundationLayoutEngine.Calculate(model, Settings);
                                int count = FoundationRebarBuilder.BuildFoundationRebar(doc, footing, layout);
                                totalBarsCreated += count;
                            }

                            tx.Commit();
                        }

                        tg.Assimilate();
                    }

                    StatusMessage = $"Đã tạo thành công {totalBarsCreated} bộ cốt thép cho {footings.Count} móng!";
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép móng", ex);
                    StatusMessage = $"Lỗi: {ex.Message}";
                    TaskDialog.Show("JNN - Lỗi", $"Không thể tạo thép móng: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = new FoundationRebarSettings();
            SettingsService<FoundationRebarSettings>.Save(Settings);
            StatusMessage = "Đã khôi phục cài đặt mặc định!";
        }
    }
}
