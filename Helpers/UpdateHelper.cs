using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArknightsLauncher.Helpers
{
    public static class UpdateHelper
    {
        public static async Task<(bool hasUpdate, string latestVersion, string downloadUrl)> CheckForUpdateAsync()
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string apiUrl = "https://api.github.com/repos/lTinchl/ArknightsLauncher/releases/latest";
            var response = await client.GetStringAsync(apiUrl);
            var json = JsonDocument.Parse(response);
            string tagName = json.RootElement.GetProperty("tag_name").GetString();
            string downloadUrl = AppInfo.GitHubReleasesUrl;

            string latestVersionStr = tagName.TrimStart('v').TrimStart('V');
            var latestVersion = new Version(latestVersionStr);
            return (latestVersion > AppInfo.CurrentVersion, latestVersionStr, downloadUrl);
        }
    }
}
