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
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                DockablePane pane = commandData.Application.GetDockablePane(CommandPalettePage.PaneId);
                if (pane == null)
                {
                    message = "Command Palette chưa được đăng ký trong Revit.";
                    return Result.Failed;
                }

                if (pane.IsShown())
                {
                    pane.Hide();
                }
                else
                {
                    pane.Show();
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
