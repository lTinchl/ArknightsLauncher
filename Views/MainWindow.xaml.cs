using ArknightsLauncher.Models;
using ArknightsLauncher.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ArknightsLauncher
{
    // 链接条目数据类（供 XAML DataTemplate 绑定）
    public class LinkItem
    {
        public string Title { get; set; } = "";
        public string Glyph { get; set; } = "";
        public string Url { get; set; } = "";
    }

    // 账号条目数据类
    public class AccountItem
    {
        public string Id { get; set; } = "";
        public string Remark { get; set; } = "";
        public override string ToString() => Remark;
    }

    public sealed partial class MainWindow : Window
    {
        private ObservableCollection<AccountItem> _accounts = new();

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Arknights Launcher";
            this.ExtendsContentIntoTitleBar = true;

            var cfg = ConfigService.Load();
            ApplyThemeAndMaterial(cfg.Theme, cfg.Material);

            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                            WinRT.Interop.WindowNative.GetWindowHandle(this)));

            if (appWindow != null)
            {
                // 1. 设置窗口大小
                int width = 860;
                int height = 640;
                appWindow.Resize(new Windows.Graphics.SizeInt32(width, height));

                // 2. 获取当前屏幕分辨率
                var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(
                    appWindow.Id,
                    Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);

                int screenWidth = displayArea.WorkArea.Width;
                int screenHeight = displayArea.WorkArea.Height;

                // 3. 计算居中坐标并移动
                int x = (screenWidth - width) / 2;
                int y = (screenHeight - height) / 2;
                appWindow.Move(new Windows.Graphics.PointInt32(x, y));
            }



            // 绑定账号下拉
            AccountCombo.ItemsSource = _accounts;
            AccountCombo.DisplayMemberPath = "Remark";

            // 绑定链接列表
            LinksRepeater.ItemsSource = new List<LinkItem>
            {
                new() { Title = "PRTS Wiki",      Glyph = "\uE8A5", Url = "https://prts.wiki/" },
                new() { Title = "方舟工具箱",      Glyph = "\uE90F", Url = "https://arkntools.app/" },
                new() { Title = "方舟一图流",      Glyph = "\uE8C4", Url = "https://ark.yituliu.cn/" },
                new() { Title = "GitHub",          Glyph = "\uE943", Url = "https://github.com/lTinchl/ArknightsLauncher" },
            };

            LoadAccounts();
            CheckUpdateOnStartup();
        }

        private void ApplyThemeAndMaterial(string theme, string material)
        {
            // 应用材质
            this.SystemBackdrop = material switch
            {
                "None" => null,
                "Acrylic" => new DesktopAcrylicBackdrop(),
                "Mica" => new MicaBackdrop { Kind = MicaKind.Base },
                "MicaAlt" => new MicaBackdrop { Kind = MicaKind.BaseAlt },
                _ => new DesktopAcrylicBackdrop()
            };

            // 应用主题（需要在内容加载后设置）
            if (this.Content is FrameworkElement root)
            {
                root.RequestedTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };
            }
        }


        // ──────────────────────────────────────────────
        //  账号加载
        // ──────────────────────────────────────────────
        private void LoadAccounts()
        {
            _accounts.Clear();
            var cfg = ConfigService.Load();

            // 确保至少有一个账号
            if (cfg.Accounts.Count == 0)
            {
                cfg.Accounts["A1"] = "默认账号";
                cfg.DefaultAccount = "A1";
                Directory.CreateDirectory(Path.Combine(ConfigService.AccountBackupDir, "A1"));
                ConfigService.Save(cfg);
            }

            // 默认账号排最前
            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
                _accounts.Add(new AccountItem { Id = cfg.DefaultAccount, Remark = cfg.Accounts[cfg.DefaultAccount] + " ⭐" });

            foreach (var kv in cfg.Accounts)
            {
                if (kv.Key == cfg.DefaultAccount) continue;
                _accounts.Add(new AccountItem { Id = kv.Key, Remark = kv.Value });
            }

            if (_accounts.Count > 0) AccountCombo.SelectedIndex = 0;

            // B服：首次运行时禁用
            BServerBtn.IsEnabled = !cfg.IsFirstRun;
        }

        private void AccountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

        // ──────────────────────────────────────────────
        //  启动逻辑
        // ──────────────────────────────────────────────

        private async void OfficialBtn_Click(object sender, RoutedEventArgs e)
        {
            await LaunchGameAsync(ServerType.Official);
        }

        private async void BServerBtn_Click(object sender, RoutedEventArgs e)
        {
            await LaunchGameAsync(ServerType.Bilibili);
        }

        private async void MAAOfficialBtn_Click(object sender, RoutedEventArgs e)
        {
            await LaunchMAAAsync(ServerType.MAA_Official);
        }

        private async void MAABServerBtn_Click(object sender, RoutedEventArgs e)
        {
            await LaunchMAAAsync(ServerType.MAA_Bilibili);
        }

        private async Task LaunchGameAsync(ServerType type)
        {
            ShowLaunch("正在准备启动…");
            try
            {
                var cfg = ConfigService.Load();

                // 首次点官服 → 解锁B服
                if (type == ServerType.Official && cfg.IsFirstRun)
                {
                    cfg.IsFirstRun = false;
                    ConfigService.Save(cfg);
                    BServerBtn.IsEnabled = true;
                }

                // 获取 / 选择根目录
                string rootPath = cfg.RootPath;
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    rootPath = await PickFolderAsync("Arknights Game");
                    if (string.IsNullOrEmpty(rootPath)) { HideLaunch(); return; }
                    cfg.RootPath = rootPath;
                    ConfigService.Save(cfg);
                }

                // 关闭已有实例
                GameService.KillArknights();

                // 首次B服 → 解压基础文件
                if (type == ServerType.Bilibili && !cfg.IsGameExtracted)
                {
                    SetLaunchStatus("首次运行B服，修复基础文件…");
                    await Task.Run(() => GameService.ExtractAndOverwrite(rootPath, "ArknightsGame.zip"));
                    cfg.IsGameExtracted = true;
                    ConfigService.Save(cfg);
                }

                // 解压 Payload
                string zipName = type == ServerType.Official ? "Payload.zip" : "Payload_B.zip";
                await Task.Run(() => GameService.ExtractAndOverwrite(rootPath, zipName));

                // 官服：切换账号
                if (type == ServerType.Official)
                {
                    var selected = AccountCombo.SelectedItem as AccountItem;
                    if (selected != null)
                    {
                        string sdkPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                            "AppData", "LocalLow", "Hypergryph", "Arknights");

                        if (!Directory.Exists(sdkPath))
                        {
                            await ShowDialogAsync("提示", "未找到 Arknights 数据目录，请先通过鹰角启动器启动一次游戏。");
                        }
                        else
                        {
                            string sdkDir = GameService.GetSdkDir();
                            if (string.IsNullOrEmpty(sdkDir))
                                await ShowDialogAsync("提示", "未找到 sdk_data_* 文件夹，请先通过鹰角启动器进入账号输入界面。");
                            else
                            {
                                await Task.Delay(3000);
                                GameService.RestoreAccount(selected.Id, sdkDir);
                            }
                        }
                    }
                }

                // 启动游戏
                SetLaunchStatus("正在启动 Arknights…");
                GameService.StartArknights(rootPath);
                await Task.Delay(2500);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("启动失败", ex.Message);
            }
            finally
            {
                HideLaunch();
            }
        }

        private async Task LaunchMAAAsync(ServerType type)
        {
            ShowLaunch("正在启动 MAA…");
            try
            {
                var cfg = ConfigService.Load();
                string exePath = type == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    exePath = await PickFileAsync("请选择 MAA.exe");
                    if (string.IsNullOrEmpty(exePath)) { HideLaunch(); return; }

                    if (type == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                    else cfg.MAA_Bilibili = exePath;
                    ConfigService.Save(cfg);
                }

                GameService.StartMAA(exePath);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("启动失败", ex.Message);
            }
            finally
            {
                HideLaunch();
            }
        }

        // ──────────────────────────────────────────────
        //  修复客户端
        // ──────────────────────────────────────────────

        private async void FixBtn_Click(object sender, RoutedEventArgs e)
        {
            var cfg = ConfigService.Load();
            if (string.IsNullOrEmpty(cfg.RootPath) || !Directory.Exists(cfg.RootPath))
            {
                await ShowDialogAsync("错误", "未找到已配置的 Arknights Game 目录，请先启动一次游戏完成配置。");
                return;
            }

            ShowLaunch("正在修复客户端文件…");
            try
            {
                GameService.KillArknights();
                await Task.Run(() => GameService.ExtractAndOverwrite(cfg.RootPath, "ArknightsGame.zip"));
                await ShowDialogAsync("修复完成", "客户端文件已成功修复！");
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("修复失败", ex.Message);
            }
            finally
            {
                HideLaunch();
            }
        }

        // ──────────────────────────────────────────────
        //  账号管理
        // ──────────────────────────────────────────────

        private async void AccountMgrBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AccountManagerDialog();
            dlg.XamlRoot = this.Content.XamlRoot;
            await dlg.ShowAsync();
            LoadAccounts(); // 刷新下拉
        }

        // ──────────────────────────────────────────────
        //  设置
        // ──────────────────────────────────────────────
        private async void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsDialog();
            dlg.XamlRoot = this.Content.XamlRoot;
            await dlg.ShowAsync();

            // 关闭后重新应用设置
            var cfg = ConfigService.Load();
            ApplyThemeAndMaterial(cfg.Theme, cfg.Material);
        }

        // ──────────────────────────────────────────────
        //  关于
        // ──────────────────────────────────────────────

        private async void AboutBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new AboutDialog();
            dlg.XamlRoot = this.Content.XamlRoot;
            await dlg.ShowAsync();
        }

        // ──────────────────────────────────────────────
        //  外部链接
        // ──────────────────────────────────────────────

        private void LinkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                }
                catch { }
            }
        }

        // ──────────────────────────────────────────────
        //  自动更新检查
        // ──────────────────────────────────────────────

        private async void CheckUpdateOnStartup()
        {
            try
            {
                var (hasUpdate, latestVersion, _) = await UpdateService.CheckAsync();

                if (hasUpdate)
                {
                    UpdateBadgeText.Text = $"检测到新版本 v{latestVersion}";
                    UpdateBadge.Visibility = Visibility.Visible;

                    var dlg = new AboutDialog();

                    dlg.XamlRoot = (this.Content as FrameworkElement)?.XamlRoot
                                   ?? this.Content.XamlRoot;

                    await dlg.ShowAsync();
                }
            }
            catch { }
        }

        // ──────────────────────────────────────────────
        //  文件夹 / 文件选择（WinUI3 方式）
        // ──────────────────────────────────────────────

        private async Task<string> PickFolderAsync(string mustBeNamed)
        {
            while (true)
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null) return "";

                string folderName = System.IO.Path.GetFileName(folder.Path.TrimEnd('\\', '/'));
                if (folderName.Equals(mustBeNamed, StringComparison.OrdinalIgnoreCase))
                    return folder.Path;

                await ShowDialogAsync("路径错误", $"请选择名为 '{mustBeNamed}' 的文件夹作为根目录。");
            }
        }


        private async Task<string> PickFileAsync(string title)
        {
            while (true)
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".exe");
                WinRT.Interop.InitializeWithWindow.Initialize(
                    picker,
                    WinRT.Interop.WindowNative.GetWindowHandle(this)
                );

                var file = await picker.PickSingleFileAsync();

                // 用户取消
                if (file == null)
                    return "";

                // 判断是否 MAA.exe
                if (file.Name.Equals("MAA.exe", StringComparison.OrdinalIgnoreCase))
                {
                    return file.Path;
                }

                // 选错提示
                var dialog = new ContentDialog
                {
                    Title = "错误",
                    Content = "请选择正确的 MAA.exe 文件",
                    CloseButtonText = "重新选择",
                    XamlRoot = this.Content.XamlRoot
                };

                await dialog.ShowAsync();
            }
        }

        // ──────────────────────────────────────────────
        //  UI 辅助
        // ──────────────────────────────────────────────

        private void ShowLaunch(string message)
        {
            LaunchStatusText.Text = message;
            LaunchOverlay.Visibility = Visibility.Visible;
        }

        private void SetLaunchStatus(string message)
        {
            LaunchStatusText.Text = message;
        }

        private void HideLaunch()
        {
            LaunchOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task ShowDialogAsync(string title, string message)
        {
            var dlg = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "确定",
                XamlRoot = this.Content.XamlRoot
            };
            await dlg.ShowAsync();
        }
    }
}
