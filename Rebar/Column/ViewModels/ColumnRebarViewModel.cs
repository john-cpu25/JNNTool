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
using JNNTool.RebarSuite.Column.Builders;
using JNNTool.RebarSuite.Column.Engine;
using JNNTool.RebarSuite.Column.Models;

namespace JNNTool.RebarSuite.Column.ViewModels
{
    public class ColumnRebarViewModel : ObservableObject
    {
        private ColumnRebarSettings _settings;
        private string _statusMessage = "Sẵn sàng tạo thép chuỗi cột theo tầng";

        public ColumnRebarSettings Settings
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
            "D16", "D18", "D20", "D22", "D25", "D28", "D32"
        };

        public ObservableCollection<string> StirrupBarOptions { get; } = new ObservableCollection<string>
        {
            "D8", "D10", "D12"
        };

        public ObservableCollection<ColumnStirrupPattern> StirrupPatterns { get; } = new ObservableCollection<ColumnStirrupPattern>
        {
            ColumnStirrupPattern.CN, ColumnStirrupPattern.AB, ColumnStirrupPattern.CTie
        };

        public ICommand CreateRebarCommand { get; }
        public ICommand ResetDefaultsCommand { get; }

        public ColumnRebarViewModel()
        {
            _settings = SettingsService<ColumnRebarSettings>.Load();

            CreateRebarCommand = new RelayCommand(OnCreateRebar);
            ResetDefaultsCommand = new RelayCommand(OnResetDefaults);
        }

        private void OnCreateRebar()
        {
            StatusMessage = "Đang gửi lệnh tới Revit...";
            SettingsService<ColumnRebarSettings>.Save(Settings);

            ActionEventHandler.Instance.Post(app =>
            {
                var uidoc = app.ActiveUIDocument;
                if (uidoc == null) return;
                var doc = uidoc.Document;

                try
                {
                    var selectedIds = uidoc.Selection.GetElementIds();
                    var columns = new List<FamilyInstance>();

                    foreach (var id in selectedIds)
                    {
                        if (doc.GetElement(id) is FamilyInstance fi &&
                            fi.Category?.Id.GetIdValue() == (long)BuiltInCategory.OST_StructuralColumns)
                        {
                            columns.Add(fi);
                        }
                    }

                    if (columns.Count == 0)
                    {
                        TaskDialog.Show("JNN Rebar", "Vui lòng chọn ít nhất một Cột kết cấu (Structural Column)!");
                        return;
                    }

                    using (var tg = new TransactionGroup(doc, "JNN - Tạo thép cột"))
                    {
                        tg.Start();

                        using (var t = new Transaction(doc, "Tạo thép và mặt cắt cột"))
                        {
                            t.Start();

                            var columnStack = ColumnRebarBuilder.ExtractColumnStack(doc, columns);
                            var layoutResult = ColumnLayoutEngine.Calculate(columnStack, Settings);
                            int createdCount = ColumnRebarBuilder.BuildRebar(doc, columns, layoutResult);

                            if (Settings.CreateColumnSection)
                            {
                                ColumnSectionBuilder.CreateSections(doc, columnStack, Settings);
                            }

                            t.Commit();

                            StatusMessage = $"Hoàn thành: Đã tạo {createdCount} nhóm thép cho chuỗi {columns.Count} tầng cột.";
                            TaskDialog.Show("JNN Rebar", $"Đã tạo thành công {createdCount} nhóm thép cho chuỗi {columns.Count} tầng cột!");
                        }

                        tg.Assimilate();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi trong quá trình tạo thép cột", ex);
                    TaskDialog.Show("Lỗi", $"Không thể tạo thép cột: {ex.Message}");
                }
            });
        }

        private void OnResetDefaults()
        {
            Settings = SettingsService<ColumnRebarSettings>.Reset();
            StatusMessage = "Đã đặt lại thông số mặc định.";
        }
    }
}
