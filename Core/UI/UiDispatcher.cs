using System;
using System.Windows.Threading;

namespace JNNTool.Core.UI
{
    /// <summary>
    /// Dispatcher UI an toàn cho Revit. Revit không tạo System.Windows.Application chuẩn,
    /// nên Dispatcher được chốt lại ngay trong App.OnStartup (chạy trên UI thread của Revit)
    /// để luôn gọi an toàn từ mọi thread.
    /// </summary>
    public static class UiDispatcher
    {
        private static Dispatcher? _dispatcher;

        public static void Capture()
        {
            try 
            { 
                _dispatcher = Dispatcher.CurrentDispatcher; 
            }
            catch { }
        }

        public static Dispatcher Current
        {
            get
            {
                if (_dispatcher != null) return _dispatcher;

                try
                {
                    if (System.Windows.Application.Current != null)
                    {
                        return System.Windows.Application.Current.Dispatcher;
                    }
                }
                catch { }

                return Dispatcher.CurrentDispatcher;
            }
        }

        public static void Invoke(Action action)
        {
            if (action == null) return;
            try
            {
                Dispatcher d = Current;
                if (d == null || d.CheckAccess()) action();
                else d.Invoke(action);
            }
            catch
            {
                // Tránh lỗi UI làm crash Revit
            }
        }
    }
}
