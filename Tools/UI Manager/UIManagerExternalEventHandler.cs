using System;
using Autodesk.Revit.UI;
using JNNTool.Tools.UIManager.Models;
using JNNTool.Tools.UIManager.Services;
using JNNTool.Tools.UIManager.ViewModels;

namespace JNNTool.Tools.UIManager
{
    /// <summary>
    /// Handler điều phối từ Modeless UI sang Revit Thread an toàn qua ExternalEvent.
    /// </summary>
    public class UIManagerExternalEventHandler : IExternalEventHandler
    {
        private ExternalEvent? _externalEvent;

        public UIManagerViewModel? ViewModel { get; set; }
        public RibbonThemeSettings? Settings { get; set; }

        public UIManagerExternalEventHandler()
        {
            _externalEvent = ExternalEvent.Create(this);
        }

        public void Execute(UIApplication app)
        {
            try
            {
                if (Settings != null)
                {
                    int updated = RibbonThemeService.ApplyThemeToRibbon(app, Settings);
                    if (ViewModel != null)
                    {
                        ViewModel.StatusMessage = $"Updated {updated} ribbon icons successfully!";
                    }
                }
            }
            catch (Exception ex)
            {
                if (ViewModel != null)
                {
                    ViewModel.StatusMessage = $"Error updating ribbon: {ex.Message}";
                }
            }
        }

        public string GetName() => "JNNTool_UIManager_ExternalEventHandler";

        public void Raise()
        {
            _externalEvent?.Raise();
        }
    }
}
