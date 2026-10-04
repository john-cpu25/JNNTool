using System;
using System.IO;
using System.Text.Json;
using JNNTool.Core.Logging;

namespace JNNTool.Core.Settings
{
    /// <summary>
    /// Dịch vụ đọc và ghi cấu hình người dùng (POCO) dưới dạng JSON trong %AppData%\JNNTool\Settings.
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu cài đặt (POCO)</typeparam>
    public static class SettingsService<T> where T : class, new()
    {
        private static readonly object _fileLock = new object();
        private static readonly string _settingsFolder;
        private static readonly string _filePath;
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        static SettingsService()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFolder = Path.Combine(appData, "JNNTool", "Settings");
            _filePath = Path.Combine(_settingsFolder, $"{typeof(T).Name}.json");

            try
            {
                if (!Directory.Exists(_settingsFolder))
                {
                    Directory.CreateDirectory(_settingsFolder);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Không thể tạo thư mục cài đặt: {_settingsFolder}", ex);
            }
        }

        public static string FilePath => _filePath;

        /// <summary>
        /// Tải cài đặt từ file JSON. Nếu chưa tồn tại hoặc lỗi, trả về instance mới với giá trị mặc định.
        /// </summary>
        public static T Load()
        {
            lock (_fileLock)
            {
                try
                {
                    if (File.Exists(_filePath))
                    {
                        string json = File.ReadAllText(_filePath);
                        var data = JsonSerializer.Deserialize<T>(json, _jsonOptions);
                        if (data != null) return data;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi khi tải cài đặt {typeof(T).Name} từ {_filePath}", ex);
                }

                var defaultSettings = new T();
                Save(defaultSettings);
                return defaultSettings;
            }
        }

        /// <summary>
        /// Lưu cài đặt hiện tại xuống file JSON.
        /// </summary>
        public static void Save(T settings)
        {
            if (settings == null) return;

            lock (_fileLock)
            {
                try
                {
                    string json = JsonSerializer.Serialize(settings, _jsonOptions);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Lỗi khi lưu cài đặt {typeof(T).Name} vào {_filePath}", ex);
                }
            }
        }

        /// <summary>
        /// Đặt lại cài đặt về mặc định và lưu file.
        /// </summary>
        public static T Reset()
        {
            var defaultSettings = new T();
            Save(defaultSettings);
            return defaultSettings;
        }
    }
}
