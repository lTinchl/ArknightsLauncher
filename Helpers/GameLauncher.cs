using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections.Generic;
using ArknightsLauncher.Models;

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

        public static void StartLinkedSoftware(string exePath)
        {
            if (!File.Exists(exePath))
                throw new Exception("未找到联动软件程序");

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });
        }

        public static void StartLinkedSoftwares(IEnumerable<LinkedSoftwareItem> softwares, bool requireAny = true)
        {
            var items = softwares
                .Where(item => !string.IsNullOrWhiteSpace(item.Path))
                .ToList();

            if (items.Count == 0)
            {
                if (requireAny)
                    throw new Exception("请先在设置中添加联动软件");
                return;
            }

            var errors = new List<string>();
            foreach (var item in items)
            {
                try
                {
                    StartLinkedSoftware(item.Path);
                }
                catch (Exception ex)
                {
                    string name = string.IsNullOrWhiteSpace(item.Name)
                        ? Path.GetFileNameWithoutExtension(item.Path)
                        : item.Name;
                    errors.Add($"{name}: {ex.Message}");
                }
            }

            if (errors.Count > 0)
                throw new Exception("部分联动软件启动失败：\n" + string.Join("\n", errors));
        }

        public static void StartMAA(string exePath)
        {
            StartLinkedSoftware(exePath);
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
