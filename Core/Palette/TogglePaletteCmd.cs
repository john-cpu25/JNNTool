using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Lệnh Ribbon bật / tắt JNN Command Palette Dockable Pane.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class TogglePaletteCmd : IExternalCommand
    {
        private static DateTime _lastToggleTime = DateTime.MinValue;
        private static readonly object _syncLock = new object();
        private const int DebounceMs = 800;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Debounce để ngăn người dùng click liên tiếp hoặc double-click làm crash native docking manager của Revit
            lock (_syncLock)
            {
                var elapsed = (DateTime.UtcNow - _lastToggleTime).TotalMilliseconds;
                if (elapsed < DebounceMs)
                {
                    return Result.Succeeded;
                }
                _lastToggleTime = DateTime.UtcNow;
            }

            try
            {
                if (!DockablePane.PaneIsRegistered(PaletteHost.PaneId))
                {
                    message = "Command Palette chưa được đăng ký trong phiên làm việc hiện tại của Revit.";
                    return Result.Failed;
                }

                DockablePane pane = commandData.Application.GetDockablePane(PaletteHost.PaneId);
                if (pane == null)
                {
                    message = "Không tìm thấy Dockable Pane Command Palette.";
                    return Result.Failed;
                }

                try
                {
                    if (pane.IsShown())
                    {
                        pane.Hide();
                    }
                    else
                    {
                        pane.Show();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("Lỗi khi Show/Hide DockablePane", ex);
                    try { pane.Show(); } catch { }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                Logger.Error("TogglePaletteCmd error", ex);
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    /// <summary>
    /// Cho phép bấm nút kể cả khi chưa mở dự án nào trong Revit.
    /// </summary>
    public class AlwaysAvailable : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication applicationData, CategorySet selectedCategories) => true;
    }
}
