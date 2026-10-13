using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace JNNTool.Core.Palette
{
    /// <summary>
    /// Nạp icon PNG từ thư mục Resources của JNNTool với kích thước tối ưu.
    /// </summary>
    internal static class IconLoader
    {
        private static string? _resourcesDir;

        public static string GetResourcesDir()
        {
            if (_resourcesDir == null)
            {
                try
                {
                    string assemblyPath = Assembly.GetExecutingAssembly().Location;
                    string assemblyDir = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
                    DirectoryInfo? parent = Directory.GetParent(assemblyDir);
                    if (parent != null)
                    {
                        string candidate = Path.Combine(parent.FullName, "Resources");
                        if (Directory.Exists(candidate)) _resourcesDir = candidate;
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(_resourcesDir))
                {
                    _resourcesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
                }
            }
            return _resourcesDir;
        }

        public static ImageSource? Load(string? fileName, int size = 0)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            try
            {
                string fullPath = Path.Combine(GetResourcesDir(), fileName);
                if (!File.Exists(fullPath)) return null;

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(fullPath, UriKind.Absolute);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                if (size > 0)
                {
                    bmp.DecodePixelWidth = size;
                    bmp.DecodePixelHeight = size;
                }
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }
    }
}
