using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ArknightsLauncher.Helpers
{
    public static class GameLauncher
    {
        public static void StartArknights(string rootPath)
        {
            string exePath = Path.Combine(rootPath, "Arknights.exe");
            if (!File.Exists(exePath)) throw new Exception("未找到 Arknights.exe");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = rootPath,
                UseShellExecute = true
            });
        }

        public static void StartMAA(string exePath)
        {
            if (!File.Exists(exePath))
                throw new Exception("未找到 MAA.exe");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });
        }

        public static void KillArknightsProcesses()
        {
            foreach (var proc in Process.GetProcessesByName("Arknights"))
            {
                proc.Kill();
                proc.WaitForExit();
            }
            foreach (var proc in Process.GetProcessesByName("PlatformProcess"))
            {
                proc.Kill();
                proc.WaitForExit();
            }
        }

        public static async Task BackupAccount(string accountName)
        {
            string sdkPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "Hypergryph", "Arknights"
            );

            string target = Path.Combine(ConfigHelper.AccountBackupDir, accountName);

            var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
            if (sdkDir == null) return;

            if (Directory.Exists(target)) Directory.Delete(target, true);
            await CopyDirectory(sdkDir, target);
        }

        public static async Task CopyDirectory(string sourceDir, string targetDir, int maxRetries = 5)
        {
            sourceDir = Path.GetFullPath(sourceDir).TrimEnd(Path.DirectorySeparatorChar);
            targetDir = Path.GetFullPath(targetDir).TrimEnd(Path.DirectorySeparatorChar);

            Directory.CreateDirectory(targetDir);

            foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                string relativePath = file.Substring(sourceDir.Length + 1);
                string destFile = Path.Combine(targetDir, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

                for (int i = 0; i < maxRetries; i++)
                {
                    try
                    {
                        File.Copy(file, destFile, true);
                        break;
                    }
                    catch (IOException) when (i < maxRetries - 1)
                    {
                        await Task.Delay(1000);
                    }
                }
            }
        }
    }
}
