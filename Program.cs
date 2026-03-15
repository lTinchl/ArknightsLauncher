using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;

enum ServerType
{
    Official, // 官服
    Bilibili,  // B服
    MAA_Official,  // MAA 官
    MAA_Bilibili,  // MAA B
    GitHub         //Github 
    
}



class Program
{
    public static readonly Version CurrentVersion = new Version("1.3.5.4");

    const string GitHubUrl = "https://github.com/lTinchl/ArknightsLauncher";
    const string ArknightsYituliuUrl = "https://ark.yituliu.cn/";
    const string ArknightsToolboxUrl = "https://arkntools.app/";
    const string PrtsWikiUrl = "https://prts.wiki/";

    static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "ArknightsLauncher");

    static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    static readonly string AccountBackupDir = Path.Combine(ConfigDir, "AccountBackups");

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Directory.CreateDirectory(AccountBackupDir);
        using var selectForm = new ServerSelectForm();
        selectForm.ShowDialog();
    }

    public static async Task<(bool hasUpdate, string latestVersion, string downloadUrl)> CheckForUpdateAsync(bool useChina = false)
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        string tagName, downloadUrl;

        if (useChina)
        {
            var handler = new HttpClientHandler();
            using var chinaClient = new HttpClient(handler);
            chinaClient.Timeout = TimeSpan.FromSeconds(10);
            chinaClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            string chinaApi = "http://47.107.30.27:8080/launcher/version.json";
            var response = await chinaClient.GetStringAsync(chinaApi);
            var json = System.Text.Json.JsonDocument.Parse(response);
            tagName = json.RootElement.GetProperty("version").GetString();
            downloadUrl = json.RootElement.GetProperty("download_url").GetString();
        }
        else
        {
            string apiUrl = "https://api.github.com/repos/lTinchl/ArknightsLauncher/releases/latest";
            var response = await client.GetStringAsync(apiUrl);
            var json = System.Text.Json.JsonDocument.Parse(response);
            tagName = json.RootElement.GetProperty("tag_name").GetString();
            downloadUrl = json.RootElement
                .GetProperty("assets")[0]
                .GetProperty("browser_download_url")
                .GetString();
        }

        string latestVersionStr = tagName.TrimStart('v').TrimStart('V');
        var latestVersion = new Version(latestVersionStr);
        return (latestVersion > CurrentVersion, latestVersionStr, downloadUrl);
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

    public static Icon LoadIcon(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{name}");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    public static string SelectFolder()
    {
        while (true)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "请选择 Arknights 根目录",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return ""; // 用户取消

            string selectedPath = dialog.SelectedPath;
            string folderName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar));

            if (folderName.Equals("Arknights Game", StringComparison.OrdinalIgnoreCase))
            {
                return selectedPath; // 选择正确，返回路径
            }
            else
            {
                MessageBox.Show("请选择名为 'Arknights Game' 的文件夹作为根目录", "错误");
                // 循环再次弹出选择框
            }
        }
    }

    public static string SelectExe(string title, string filterName)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "MAA.exe|MAA.exe",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == DialogResult.OK
            ? dialog.FileName
            : "";
    }

    public static AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigFile)) return new AppConfig();
        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile))
                   ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void SaveConfig(AppConfig cfg)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile,
            JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }


    public static void ExtractAndOverwrite(string targetRoot, string zipResourceName)
    {
        var asm = Assembly.GetExecutingAssembly();

        // 临时保存路径
        string tempZipPath = Path.Combine(Path.GetTempPath(), $"temp_{zipResourceName}");

        try
        {
            // 从嵌入资源中提取压缩包到临时文件
            using (Stream resourceStream = asm.GetManifestResourceStream($"ArknightsLauncher.{zipResourceName}"))
            {
                if (resourceStream == null)
                {
                    throw new Exception($"未找到嵌入的资源: {zipResourceName}");
                }

                using (FileStream fileStream = new FileStream(tempZipPath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                }
            }
            int maxRetry = 5;
            for (int i = 0; i < maxRetry; i++)
            {
                try
                {
                    ZipFile.ExtractToDirectory(tempZipPath, targetRoot, true);
                    break; // 成功则跳出
                }
                catch (IOException) when (i < maxRetry - 1)
                {
                    System.Threading.Thread.Sleep(1000); // 等 1 秒后重试
                }
            }
        }
        finally
        {
            // 清理临时文件
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
        }
    }


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

    public static void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
        }
    }

    public static void OpenArknightsYituliu()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ArknightsYituliuUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
        }
    }

    public static void OpenArknightsToolbox()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = ArknightsToolboxUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
        }
    }

    public static void OpenPrtsWiki()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = PrtsWikiUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
        }
    }

    public static async Task BackupAccount(string accountName)
    {
        // 获取 LocalLow 路径
        string sdkPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Hypergryph", "Arknights"
        );

        string target = Path.Combine(AccountBackupDir, accountName);

        var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
        if (sdkDir == null) return;

        if (Directory.Exists(target)) Directory.Delete(target, true);
        await CopyDirectory(sdkDir, target);
    }

    private static async Task CopyDirectory(string sourceDir, string targetDir, int maxRetries = 5)
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


    public class AppConfig
    {
        public string RootPath { get; set; } = "";     // Arknights
        public string MAA_Official { get; set; } = ""; //MAA 官
        public string MAA_Bilibili { get; set; } = ""; //MAA B

        public Dictionary<string, string> Accounts { get; set; }
        = new Dictionary<string, string>();

        public string DefaultAccount { get; set; } = "";                    // 默认账号 ID（如 "A1"）
        public bool IsFirstRun { get; set; } = true;                        // 是否首次运行（用于控制首次点击官服时解锁 B 服按钮）
        public string LastNotifiedVersion { get; set; } = "";               
    }

    class UpdateNotifyDialog : Form
    {
        public bool NeverRemind => _chkNever.Checked;

        private readonly CheckBox _chkNever;

        public UpdateNotifyDialog(string latestVersion)
        {
            Text = "发现新版本";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(340, 155);

            var lbl = new Label
            {
                Text = $"发现新版本 v{latestVersion}，\n请在关于页面点击 [检查更新] 下载。",
                AutoSize = false,
                Size = new Size(300, 50),
                Location = new Point(20, 18),
                Font = new Font("Segoe UI", 10)
            };
            Controls.Add(lbl);

            _chkNever = new CheckBox
            {
                Text = "不再提醒",
                AutoSize = true,
                Location = new Point(22, 75),
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(_chkNever);

            var btnOK = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point(130, 108),
                DialogResult = DialogResult.OK
            };
            Controls.Add(btnOK);
            AcceptButton = btnOK;
        }
    }

    class ImageMessageBox : Form
    {
        public ImageMessageBox(string message, Image image, string title = "提示")
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int padding = 20;
            int btnHeight = 40;

            // 压缩图片到固定尺寸
            int targetWidth = 500;   // 压缩宽度
            int targetHeight = 300;  // 压缩高度
            Image resizedImage = ResizeImage(image, targetWidth, targetHeight);

            // 设置窗体尺寸
            ClientSize = new Size(Math.Max(resizedImage.Width + padding * 2, 300),
                                  resizedImage.Height + 70 + btnHeight);

            // 图片控件
            var picBox = new PictureBox
            {
                Image = resizedImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(resizedImage.Width, resizedImage.Height),
                Location = new Point((ClientSize.Width - resizedImage.Width) / 2, padding)
            };
            Controls.Add(picBox);

            // 文本控件
            var lbl = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                Size = new Size(ClientSize.Width - padding * 2, 50),
                Location = new Point(padding, picBox.Bottom + 10)
            };
            Controls.Add(lbl);

            // 确定按钮
            var btn = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point((ClientSize.Width - 80) / 2, lbl.Bottom + 1),
                DialogResult = DialogResult.OK
            };
            Controls.Add(btn);

            AcceptButton = btn;
        }

        // 缩放图片方法
        private static Image ResizeImage(Image img, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using var wrapMode = new System.Drawing.Imaging.ImageAttributes();
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }

        // 调用示例
        public static void Show(string message, string resourceName, string title = "提示")
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{resourceName}");
            if (stream == null)
            {
                MessageBox.Show("未找到资源: " + resourceName);
                return;
            }

            Image img = Image.FromStream(stream);
            using var box = new ImageMessageBox(message, img, title);
            box.ShowDialog();
        }
    }

    class LaunchForm : Form
    {
        private readonly ServerType _serverType;
        private readonly string _rootPath;
        private Label statusLabel;

        public LaunchForm(ServerType serverType, string rootPath = "")
        {
            _serverType = serverType;
            _rootPath = rootPath;

            Text = serverType switch
            {
                ServerType.Official => "Arknights Launcher(官服)",
                ServerType.Bilibili => "Arknights Launcher(B服)",
                ServerType.MAA_Official => "MAA(官服)",
                ServerType.MAA_Bilibili => "MAA(B服)",
                _ => "Launcher"
            };

            Icon = serverType switch
            {
                ServerType.Official => Program.LoadIcon("official.ico"),
                ServerType.Bilibili => Program.LoadIcon("bserver.ico"),
                ServerType.MAA_Official => Program.LoadIcon("MAA.ico"),
                ServerType.MAA_Bilibili => Program.LoadIcon("MAA.ico"),
                _ => SystemIcons.Application
            };

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(320, 140);
            TopMost = true;

            statusLabel = new Label
            {
                Text = serverType == ServerType.MAA_Official || serverType == ServerType.MAA_Bilibili
                       ? "正在启动 MAA…"
                       : "正在启动 Arknights…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11)
            };

            Controls.Add(statusLabel);
        }
        public async Task RunCore()
        {
            try
            {
                var cfg = Program.LoadConfig();

                if (_serverType == ServerType.MAA_Official || _serverType == ServerType.MAA_Bilibili)
                {
                    string exePath = _serverType == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {

                        exePath = (string)this.Invoke((Func<string>)(() =>
                            Program.SelectExe("请选择 MAA.exe", "MAA 程序")));

                        if (string.IsNullOrEmpty(exePath)) return;

                        if (_serverType == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                        else cfg.MAA_Bilibili = exePath;
                        Program.SaveConfig(cfg);
                    }

                    Program.StartMAA(exePath);
                    await Task.Delay(1000);
                }
                else
                {
                    string rootPath = _rootPath;

                    if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    {

                        rootPath = (string)this.Invoke((Func<string>)(() =>
                            Program.SelectFolder()));

                        if (string.IsNullOrEmpty(rootPath)) return;

                        cfg.RootPath = rootPath;
                        Program.SaveConfig(cfg);
                    }

                    string zipResourceName = _serverType == ServerType.Official ? "Payload.zip" : "Payload_B.zip";
                    await Task.Run(() => Program.ExtractAndOverwrite(rootPath, zipResourceName));

                    Program.StartArknights(rootPath);
                    await Task.Delay(2500);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "启动失败");
            }
            finally
            {
                this.Invoke(() => this.Close());
            }
        }
    }

    class AccountItem
    {
        public string Id { get; set; }
        public string Remark { get; set; }
        public override string ToString() => Remark;
    }


    class ServerSelectForm : Form
    {
        private ComboBox officialCombo;
        public ServerType SelectedServer { get; private set; }

        private void ReloadAccounts()
        {
            officialCombo.Items.Clear();
            var cfg = Program.LoadConfig();

            if (cfg.Accounts.Count == 0)
            {
                string defaultId = "A1";
                string defaultRemark = "默认账号";
                cfg.Accounts[defaultId] = defaultRemark;
                cfg.DefaultAccount = defaultId;

                // 创建备份文件夹
                Directory.CreateDirectory(Path.Combine(Program.AccountBackupDir, defaultId));

                Program.SaveConfig(cfg);
            }

            // 默认账号优先
            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
            {
                var defaultItem = new AccountItem
                {
                    Id = cfg.DefaultAccount,
                    Remark = cfg.Accounts[cfg.DefaultAccount] + "⭐"
                };
                officialCombo.Items.Add(defaultItem);
            }

            // 添加剩余账号
            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue;
                officialCombo.Items.Add(new AccountItem
                {
                    Id = acc.Key,
                    Remark = acc.Value
                });
            }

            // 默认选中第一个
            if (officialCombo.Items.Count > 0)
                officialCombo.SelectedIndex = 0;
        }

        public ServerSelectForm()
        {
            Text = "Arknights Launcher";
            Icon = Program.LoadIcon("ArknightsLauncher.ico");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(395, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            officialCombo = new ComboBox
            {
                Location = new Point(125, 36),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var cfg = Program.LoadConfig();

            ReloadAccounts();

            // 设置默认账号
            if (!string.IsNullOrEmpty(cfg.DefaultAccount))
                officialCombo.Text = cfg.DefaultAccount;
            Controls.Add(officialCombo);

            var manageBtn = new Button
            {
                Text = "账号管理",
                Size = new Size(120, 30),
                Location = new Point(5, 5),
                Cursor = Cursors.Hand
            };

            manageBtn.Click += (_, __) =>
            {
                using var f = new AccountManagerForm();
                f.ShowDialog();

                ReloadAccounts();
            };

            Controls.Add(manageBtn);

            Button bServerBtn = null;
            // 创建 B服按钮
            var officialBtn = CreateServerButton("官服", Program.LoadIcon("official.ico"), new Point(5, 35), ServerType.Official, () => bServerBtn);
            Controls.Add(officialBtn);

            // 官服按钮
            bServerBtn = CreateServerButton("B服", Program.LoadIcon("bserver.ico"), new Point(5, 95), ServerType.Bilibili, null);
            bServerBtn.Enabled = !cfg.IsFirstRun; // 如果是首次运行，B服按钮禁用
            Controls.Add(bServerBtn);

            Controls.Add(CreateServerButton_MAA("MAA-官", Program.LoadIcon("MAA.ico"), new Point(270, 35), ServerType.MAA_Official)); // MAA-官
            Controls.Add(CreateServerButton_MAA("MAA-B", Program.LoadIcon("MAA.ico"), new Point(270, 95), ServerType.MAA_Bilibili)); //  MAA-B
            Controls.Add(CreateServerButton_PrtsWiki("PRTS Wiki", Program.LoadIcon("PRTS_WIKI.ico"), new Point(5, 155))); // PRTS Wiki
            Controls.Add(CreateServerButton_ArknightsToolbox("方舟工具箱", Program.LoadIcon("Arknights_Toolbox.ico"), new Point(85, 155))); // 明日方舟工具箱
            Controls.Add(CreateServerButton_ArknightsYituliu("方舟一图流", Program.LoadIcon("Arknights_Yituliu.ico"), new Point(200, 155))); // 明日方舟一图流
            Controls.Add(CreateServerButton_About("关于", Program.LoadIcon("Info.ico"), new Point(310, 155))); // Github

            //设定按钮
            var settingsBtn = new Button
            {
                Text = "设置",
                Image = ResizeBitmap(Program.LoadIcon("Setting.ico").ToBitmap(), 20, 20),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Size = new Size(80, 30),
                Location = new Point(310, 5),
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, -5, 0, 0),
                Cursor = Cursors.Hand
            };
            settingsBtn.Click += (_, __) =>
            {
                using var f = new SettingsForm();
                f.ShowDialog();
            };
            Controls.Add(settingsBtn);


            // ── 启动时检查更新（若用户未选择"不再提醒"）──────────────────────
            this.Shown += async (_, __) =>
            {
                try
                {
                    var config = Program.LoadConfig();
                    var (hasUpdate, latestVersion, _) = await Program.CheckForUpdateAsync();

                    if (hasUpdate)
                    {
                        if (config.LastNotifiedVersion != latestVersion)
                        {
                            // 第一次发现这个新版本，弹提示
                            MessageBox.Show($"发现新版本 v{latestVersion}，请在关于页面更新。", "发现新版本");
                            config.LastNotifiedVersion = latestVersion;
                            Program.SaveConfig(config);
                        }

                        // 只要有新版本，标题就加 [new]
                        this.Text = "Arknights Launcher [New↑]";
                    }
                }
                catch { }
            };
        }

        //对图片进行缩放以适应 MessageBox 的显示需求
        private Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, width, height);
            }
            return resized;
        }

        private Button CreateServerButton(string text, Icon icon, Point pos, ServerType type, Func<Button> getBServerBtn = null)
        {
            var btn = new Button
            {
                Text = text,
                Image = icon.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Size = new Size(120, 60),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btn.Click += async (_, __) =>
            {
                SelectedServer = type;
                if (type == ServerType.Official && getBServerBtn != null)
                {
                    var cfg = Program.LoadConfig();
                    if (cfg.IsFirstRun)
                    {
                        var bServerButton = getBServerBtn();
                        if (bServerButton != null)
                        {
                            bServerButton.Enabled = true;    // 解锁 B服按钮
                        }
                        cfg.IsFirstRun = false;       // 标记首次点击完成
                        Program.SaveConfig(cfg);      // 保存配置
                    }
                }

                try
                {
                    foreach (var proc in Process.GetProcessesByName("Arknights"))
                    {
                        proc.Kill();
                        proc.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("关闭 Arknights 时出错:\n" + ex.Message, "错误");
                }

                var cfg2 = Program.LoadConfig();
                string rootPath = cfg2.RootPath;

                // 如果没有配置或者目录不存在，就弹出选择文件夹
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    rootPath = Program.SelectFolder();
                    if (string.IsNullOrEmpty(rootPath)) return; // 用户取消

                    cfg2.RootPath = rootPath;
                    Program.SaveConfig(cfg2);
                }



                // 立即显示启动窗口
                var launchForm = new LaunchForm(type, rootPath);
                launchForm.Show();

                if (type == ServerType.Official)
                {
                    var selectedItem = officialCombo.SelectedItem as AccountItem;
                    if (selectedItem == null) return;

                    string selectedAccount = selectedItem.Id;
                    string backupFolder = Path.Combine(Program.AccountBackupDir, selectedAccount);

                    string sdkPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "AppData", "LocalLow", "Hypergryph", "Arknights"
                    );
                    if (!Directory.Exists(sdkPath))
                    {
                        _ = Task.Run(() =>
                        {
                            ImageMessageBox.Show("未找到 Arknights 数据目录，请先通过鹰角启动器启动一次游戏。", "Start.png", "提示");
                        });
                    }
                    else
                    {
                        var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
                        if (sdkDir == null)
                        {
                            _ = Task.Run(() =>
                            {
                                ImageMessageBox.Show("未找到,sdk_data_*文件夹，请通过鹰角启动器启动一次进入到账号输入界面(如所示)再手动关闭进程", "Main.png", "提示");
                            });
                        }
                        else
                        {
                            if (Directory.Exists(backupFolder))
                            {
                                await Task.Delay(3000);
                                await CopyDirectory(backupFolder, sdkDir);
                            }
                        }
                    }
                }

                // 异步执行启动逻辑
                _ = Task.Run(async () =>
                {
                    await Task.Delay(4000);
                    await launchForm.RunCore();
                    this.Invoke(() => this.Show());
                });
            };

            return btn;
        }

        private Button CreateServerButton_MAA(string text, Icon icon, Point pos, ServerType type)
        {
            var btn = new Button
            {
                Text = text,
                Image = icon.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Size = new Size(120, 60),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btn.Click += async (_, __) =>
            {
                SelectedServer = type;
                var cfg = Program.LoadConfig();

                // UI 线程选择 MAA.exe
                string exePath = type == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    string title = type == ServerType.MAA_Official ? "请选择 MAA.exe (MAA-官)" : "请选择 MAA.exe (MAA-B)";
                    exePath = Program.SelectExe(title, "MAA 程序");
                    if (string.IsNullOrEmpty(exePath)) return;

                    if (type == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                    else cfg.MAA_Bilibili = exePath;

                    Program.SaveConfig(cfg);
                }

                // 显示启动动画窗口
                var launchForm = new LaunchForm(type);
                launchForm.Show();

                // 异步执行启动逻辑
                _ = Task.Run(async () =>
                {
                    await launchForm.RunCore();
                    this.Invoke(() => this.Show());
                });
            };

            return btn;
        }

        private Button CreateServerButton_PrtsWiki(string text, Icon icon, Point pos)
        {
            var originalBmp = icon.ToBitmap();
            var btn = new Button
            {
                Text = text,
                Image = ResizeBitmap(originalBmp, 25, 25),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Size = new Size(80, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            originalBmp.Dispose();
            btn.Click += (_, __) =>
            {
                Program.OpenPrtsWiki();
            };
            return btn;
        }

        private Button CreateServerButton_ArknightsToolbox(string text, Icon icon, Point pos)
        {
            var originalBmp = icon.ToBitmap();
            var btn = new Button
            {
                Text = text,
                Image = ResizeBitmap(originalBmp, 25, 25),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Size = new Size(115, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            originalBmp.Dispose();
            btn.Click += (_, __) =>
            {
                Program.OpenArknightsToolbox();
            };
            return btn;
        }

        private Button CreateServerButton_ArknightsYituliu(string text, Icon icon, Point pos)
        {
            var originalBmp = icon.ToBitmap();
            var btn = new Button
            {
                Text = text,
                Image = ResizeBitmap(originalBmp, 20, 20),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Size = new Size(110, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            originalBmp.Dispose();
            btn.Click += (_, __) =>
            {
                Program.OpenArknightsYituliu();
            };
            return btn;
        }

        private Button CreateServerButton_About(string text, Icon icon, Point pos)
        {
            var originalBmp = icon.ToBitmap();
            var btn = new Button
            {
                Text = text,
                Image = ResizeBitmap(originalBmp, 25, 25),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                Size = new Size(80, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand,
            };
            originalBmp.Dispose();
            btn.Click += (_, __) =>
            {
                using (var aboutForm = new AboutForm())
                {
                    aboutForm.ShowDialog();
                }
            };
            return btn;
        }
    }
    class AccountManagerForm : Form
    {
        private ListBox listBox;
        private Button btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename;
        private Label hintLabel;

        public AccountManagerForm()
        {
            Text = "账号管理";
            Size = new Size(350, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            hintLabel = new Label
            {
                Text = "双击列表项可重命名",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            Controls.Add(hintLabel);

            listBox = new ListBox { Dock = DockStyle.Top, Height = 250 };
            listBox.DoubleClick += RenameAccount;
            Controls.Add(listBox);

            btnAdd = new Button { Text = "新增", Width = 70 };
            btnDelete = new Button { Text = "删除", Width = 70 };
            btnBackup = new Button { Text = "备份当前", Width = 90 };
            btnSetDefault = new Button { Text = "设为默认", Width = 90 };
            btnRename = new Button { Text = "重命名", Width = 90 };

            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60 };
            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename });

            Controls.Add(listBox);
            Controls.Add(panel);

            btnAdd.Click += AddAccount;
            btnDelete.Click += DeleteAccount;
            btnBackup.Click += BackupCurrent;
            btnSetDefault.Click += SetDefault;
            btnRename.Click += RenameAccount;

            LoadAccounts();
        }

        private void LoadAccounts()
        {
            listBox.Items.Clear();
            var cfg = Program.LoadConfig();

            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
            {
                var defaultItem = new AccountItem
                {
                    Id = cfg.DefaultAccount,
                    Remark = cfg.Accounts[cfg.DefaultAccount] + " ⭐"
                };
                listBox.Items.Add(defaultItem);
            }

            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue; // 已添加默认账号
                listBox.Items.Add(new AccountItem
                {
                    Id = acc.Key,
                    Remark = acc.Value
                });
            }
        }

        private void AddAccount(object sender, EventArgs e)
        {
            string remark = Microsoft.VisualBasic.Interaction.InputBox("请输入账号备注", "新增账号");
            if (string.IsNullOrWhiteSpace(remark)) return;

            var cfg = Program.LoadConfig();
            int index = 1;
            while (cfg.Accounts.ContainsKey("A" + index)) index++;
            string id = "A" + index;

            cfg.Accounts[id] = remark;
            Program.SaveConfig(cfg);

            Directory.CreateDirectory(Path.Combine(Program.AccountBackupDir, id));
            LoadAccounts();
        }

        private void RenameAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            string newRemark = Microsoft.VisualBasic.Interaction.InputBox($"修改 {selectedItem.Remark.Replace(" ⭐", "")}",
                                                                          "重命名",
                                                                          selectedItem.Remark.Replace(" ⭐", ""));
            if (string.IsNullOrWhiteSpace(newRemark)) return;

            var cfg = Program.LoadConfig();
            cfg.Accounts[selectedItem.Id] = newRemark;
            Program.SaveConfig(cfg);
            LoadAccounts();
        }

        private void DeleteAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;
            var cfg = Program.LoadConfig();

            if (cfg.Accounts.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个账号", "提示");
                return;
            }

            if (MessageBox.Show($"确认删除 {selectedItem.Remark.Replace(" ⭐", "")}？", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            cfg.Accounts.Remove(selectedItem.Id);
            if (cfg.DefaultAccount == selectedItem.Id) cfg.DefaultAccount = "";
            Program.SaveConfig(cfg);

            string backupDir = Path.Combine(Program.AccountBackupDir, selectedItem.Id);
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);

            LoadAccounts();
        }

        private void SetDefault(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            var cfg = Program.LoadConfig();
            cfg.DefaultAccount = selectedItem.Id;
            Program.SaveConfig(cfg);

            LoadAccounts();
        }

        private async void BackupCurrent(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            await Program.BackupAccount(selectedItem.Id);
            MessageBox.Show("备份完成", "成功");
        }
    }

    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "关于";
            this.Size = new Size(400, 340);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // 软件图标/Logo
            var picBox = new PictureBox
            {
                Image = ResizeBitmap(Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(), 64, 64),
                Size = new Size(64, 64),
                Location = new Point(160, 20),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            // 软件名
            var labelName = new Label
            {
                Text = "Arknights Launcher",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 100),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.None,
            };
            labelName.Left = (this.ClientSize.Width - labelName.PreferredWidth) / 2;

            // 版本号
            var labelVersion = new Label
            {
                Text = $"版本 v{Program.CurrentVersion}",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(0, 135),
            };
            labelVersion.Left = (this.ClientSize.Width - labelVersion.PreferredWidth) / 2;

            // 描述
            var labelDesc = new Label
            {
                Text = "By:Tinch",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, 160),
            };
            labelDesc.Left = (this.ClientSize.Width - labelDesc.PreferredWidth) / 2;

            // GitHub 链接
            var githubBmp = LoadIcon("GitHub.ico").ToBitmap();
            var linkGitHub = new Button
            {
                Text = "GitHub 主页",
                Image = ResizeBitmap(githubBmp, 25, 25),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.RoyalBlue,
                AutoSize = true,
                Location = new Point(128, 182),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Padding = new Padding(9, 0, 0, 0),
            };
            linkGitHub.FlatAppearance.BorderSize = 0;
            linkGitHub.FlatAppearance.MouseOverBackColor = Color.Transparent;
            linkGitHub.FlatAppearance.MouseDownBackColor = Color.Transparent;
            linkGitHub.Click += (_, __) => Program.OpenGitHub();

            var labelSource = new Label
            {
                Text = "更新源：",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, 225),
            };

            var comboSource = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Size = new Size(120, 25),
                Location = new Point(0, 220),
            };
            comboSource.Items.AddRange(new string[] { "GitHub", "国内服务器(低速)" });
            comboSource.SelectedIndex = 0;

            int groupWidth = labelSource.PreferredWidth + 4 + comboSource.Width;
            labelSource.Left = (this.ClientSize.Width - groupWidth) / 2;
            comboSource.Left = labelSource.Left + labelSource.PreferredWidth + 4;

            // 检查更新按钮
            var btnUpdate = new Button
            {
                Text = "检查更新",
                Size = new Size(100, 30),
                Location = new Point(0, 255),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand,
            };
            btnUpdate.Left = (this.ClientSize.Width - btnUpdate.Width) / 2;

            btnUpdate.Click += async (_, __) =>
            {
                btnUpdate.Enabled = false;
                btnUpdate.Text = "检查中...";
                bool useChina = comboSource.SelectedIndex == 1;
                try
                {
                    var (hasUpdate, latestVersion, downloadUrl) = await Program.CheckForUpdateAsync(useChina);
                    if (hasUpdate)
                    {
                        var result = MessageBox.Show(
                            $"发现新版本 v{latestVersion}，是否立即下载？",
                            "有新版本",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information
                        );
                        if (result == DialogResult.Yes)
                        {
                            await Program.DownloadAndInstallAsync(downloadUrl, btnUpdate, "检查更新");
                            return;
                        }
                    }
                    else
                    {
                        MessageBox.Show("当前已是最新版本！", "检查更新");
                    }
                }
                catch (TaskCanceledException)
                {
                    if (MessageBox.Show("连接超时，是否打开浏览器手动下载？", "检查更新失败",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        Program.OpenGitHub();
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"请求失败：{ex.Message}\n状态码：{ex.StatusCode}", "调试信息");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"检查更新失败：{ex.Message}", "错误");
                }
                finally
                {
                    btnUpdate.Text = "检查更新";
                    btnUpdate.Enabled = true;
                }
            };

            this.Controls.AddRange(new Control[]
            {
                picBox, labelName, labelVersion, labelDesc, linkGitHub,
                labelSource, comboSource, btnUpdate
            });
        }

        private Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using (var g = Graphics.FromImage(resized))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(bmp, 0, 0, width, height);
            }
            return resized;
        }
    }

    class SettingsForm : Form
    {
        public SettingsForm()
        {
            Text = "设置";
            Size = new Size(400, 180);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var cfg = Program.LoadConfig();

            // ── 根目录 ──────────────────────────────────────
            Controls.Add(new Label
            {
                Text = "根目录：",
                AutoSize = true,
                Location = new Point(15, 22),
                Font = new Font("Segoe UI", 8)
            });

            var txtRoot = new TextBox
            {
                Text = cfg.RootPath,
                ReadOnly = true,
                Size = new Size(220, 25),
                Location = new Point(80, 19),
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(txtRoot);

            var btnRoot = new Button
            {
                Text = "浏览...",
                Size = new Size(70, 25),
                Location = new Point(305, 19),
                Cursor = Cursors.Hand
            };
            btnRoot.Click += (_, __) =>
            {
                string newPath = Program.SelectFolder();
                if (string.IsNullOrEmpty(newPath)) return;
                txtRoot.Text = newPath;
                var c = Program.LoadConfig();
                c.RootPath = newPath;
                Program.SaveConfig(c);
            };
            Controls.Add(btnRoot);

            // ── MAA 官服 ─────────────────────────────────────
            Controls.Add(new Label
            {
                Text = "MAA-官：",
                AutoSize = true,
                Location = new Point(15, 62),
                Font = new Font("Segoe UI", 9)
            });

            var txtMaaOfficial = new TextBox
            {
                Text = cfg.MAA_Official,
                ReadOnly = true,
                Size = new Size(220, 25),
                Location = new Point(80, 59),
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(txtMaaOfficial);

            var btnMaaOfficial = new Button
            {
                Text = "浏览...",
                Size = new Size(70, 25),
                Location = new Point(305, 59),
                Cursor = Cursors.Hand
            };
            btnMaaOfficial.Click += (_, __) =>
            {
                string newPath = Program.SelectExe("请选择 MAA.exe (MAA-官)", "MAA 程序");
                if (string.IsNullOrEmpty(newPath)) return;
                txtMaaOfficial.Text = newPath;
                var c = Program.LoadConfig();
                c.MAA_Official = newPath;
                Program.SaveConfig(c);
            };
            Controls.Add(btnMaaOfficial);

            // ── MAA B服 ──────────────────────────────────────
            Controls.Add(new Label
            {
                Text = "MAA-B：",
                AutoSize = true,
                Location = new Point(15, 102),
                Font = new Font("Segoe UI", 9)
            });

            var txtMaaBilibili = new TextBox
            {
                Text = cfg.MAA_Bilibili,
                ReadOnly = true,
                Size = new Size(220, 25),
                Location = new Point(80, 99),
                Font = new Font("Segoe UI", 9)
            };
            Controls.Add(txtMaaBilibili);

            var btnMaaBilibili = new Button
            {
                Text = "浏览...",
                Size = new Size(70, 25),
                Location = new Point(305, 99),
                Cursor = Cursors.Hand
            };
            btnMaaBilibili.Click += (_, __) =>
            {
                string newPath = Program.SelectExe("请选择 MAA.exe (MAA-B)", "MAA 程序");
                if (string.IsNullOrEmpty(newPath)) return;
                txtMaaBilibili.Text = newPath;
                var c = Program.LoadConfig();
                c.MAA_Bilibili = newPath;
                Program.SaveConfig(c);
            };
            Controls.Add(btnMaaBilibili);
        }
    }
}
