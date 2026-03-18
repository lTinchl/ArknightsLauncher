using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace ArknightsLauncher.Helpers
{
    public static class UpdateHelper
    {
        public static async Task<(bool hasUpdate, string latestVersion, string downloadUrl)> CheckForUpdateAsync(bool useChina = false)
        {
            string tagName, downloadUrl;

            if (useChina)
            {
                using var chinaClient = new HttpClient();
                chinaClient.Timeout = TimeSpan.FromSeconds(10);
                chinaClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string chinaApi = "http://47.107.30.27:8080/launcher/version.json";
                var response = await chinaClient.GetStringAsync(chinaApi);
                var json = JsonDocument.Parse(response);
                tagName = json.RootElement.GetProperty("version").GetString();
                downloadUrl = json.RootElement.GetProperty("download_url").GetString();
            }
            else
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

                string apiUrl = "https://api.github.com/repos/lTinchl/ArknightsLauncher/releases/latest";
                var response = await client.GetStringAsync(apiUrl);
                var json = JsonDocument.Parse(response);
                tagName = json.RootElement.GetProperty("tag_name").GetString();
                downloadUrl = json.RootElement
                    .GetProperty("assets")[0]
                    .GetProperty("browser_download_url")
                    .GetString();
            }

            string latestVersionStr = tagName.TrimStart('v').TrimStart('V');
            var latestVersion = new Version(latestVersionStr);
            return (latestVersion > AppInfo.CurrentVersion, latestVersionStr, downloadUrl);
        }

        public static async Task DownloadAndInstallAsync(string downloadUrl, Button btn, string originalText)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(300);

            string tempNewExe = Path.Combine(Path.GetTempPath(), "ArknightsLauncher_new.exe");
            string batPath = Path.Combine(Path.GetTempPath(), "update_cleanup.bat");
            string currentExe = Application.ExecutablePath;

            var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            long? totalBytes = response.Content.Headers.ContentLength;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(tempNewExe, FileMode.Create))
            {
                var buffer = new byte[8192];
                long downloaded = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloaded += bytesRead;

                    if (totalBytes.HasValue)
                    {
                        int percent = (int)(downloaded * 100 / totalBytes.Value);
                        btn.Invoke((Action)(() => btn.Text = $"下载中 {percent}%"));
                    }
                }
            }

            string batContent = "@echo off\r\n" +
                "timeout /t 2 /nobreak >nul\r\n" +
                $"copy /y \"{tempNewExe}\" \"{currentExe}\"\r\n" +
                $"start \"\" \"{currentExe}\"\r\n" +
                $"del \"{tempNewExe}\"\r\n" +
                "del \"%~f0\"\r\n";

            File.WriteAllText(batPath, batContent, System.Text.Encoding.Default);

            Process.Start(new ProcessStartInfo
            {
                FileName = batPath,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            Application.Exit();
        }
    }
}
