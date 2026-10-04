using System;
using System.IO;

namespace JNNTool.Core.Logging
{
    /// <summary>
    /// Ghi log hệ thống cho JNNTool tại %AppData%\JNNTool\Logs.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lockObj = new object();
        private static readonly string _logDir;

        static Logger()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _logDir = Path.Combine(appData, "JNNTool", "Logs");

            try
            {
                if (!Directory.Exists(_logDir))
                {
                    Directory.CreateDirectory(_logDir);
                }
            }
            catch
            {
                // Bỏ qua lỗi khởi tạo thư mục
            }
        }

        public static string LogDirectory => _logDir;

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Warning(string message)
        {
            WriteLog("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            string details = ex != null ? $"{message}\nException: {ex.GetType().Name} - {ex.Message}\nStackTrace:\n{ex.StackTrace}" : message;
            WriteLog("ERROR", details);
        }

        private static void WriteLog(string level, string message)
        {
            try
            {
                string logFile = Path.Combine(_logDir, $"JNNTool_{DateTime.Now:yyyyMMdd}.log");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

                lock (_lockObj)
                {
                    File.AppendAllText(logFile, logEntry);
                }
            }
            catch
            {
                // Logging không bao giờ gây crash Revit
            }
        }
    }
}
