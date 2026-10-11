#nullable enable
using System;
using Autodesk.Revit.UI;
using JNNTool.Core.Logging;

namespace JNNTool.Tools.AutoDimColumn.Commands
{
    /// <summary>
    /// Dedicated ExternalEventHandler for Auto Column Dimension.
    /// Created directly on the Revit UI thread during Command.Execute.
    /// </summary>
    public class AutoDimColumnHandler : IExternalEventHandler
    {
        public Action<UIApplication>? RequestAction { get; set; }

        public void Execute(UIApplication app)
        {
            if (RequestAction == null) return;

            var action = RequestAction;
            RequestAction = null;

            try
            {
                action(app);
            }
            catch (Exception ex)
            {
                Logger.Error("Error executing AutoDimColumnHandler action", ex);
            }
        }

        public string GetName() => "JNNTool_AutoDimColumnHandler";
    }
}
