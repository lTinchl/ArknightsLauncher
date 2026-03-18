using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace ArknightsLauncher.Helpers
{
    public static class ResourceHelper
    {
        public static System.Drawing.Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{name}");
            return stream != null ? new System.Drawing.Icon(stream) : System.Drawing.SystemIcons.Application;
        }

        public static void ExtractAndOverwrite(string targetRoot, string zipResourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            string tempZipPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"temp_{zipResourceName}");

            try
            {
                using (Stream resourceStream = asm.GetManifestResourceStream($"ArknightsLauncher.{zipResourceName}"))
                {
                    if (resourceStream == null)
                        throw new System.Exception($"未找到嵌入的资源: {zipResourceName}");

                    using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                        resourceStream.CopyTo(fileStream);
                }

                int maxRetry = 5;
                for (int i = 0; i < maxRetry; i++)
                {
                    try
                    {
                        ZipFile.ExtractToDirectory(tempZipPath, targetRoot, true);
                        break;
                    }
                    catch (IOException) when (i < maxRetry - 1)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }
            finally
            {
                if (File.Exists(tempZipPath))
                    File.Delete(tempZipPath);
            }
        }
    }
}
