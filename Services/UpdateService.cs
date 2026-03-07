using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArknightsLauncher.Services
{
    public class UpdateProgress
    {
        public int Percent { get; set; }
        public string Message { get; set; } = "";
    }

    public static class UpdateService
    {
        public static readonly Version CurrentVersion = new("1.3.5.2");

        public static async Task<(bool hasUpdate, string latestVersion, string downloadUrl)>
            CheckAsync(bool useChina = false)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string tagName, downloadUrl;

            if (useChina)
            {
                string chinaApi = "http://47.107.30.27:8080/launcher/version.json";
                var response = await client.GetStringAsync(chinaApi);
                var json = JsonDocument.Parse(response);
                tagName = json.RootElement.GetProperty("version").GetString()!;
                downloadUrl = json.RootElement.GetProperty("download_url").GetString()!;
            }
            else
            {
                string apiUrl = "https://api.github.com/repos/lTinchl/ArknightsLauncher/releases/latest";
                var response = await client.GetStringAsync(apiUrl);
                var json = JsonDocument.Parse(response);
                tagName = json.RootElement.GetProperty("tag_name").GetString()!;
                downloadUrl = json.RootElement
                    .GetProperty("assets")[0]
                    .GetProperty("browser_download_url")
                    .GetString()!;
            }

            string latestVersionStr = tagName.TrimStart('v').TrimStart('V');
            var latestVersion = new Version(latestVersionStr);
            return (latestVersion > CurrentVersion, latestVersionStr, downloadUrl);
        }

        public static async Task DownloadAndInstallAsync(
            string downloadUrl,
            IProgress<UpdateProgress>? progress = null)
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(300);
            string tempPath = Path.Combine(Path.GetTempPath(), "update_setup.exe");

            var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            long? totalBytes = response.Content.Headers.ContentLength;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                var buffer = new byte[8192];
                long downloaded = 0;
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                    downloaded += bytesRead;

                    if (totalBytes.HasValue)
                    {
                        int pct = (int)(downloaded * 100 / totalBytes.Value);
                        progress?.Report(new UpdateProgress { Percent = pct, Message = $"下载中 {pct}%" });
                    }
                }
            }

            System.Diagnostics.Process.Start(tempPath);
            Microsoft.UI.Xaml.Application.Current.Exit();
        }
    }
}
