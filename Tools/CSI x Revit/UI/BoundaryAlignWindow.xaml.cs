using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.ExternalEvents;
using JNNTool.Tools.CSIxRevit.Builder;

namespace JNNTool.Tools.CSIxRevit.UI
{
    public partial class BoundaryAlignWindow : Window
    {
        private readonly UIDocument _uidoc;
        private readonly Document _doc;
        private List<ElementId> _selectedElementIds = new List<ElementId>();

        public BoundaryAlignWindow(UIDocument uidoc, ICollection<ElementId> initialSelection = null)
        {
            InitializeComponent();
            _uidoc = uidoc ?? throw new ArgumentNullException(nameof(uidoc));
            _doc = _uidoc.Document;

            SetSelection(initialSelection);
        }

        public void SetSelection(ICollection<ElementId> selection)
        {
            if (selection != null && selection.Count > 0)
            {
                var validIds = new List<ElementId>();
                var framingId = new ElementId(BuiltInCategory.OST_StructuralFraming);
                var colId = new ElementId(BuiltInCategory.OST_StructuralColumns);

                foreach (var id in selection)
                {
                    var elem = _doc.GetElement(id);
                    var cat = elem?.Category;
                    if (cat != null && (cat.Id == framingId || cat.Id == colId))
                    {
                        validIds.Add(id);
                    }
                }

                if (validIds.Count > 0)
                {
                    _selectedElementIds = validIds;
                }
            }

            UpdateSelectionStatus();
        }

        private void BtnPickObjects_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();

            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var uidoc = app.ActiveUIDocument;
                    var selectedRefs = uidoc.Selection.PickObjects(
                        Autodesk.Revit.UI.Selection.ObjectType.Element,
                        new FrameAndColumnSelectionFilter(),
                        "Quét chọn các Dầm và Cột biên cần căn lề..."
                    );

                    var ids = selectedRefs.Select(r => r.ElementId).Distinct().ToList();

                    Dispatcher.Invoke(() =>
                    {
                        _selectedElementIds = ids;
                        UpdateSelectionStatus();
                        this.Show();
                        this.Activate();
                    });
                }
                catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                {
                    Dispatcher.Invoke(() =>
                    {
                        this.Show();
                        this.Activate();
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TaskDialog.Show("Lỗi", $"Lỗi quét chọn: {ex.Message}");
                        this.Show();
                        this.Activate();
                    });
                }
            });
        }

        private void BtnUseCurrentSelection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selIds = _uidoc.Selection.GetElementIds();
                var validIds = new List<ElementId>();
                var framingId = new ElementId(BuiltInCategory.OST_StructuralFraming);
                var colId = new ElementId(BuiltInCategory.OST_StructuralColumns);

                foreach (var id in selIds)
                {
                    var elem = _doc.GetElement(id);
                    var cat = elem?.Category;
                    if (cat != null && (cat.Id == framingId || cat.Id == colId))
                    {
                        validIds.Add(id);
                    }
                }

                if (validIds.Count > 0)
                {
                    _selectedElementIds = validIds;
                    UpdateSelectionStatus();
                }
                else
                {
                    TaskDialog.Show("Thông báo", "Không có Dầm hoặc Cột kết cấu nào trong danh sách đang chọn trên Revit.\n\nVui lòng quét chọn dầm/cột trên mô hình Revit rồi bấm lại nút này.");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Lỗi", $"Không thể lấy lựa chọn hiện tại: {ex.Message}");
            }
        }

        private void UpdateSelectionStatus()
        {
            if (_selectedElementIds == null || _selectedElementIds.Count == 0)
            {
                TxtSelectionStatus.Text = "Chưa chọn đối tượng nào. Bấm nút 'Quét chọn' để kéo chuột chọn dầm, cột biên trên mặt bằng hoặc 3D.";
                TxtAlignResultLog.Text = "Sẵn sàng thực hiện căn lề biên.";
                return;
            }

            int beamCount = 0;
            int colCount = 0;
            var framingId = new ElementId(BuiltInCategory.OST_StructuralFraming);
            var colId = new ElementId(BuiltInCategory.OST_StructuralColumns);

            foreach (var id in _selectedElementIds)
            {
                var elem = _doc.GetElement(id);
                var cat = elem?.Category;
                if (cat != null)
                {
                    if (cat.Id == framingId) beamCount++;
                    else if (cat.Id == colId) colCount++;
                }
            }

            TxtSelectionStatus.Text = $"✅ Đã chọn thành công: {beamCount} Dầm và {colCount} Cột kết cấu.";
            TxtAlignResultLog.Text = $"Sẵn sàng căn lề cho {beamCount} dầm và {colCount} cột.";
        }

        private void BtnExecuteEdgeAlign_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedElementIds == null || _selectedElementIds.Count == 0)
            {
                TaskDialog.Show("Thông báo", "Vui lòng chọn Dầm & Cột trước khi thực hiện căn lề biên!");
                return;
            }

            AlignDirection dir = AlignDirection.AutoGrid;
            if (RbShiftUpY.IsChecked == true) dir = AlignDirection.UpY;
            else if (RbShiftDownY.IsChecked == true) dir = AlignDirection.DownY;
            else if (RbShiftRightX.IsChecked == true) dir = AlignDirection.RightX;
            else if (RbShiftLeftX.IsChecked == true) dir = AlignDirection.LeftX;

            double customOffsetFeet = 0;
            if (RbOffsetCustom.IsChecked == true)
            {
                if (double.TryParse(TxtCustomOffsetMm.Text.Trim(), out double offsetMm) && offsetMm > 0)
                {
                    customOffsetFeet = UnitUtils.ConvertToInternalUnits(offsetMm, UnitTypeId.Millimeters);
                }
                else
                {
                    TaskDialog.Show("Thông báo", "Vui lòng nhập khoảng cách dịch hợp lệ (> 0 mm)!");
                    return;
                }
            }

            bool autoExtend = ChkAutoExtendIntersecting.IsChecked == true;
            var targetIds = new List<ElementId>(_selectedElementIds);

            TxtAlignResultLog.Text = "⏳ Đang thực hiện căn lề biên...";

            // Thực thi an toàn 100% trong Revit API context qua ExternalEvent
            ActionEventHandler.Instance.Post(app =>
            {
                try
                {
                    var doc = app.ActiveUIDocument.Document;
                    var alignResult = BoundaryAligner.AlignElements(
                        doc,
                        targetIds,
                        dir,
                        customOffsetFeet,
                        autoExtend
                    );

                    Dispatcher.Invoke(() =>
                    {
                        TxtAlignResultLog.Text = alignResult.Message;

                        string summary = $"🎉 KẾT QUẢ CĂN LỀ BIÊN:\n\n" +
                                         $"• Dầm đã dời: {alignResult.MovedBeams}\n" +
                                         $"• Cột đã dời: {alignResult.MovedColumns}\n" +
                                         (alignResult.AdjustedIntersectingBeams > 0 ? $"• Dầm ngang tự động co/kéo: {alignResult.AdjustedIntersectingBeams}\n" : "") +
                                         $"\nThông báo: {alignResult.Message}";

                        TaskDialog.Show("Căn Lề Thành Công", summary);
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        TxtAlignResultLog.Text = "❌ Lỗi: " + ex.Message;
                        TaskDialog.Show("Lỗi Căn Lề Biên", $"Gặp lỗi khi thực hiện căn lề biên: {ex.Message}");
                    });
                }
            });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
