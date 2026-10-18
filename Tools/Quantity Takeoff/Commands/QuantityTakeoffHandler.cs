using System;
using Autodesk.Revit.UI;
using JNNTool.Tools.QuantityTakeoff.Helpers;

namespace JNNTool.Tools.QuantityTakeoff.Commands
{
    public class QuantityTakeoffHandler : IExternalEventHandler
    {
        private Action<UIApplication> _pendingAction;
        private readonly object _lock = new object();

        public void SetAction(Action<UIApplication> action)
        {
            lock (_lock)
            {
                _pendingAction = action;
            }
        }

        public void Execute(UIApplication app)
        {
            Action<UIApplication> actionToRun = null;
            lock (_lock)
            {
                actionToRun = _pendingAction;
                _pendingAction = null;
            }

            if (actionToRun == null) return;

            try
            {
                actionToRun.Invoke(app);
            }
            catch (Exception ex)
            {
                QtoLogger.Error("Lỗi thực thi trong QuantityTakeoffHandler", ex);
            }
        }

        public string GetName() => "JNN_QuantityTakeoffHandler";
    }
}
