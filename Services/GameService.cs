using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace ArknightsLauncher.Services
{
    public static class GameService
    {
        // ──────────────────────────────────────────────
        //  启动方法
        // ──────────────────────────────────────────────

        public static void StartArknights(string rootPath)
        {
            string exePath = Path.Combine(rootPath, "Arknights.exe");
            if (!File.Exists(exePath))
                throw new FileNotFoundException("未找到 Arknights.exe", exePath);

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
                throw new FileNotFoundException("未找到 MAA.exe", exePath);

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = true
            });
        }

        public static void KillArknights()
        {
            foreach (var p in Process.GetProcessesByName("Arknights"))
            {
                p.Kill();
                p.WaitForExit();
            }
        }

        // ──────────────────────────────────────────────
        //  文件操作
        // ──────────────────────────────────────────────

        /// 从嵌入资源解压并覆盖到目标目录
        public static void ExtractAndOverwrite(string targetRoot, string zipResourceName)
        {
            var asm = Assembly.GetExecutingAssembly();
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"temp_{zipResourceName}");

            try
            {
                using (var res = asm.GetManifestResourceStream($"ArknightsLauncher.{zipResourceName}"))
                {
                    if (res == null)
                        throw new Exception($"未找到嵌入资源: {zipResourceName}");

                    using var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write);
                    res.CopyTo(fs);
                }

                const int maxRetry = 5;
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

        // ──────────────────────────────────────────────
        //  账号备份 / 恢复
        // ──────────────────────────────────────────────

        public static string GetSdkDir()
        {
            string sdkPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "AppData", "LocalLow", "Hypergryph", "Arknights");

            return Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault() ?? "";
        }

        public static void BackupAccount(string accountId)
        {
            string sdkDir = GetSdkDir();
            if (string.IsNullOrEmpty(sdkDir)) return;

            string target = Path.Combine(ConfigService.AccountBackupDir, accountId);
            if (Directory.Exists(target)) Directory.Delete(target, true);
            CopyDirectory(sdkDir, target);
        }

        public static void RestoreAccount(string accountId, string sdkDir)
        {
            string backupFolder = Path.Combine(ConfigService.AccountBackupDir, accountId);
            if (Directory.Exists(backupFolder))
                CopyDirectory(backupFolder, sdkDir);
        }

        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));

            foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(sourceDir, targetDir), true);
        }
    }
}
