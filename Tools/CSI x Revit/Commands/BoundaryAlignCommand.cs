using System;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.ExternalEvents;
using JNNTool.Core.Logging;
using JNNTool.Tools.CSIxRevit.UI;

namespace JNNTool.Tools.CSIxRevit.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class BoundaryAlignCommand : IExternalCommand
    {
        private static BoundaryAlignWindow? _window;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            if (uidoc == null)
            {
                message = "Không tìm thấy tài liệu Revit đang mở.";
                return Result.Failed;
            }

            try
            {
                // Đảm bảo ActionEventHandler đã sẵn sàng trên UI thread
                ActionEventHandler.Instance.Initialize();

                var currentSelection = uidoc.Selection.GetElementIds();

                // Modeless Window Check (Chuẩn JNNTool Modeless Pattern)
                if (_window == null || !_window.IsLoaded)
                {
                    _window = new BoundaryAlignWindow(uidoc, currentSelection);
                    _window.Closed += (s, e) => _window = null;

                    var helper = new System.Windows.Interop.WindowInteropHelper(_window);
                    helper.Owner = uiapp.MainWindowHandle;

                    _window.Show();
                }
                else
                {
                    // Nếu cửa sổ đang mở mà người dùng quét chọn cấu kiện mới rồi bấm lại tool:
                    if (currentSelection != null && currentSelection.Count > 0)
                    {
                        _window.SetSelection(currentSelection);
                    }

                    if (_window.WindowState == System.Windows.WindowState.Minimized)
                    {
                        _window.WindowState = System.Windows.WindowState.Normal;
                    }

                    _window.Activate();
                    _window.Focus();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("Lỗi khi mở công cụ Căn Lề Biên", ex);
                message = ex.Message;
                TaskDialog.Show("Lỗi", $"Không thể mở công cụ Căn Lề Biên: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}
