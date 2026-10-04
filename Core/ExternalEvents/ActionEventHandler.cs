using System;
using System.Collections.Concurrent;
using Autodesk.Revit.UI;

namespace JNNTool.Core.ExternalEvents
{
    /// <summary>
    /// Handler điều phối việc thực thi lệnh từ Modeless UI sang Revit Thread thông qua ExternalEvent.
    /// </summary>
    public class ActionEventHandler : IExternalEventHandler
    {
        private readonly ConcurrentQueue<Action<UIApplication>> _actionQueue = new ConcurrentQueue<Action<UIApplication>>();
        private ExternalEvent? _externalEvent;

        public static ActionEventHandler Instance { get; } = new ActionEventHandler();

        public ActionEventHandler()
        {
        }

        /// <summary>
        /// Khởi tạo ExternalEvent khi Add-in Revit khởi động (trên Revit UI Thread).
        /// </summary>
        public void Initialize()
        {
            if (_externalEvent == null)
            {
                _externalEvent = ExternalEvent.Create(this);
            }
        }

        /// <summary>
        /// Đẩy một Action vào hàng đợi và kích hoạt ExternalEvent để Revit thực thi trên UI Thread.
        /// </summary>
        public void Post(Action<UIApplication> action)
        {
            if (action == null) return;

            _actionQueue.Enqueue(action);
            _externalEvent?.Raise();
        }

        public void Execute(UIApplication app)
        {
            while (_actionQueue.TryDequeue(out var action))
            {
                try
                {
                    action(app);
                }
                catch (Exception ex)
                {
                    Logging.Logger.Error("Lỗi trong khi thực thi ExternalEvent Action", ex);
                }
            }
        }

        public string GetName() => "JNNTool_ActionEventHandler";
    }
}
