using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ArknightsLauncher.Helpers
{
    public static class SklandSignHelper
    {
        private const string AppCode = "4ca99fa6b56cc2ba";
        private const string GrantCodeUrl = "https://as.hypergryph.com/user/oauth2/v2/grant";
        private const string CredCodeUrl = "https://zonai.skland.com/web/v1/user/auth/generate_cred_by_code";
        private const string ScanLoginUrl = "https://as.hypergryph.com/general/v1/gen_scan/login";
        private const string ScanStatusUrl = "https://as.hypergryph.com/general/v1/scan_status";
        private const string TokenScanCodeUrl = "https://as.hypergryph.com/user/auth/v1/token_by_scan_code";
        private const string SendPhoneCodeUrl = "https://as.hypergryph.com/general/v1/send_phone_code";
        private const string TokenPhoneCodeUrl = "https://as.hypergryph.com/user/auth/v2/token_by_phone_code";
        private const string TokenPasswordUrl = "https://as.hypergryph.com/user/auth/v1/token_by_phone_password";
        private const string BindingUrl = "https://zonai.skland.com/api/v1/game/player/binding";
        private const string ArknightsAttendanceUrl = "https://zonai.skland.com/api/v1/game/attendance";
        private const string EndfieldAttendanceUrl = "https://zonai.skland.com/web/v1/game/endfield/attendance";
        private const string UserAgent = "Mozilla/5.0 (Linux; Android 12; SM-A5560 Build/V417IR; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/101.0.4951.61 Safari/537.36; SKLand/1.52.1";
        private static readonly string DeviceId = Guid.NewGuid().ToString("N");

        private static readonly HttpClient Client = new HttpClient(new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        });

        public static async Task<string> SignAsync(string tokenText)
        {
            var tokens = SplitTokens(tokenText);
            if (tokens.Count == 0)
                throw new Exception("请先填写 SKYLAND_TOKEN");

            var messages = new List<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                try
                {
                    var auth = await LoginByTokenAsync(tokens[i]);
                    var accountMessages = await SignAccountAsync(auth.Cred, auth.SignToken);
                    if (accountMessages.Count == 0)
                        messages.Add($"[账号{i + 1}] 没有找到可签到的绑定角色");
                    else
                        messages.AddRange(accountMessages.Select(message => $"[账号{i + 1}] {message}"));
                }
                catch (Exception ex)
                {
                    messages.Add($"[账号{i + 1}] 签到失败: {ex.Message}");
                }
            }

            return string.Join(Environment.NewLine, messages);
        }

        public static async Task<(string ScanId, string ScanUrl)> CreateScanLoginAsync()
        {
            using var response = await Client.SendAsync(CreateLoginRequest(HttpMethod.Post, ScanLoginUrl, JsonContent(new { })));
            using var doc = await ReadJsonAsync(response);
            var root = doc.RootElement;
            CheckAuthResponse(root, "创建扫码登录");

            var data = root.GetProperty("data");
            string scanId = GetString(data, "scanId");
            string scanUrl = GetString(data, "scanUrl");
            if (string.IsNullOrWhiteSpace(scanId) || string.IsNullOrWhiteSpace(scanUrl))
                throw new Exception("创建扫码登录失败: 返回缺少 scanId 或 scanUrl");

            return (scanId, scanUrl);
        }

        public static async Task<string> QueryScanLoginTokenAsync(string scanId)
        {
            string url = ScanStatusUrl + "?scanId=" + Uri.EscapeDataString(scanId);
            using var statusResp = await Client.SendAsync(CreateLoginRequest(HttpMethod.Get, url, null));
            using var statusDoc = await ReadJsonAsync(statusResp);
            var statusRoot = statusDoc.RootElement;
            int status = GetInt(statusRoot, "status");
            var data = statusRoot.TryGetProperty("data", out var dataValue) ? dataValue : default;
            string scanCode = data.ValueKind == JsonValueKind.Object ? GetString(data, "scanCode") : "";

            if (status == 0 && !string.IsNullOrWhiteSpace(scanCode))
                return await LoginByScanCodeAsync(scanCode);

            if (status == 100)
                throw new SklandScanPendingException("等待扫码...");
            if (status == 101)
                throw new SklandScanPendingException("已扫码，等待在 App 内确认登录...");
            if (status == 102)
                throw new Exception("二维码已过期，请重新扫码");

            string message = GetString(statusRoot, "msg");
            throw new SklandScanPendingException(string.IsNullOrWhiteSpace(message) ? $"等待扫码状态: {status}" : message);
        }

        public static async Task<string> LoginByScanCodeAsync(string scanCode)
        {
            using var response = await Client.SendAsync(CreateLoginRequest(HttpMethod.Post, TokenScanCodeUrl, JsonContent(new
            {
                scanCode
            })));
            using var doc = await ReadJsonAsync(response);
            return ExtractLoginToken(doc.RootElement, "扫码登录");
        }

        public static async Task SendPhoneCodeAsync(string phone)
        {
            using var response = await Client.SendAsync(CreateLoginRequest(HttpMethod.Post, SendPhoneCodeUrl, JsonContent(new
            {
                phone,
                type = 2
            })));
            using var doc = await ReadJsonAsync(response);
            CheckAuthResponse(doc.RootElement, "发送验证码");
        }

        public static async Task<string> LoginByPhoneCodeAsync(string phone, string code)
        {
            using var response = await Client.SendAsync(CreateLoginRequest(HttpMethod.Post, TokenPhoneCodeUrl, JsonContent(new
            {
                phone,
                code
            })));
            using var doc = await ReadJsonAsync(response);
            return ExtractLoginToken(doc.RootElement, "手机号验证码登录");
        }

        public static async Task<string> LoginByPasswordAsync(string phone, string password)
        {
            using var response = await Client.SendAsync(CreateLoginRequest(HttpMethod.Post, TokenPasswordUrl, JsonContent(new
            {
                phone,
                password
            })));
            using var doc = await ReadJsonAsync(response);
            return ExtractLoginToken(doc.RootElement, "账号密码登录");
        }

        private static List<string> SplitTokens(string value)
        {
            return (value ?? "")
                .Replace("\r", "\n")
                .Replace(';', '\n')
                .Replace(',', '\n')
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct()
                .ToList();
        }

        private static async Task<(string Cred, string SignToken)> LoginByTokenAsync(string tokenCode)
        {
            string token = ParseUserToken(tokenCode);

            using var grantContent = JsonContent(new
            {
                appCode = AppCode,
                token,
                type = 0
            });
            using var grantResp = await Client.SendAsync(CreateJsonRequest(HttpMethod.Post, GrantCodeUrl, grantContent));
            using var grantDoc = await ReadJsonAsync(grantResp);
            var grantRoot = grantDoc.RootElement;
            if (GetInt(grantRoot, "status") != 0)
                throw new Exception("获取认证代码失败: " + GetString(grantRoot, "msg"));

            string grantCode = grantRoot.GetProperty("data").GetProperty("code").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(grantCode))
                throw new Exception("获取认证代码失败: 返回为空");

            using var credContent = JsonContent(new
            {
                code = grantCode,
                kind = 1
            });
            using var credResp = await Client.SendAsync(CreateJsonRequest(HttpMethod.Post, CredCodeUrl, credContent));
            using var credDoc = await ReadJsonAsync(credResp);
            var credRoot = credDoc.RootElement;
            if (GetInt(credRoot, "code") != 0)
                throw new Exception("获取 cred 失败: " + GetString(credRoot, "message"));

            var data = credRoot.GetProperty("data");
            string cred = data.GetProperty("cred").GetString() ?? "";
            string signToken = data.GetProperty("token").GetString() ?? "";
            if (string.IsNullOrWhiteSpace(cred) || string.IsNullOrWhiteSpace(signToken))
                throw new Exception("获取 cred 失败: 返回缺少 cred 或 token");

            return (cred, signToken);
        }

        private static string ParseUserToken(string tokenCode)
        {
            try
            {
                using var doc = JsonDocument.Parse(tokenCode);
                return doc.RootElement.GetProperty("data").GetProperty("content").GetString() ?? tokenCode;
            }
            catch
            {
                return tokenCode;
            }
        }

        private static string ExtractLoginToken(JsonElement root, string action)
        {
            CheckAuthResponse(root, action);

            var data = root.GetProperty("data");
            string token = GetString(data, "content");
            if (string.IsNullOrWhiteSpace(token))
                token = GetString(data, "token");
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception($"{action}失败: 返回缺少 token");

            return token;
        }

        private static void CheckAuthResponse(JsonElement root, string action)
        {
            int status = root.TryGetProperty("status", out _) ? GetInt(root, "status") : GetInt(root, "code");
            if (status != 0)
            {
                string message = GetString(root, "msg");
                if (string.IsNullOrWhiteSpace(message))
                    message = GetString(root, "message");
                throw new Exception($"{action}失败: {message}");
            }
        }

        private static async Task<List<string>> SignAccountAsync(string cred, string signToken)
        {
            using var request = CreateSignedRequest(HttpMethod.Get, BindingUrl, cred, signToken, null);
            using var response = await Client.SendAsync(request);
            using var doc = await ReadJsonAsync(response);
            var root = doc.RootElement;
            if (GetInt(root, "code") != 0)
                throw new Exception("请求角色列表失败: " + GetString(root, "message"));

            var results = new List<string>();
            foreach (var game in root.GetProperty("data").GetProperty("list").EnumerateArray())
            {
                string appCode = GetString(game, "appCode");
                if (appCode != "arknights" && appCode != "endfield")
                    continue;

                if (!game.TryGetProperty("bindingList", out var bindings))
                    continue;

                foreach (var binding in bindings.EnumerateArray())
                {
                    if (appCode == "arknights")
                        results.Add(await SignArknightsAsync(binding, cred, signToken));
                    else
                        results.AddRange(await SignEndfieldAsync(binding, cred, signToken));
                }
            }

            return results;
        }

        private static async Task<string> SignArknightsAsync(JsonElement binding, string cred, string signToken)
        {
            var body = new Dictionary<string, object>
            {
                ["uid"] = GetString(binding, "uid"),
                ["gameId"] = GetString(binding, "channelMasterId")
            };

            using var request = CreateSignedRequest(HttpMethod.Post, ArknightsAttendanceUrl, cred, signToken, body);
            using var response = await Client.SendAsync(request);
            using var doc = await ReadJsonAsync(response);
            var root = doc.RootElement;

            string gameName = GetString(binding, "gameName");
            string channelName = GetString(binding, "channelName");
            string nickName = GetString(binding, "nickName");

            if (GetInt(root, "code") != 0)
                return $"[{gameName}] 角色 {nickName}({channelName}) 签到失败: {GetString(root, "message")}";

            var awards = new List<string>();
            if (root.GetProperty("data").TryGetProperty("awards", out var awardArray))
            {
                foreach (var award in awardArray.EnumerateArray())
                {
                    var resource = award.GetProperty("resource");
                    string name = GetString(resource, "name");
                    int count = GetInt(award, "count");
                    awards.Add($"{name}x{(count == 0 ? 1 : count)}");
                }
            }

            return $"[{gameName}] 角色 {nickName}({channelName}) 签到成功: {string.Join(" ", awards)}";
        }

        private static async Task<List<string>> SignEndfieldAsync(JsonElement binding, string cred, string signToken)
        {
            var results = new List<string>();
            if (!binding.TryGetProperty("roles", out var roles))
                return results;

            foreach (var role in roles.EnumerateArray())
            {
                using var request = CreateSignedRequest(HttpMethod.Post, EndfieldAttendanceUrl, cred, signToken, null);
                request.Content = new StringContent("", Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("sk-game-role", $"3_{GetString(role, "roleId")}_{GetString(role, "serverId")}");
                request.Headers.TryAddWithoutValidation("referer", "https://game.skland.com/");
                request.Headers.TryAddWithoutValidation("origin", "https://game.skland.com/");

                using var response = await Client.SendAsync(request);
                using var doc = await ReadJsonAsync(response);
                var root = doc.RootElement;

                string gameName = GetString(binding, "gameName");
                string channelName = GetString(binding, "channelName");
                string nickName = GetString(role, "nickname");

                if (GetInt(root, "code") != 0)
                {
                    results.Add($"[{gameName}] 角色 {nickName}({channelName}) 签到失败: {GetString(root, "message")}");
                    continue;
                }

                results.Add($"[{gameName}] 角色 {nickName}({channelName}) 签到成功");
            }

            return results;
        }

        private static HttpRequestMessage CreateSignedRequest(
            HttpMethod method,
            string url,
            string cred,
            string signToken,
            Dictionary<string, object> body)
        {
            string bodyText = body == null ? "" : JsonSerializer.Serialize(body);
            var uri = new Uri(url);
            string bodyOrQuery = method == HttpMethod.Get ? uri.Query.TrimStart('?') : bodyText;
            string timestamp = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 2).ToString();
            string headerCa = JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["platform"] = "3",
                ["timestamp"] = timestamp,
                ["dId"] = "",
                ["vName"] = "1.0.0"
            });

            string sign = CreateSign(signToken, uri.AbsolutePath + bodyOrQuery + timestamp + headerCa);
            var request = CreateJsonRequest(method, url, method == HttpMethod.Get ? null : JsonContent(body));
            request.Headers.TryAddWithoutValidation("cred", cred);
            request.Headers.TryAddWithoutValidation("sign", sign);
            request.Headers.TryAddWithoutValidation("platform", "3");
            request.Headers.TryAddWithoutValidation("timestamp", timestamp);
            request.Headers.TryAddWithoutValidation("dId", "");
            request.Headers.TryAddWithoutValidation("vName", "1.0.0");
            return request;
        }

        private static string CreateSign(string token, string text)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(token));
            string sha = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
            using var md5 = MD5.Create();
            return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(sha))).ToLowerInvariant();
        }

        private static HttpRequestMessage CreateJsonRequest(HttpMethod method, string url, HttpContent content)
        {
            var request = new HttpRequestMessage(method, url);
            if (content != null)
                request.Content = content;
            request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
            request.Headers.TryAddWithoutValidation("Connection", "close");
            request.Headers.TryAddWithoutValidation("X-Requested-With", "com.hypergryph.skland");
            return request;
        }

        private static HttpRequestMessage CreateLoginRequest(HttpMethod method, string url, HttpContent content)
        {
            var request = CreateJsonRequest(method, url, content);
            request.Headers.TryAddWithoutValidation("dId", DeviceId);
            return request;
        }

        private static StringContent JsonContent(object body)
        {
            return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        }

        private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
        {
            string content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception($"HTTP {(int)response.StatusCode}: {content}");
            return JsonDocument.Parse(content);
        }

        private static string GetString(JsonElement element, string name)
        {
            return element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.ToString()
                : "";
        }

        private static int GetInt(JsonElement element, string name)
        {
            if (!element.TryGetProperty(name, out var value))
                return 0;
            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
                return number;
            return int.TryParse(value.ToString(), out number) ? number : 0;
        }
    }

    public class SklandScanPendingException : Exception
    {
        public SklandScanPendingException(string message) : base(message)
        {
        }
    }
}
