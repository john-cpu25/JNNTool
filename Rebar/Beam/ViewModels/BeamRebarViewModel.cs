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
using JNNTool.RebarSuite.Beam.Builders;
using JNNTool.RebarSuite.Beam.Engine;
using JNNTool.RebarSuite.Beam.Models;

namespace JNNTool.RebarSuite.Beam.ViewModels
{
    public class BeamRebarViewModel : ObservableObject
    {
        private BeamRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép dầm liên tục";

        public BeamRebarSettings Settings
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
            "D6", "D8", "D10", "D12", "D14", "D16", "D18", "D20", "D22", "D25", "D28", "D32"
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public BeamRebarViewModel()
        {
            _settings = SettingsService<BeamRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<BeamRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var beams = new List<FamilyInstance>();

                    foreach (var id in selectedIds)
                    {
                        if (doc.GetElement(id) is FamilyInstance fi &&
                            fi.Category?.Id.GetIdValue() == (long)BuiltInCategory.OST_StructuralFraming)
                        {
                            beams.Add(fi);
                        }
                    }

                    if (beams.Count == 0)
                    {
                        TaskDialog.Show("JNN Rebar", "Vui lòng chọn ít nhất một Dầm kết cấu (Structural Framing)!");
                        return;
                    }

                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép dầm liên tục"))
                    {
                        tg.Start();

                        using (var t = new Transaction(doc, "Tạo thép và mặt cắt dầm"))
                        {
                            t.Start();

                            var beamModel = BeamRebarBuilder.ExtractContinuousBeam(doc, beams);
                            var layoutResult = BeamLayoutEngine.Calculate(beamModel, Settings);
                            int createdCount = BeamRebarBuilder.BuildRebar(doc, beams, layoutResult);

                            if (Settings.CreateLongitudinalSection || Settings.CreateCrossSections)
                            {
                                BeamSectionBuilder.CreateSections(doc, beamModel, Settings);
                            }

                            t.Commit();

                            StatusMessage = $"Hoàn thành: Đã tạo {createdCount} nhóm thép cho chuỗi {beams.Count} dầm.";
                            TaskDialog.Show("JNN Rebar", $"Đã tạo thành công {createdCount} nhóm thép cho chuỗi {beams.Count} nhịp dầm!");
                        }

                        tg.Assimilate();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép dầm", ex);
                    TaskDialog.Show("Lỗi", $"Không thể tạo thép dầm: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = SettingsService<BeamRebarSettings>.Reset();
            StatusMessage = "Đã đặt lại thông số mặc định.";
        }
    }
}
