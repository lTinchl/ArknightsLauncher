using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ArknightsLauncher.Helpers
{
    public static class AppIconHelper
    {
        // 读取 EXE 自带的图标
        public static ImageSource? FromExe(string? exePath)
        {
            if (string.IsNullOrWhiteSpace(exePath) ||
                !File.Exists(exePath))
                return null;

            try
            {
                using var icon =
                    System.Drawing.Icon.ExtractAssociatedIcon(exePath);

                return icon == null ? null : ConvertIcon(icon);
            }
            catch
            {
                // 图标加载失败不影响启动器运行
                return null;
            }
        }

        // 读取项目内嵌的 ICO 图标
        public static ImageSource? FromResource(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var assembly = Assembly.GetExecutingAssembly();

            using var stream = assembly.GetManifestResourceStream(
                $"ArknightsLauncher.Icons.{name}");

            if (stream == null)
                return null;

            using var icon = new System.Drawing.Icon(stream);
            return ConvertIcon(icon);
        }

        private static ImageSource ConvertIcon(
            System.Drawing.Icon icon)
        {
            var image = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromWidthAndHeight(32, 32)
            );

            image.Freeze();
            return image;
        }
    }
}
