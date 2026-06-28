using System.IO;
using System.Reflection;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ArknightsLauncher.Helpers
{
    public static class ResourceHelper
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

        public static System.Drawing.Icon LoadIcon(string name)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{name}");
            return stream != null ? new System.Drawing.Icon(stream) : System.Drawing.SystemIcons.Application;
        }

        public static string GetLoadPayloadDirectory(bool isOfficial)
        {
            string folderName = isOfficial ? "ArkOfficial" : "ArkBilibili";
            return Path.Combine(System.AppContext.BaseDirectory, "load", folderName);
        }

        public static bool LinkLoadPayloadAndOverwrite(string targetRoot, bool isOfficial)
        {
            string sourceRoot = GetLoadPayloadDirectory(isOfficial);
            if (!Directory.Exists(sourceRoot))
                throw new System.Exception($"未找到加载文件目录: {sourceRoot}");

            return HardLinkOrCopyDirectory(sourceRoot, targetRoot);
        }

        public static bool HardLinkOrCopyDirectory(string sourceRoot, string targetRoot)
        {
            sourceRoot = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar);
            targetRoot = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar);

            Directory.CreateDirectory(targetRoot);
            bool sameVolume = OnSameVolume(sourceRoot, targetRoot);
            bool usedHardLinkForAllFiles = sameVolume;

            foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string relativePath = sourceFile.Substring(sourceRoot.Length + 1);
                string targetFile = Path.Combine(targetRoot, relativePath);
                string targetDir = Path.GetDirectoryName(targetFile);
                if (!string.IsNullOrEmpty(targetDir))
                    Directory.CreateDirectory(targetDir);

                const int maxRetry = 5;
                for (int i = 0; i < maxRetry; i++)
                {
                    try
                    {
                        if (!HardLinkOrCopyFile(sourceFile, targetFile, sameVolume))
                            usedHardLinkForAllFiles = false;
                        break;
                    }
                    catch (IOException) when (i < maxRetry - 1)
                    {
                        Thread.Sleep(1000);
                    }
                }
            }

            return usedHardLinkForAllFiles;
        }

        private static bool OnSameVolume(string pathA, string pathB)
        {
            string rootA = Path.GetPathRoot(Path.GetFullPath(pathA));
            string rootB = Path.GetPathRoot(Path.GetFullPath(pathB));
            return string.Equals(rootA, rootB, StringComparison.OrdinalIgnoreCase);
        }

        private static bool HardLinkOrCopyFile(string sourceFile, string targetFile, bool sameVolume)
        {
            if (sameVolume && TryReplaceWithHardLink(sourceFile, targetFile))
                return true;

            ReplaceFileByCopy(sourceFile, targetFile);
            return false;
        }

        private static bool TryReplaceWithHardLink(string sourceFile, string targetFile)
        {
            try
            {
                DeleteTargetFile(targetFile);
                return CreateHardLink(targetFile, sourceFile, IntPtr.Zero);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private static void ReplaceFileByCopy(string sourceFile, string targetFile)
        {
            DeleteTargetFile(targetFile);
            File.Copy(sourceFile, targetFile, true);
        }

        private static void DeleteTargetFile(string targetFile)
        {
            if (!File.Exists(targetFile))
                return;

            File.SetAttributes(targetFile, FileAttributes.Normal);
            File.Delete(targetFile);
        }
    }
}
