using ArknightsLauncher.Services;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Storage.Pickers;
using WinRT;
using WinRT.Interop;

namespace ArknightsLauncher
{
    public sealed partial class SetupWizardWindow : Window
    {
        private int _currentStep = 1;
        private const int TotalSteps = 2;

        // 步骤标题
        private static readonly string[] StepTitles = { "个性化", "游戏设置" };

        // 进度条宽度（窗口内容区约516px，1/2 = 258，2/2 = 516）
        private static readonly double[] ProgressWidths = { 258, 516 };

        public SetupWizardWindow()
        {
            this.InitializeComponent();
            this.Title = "Arknights Launcher - 初始设置";
            this.ExtendsContentIntoTitleBar = true;
            // 设置固定窗口大小
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(
                            Microsoft.UI.Win32Interop.GetWindowIdFromWindow(
                            WinRT.Interop.WindowNative.GetWindowHandle(this)));

            if (appWindow != null)
            {
                // 1. 设置窗口大小
                int width = 580;
                int height = 660;
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

            UpdateUI();
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (this.Content is FrameworkElement root)
            {
                if (ThemeLight.IsChecked == true)
                    root.RequestedTheme = ElementTheme.Light;
                else if (ThemeDark.IsChecked == true)
                    root.RequestedTheme = ElementTheme.Dark;
                else
                    root.RequestedTheme = ElementTheme.Default;
            }
        }

        private void MaterialCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (MaterialCombo.SelectedIndex)
            {
                case 0: // 无
                    this.SystemBackdrop = null;
                    break;
                case 1: // 亚克力
                    this.SystemBackdrop = new DesktopAcrylicBackdrop();
                    break;
                case 2: // 云母
                    this.SystemBackdrop = new MicaBackdrop()
                    {
                        Kind = MicaKind.Base
                    };
                    break;
                case 3: // 云母Alt
                    this.SystemBackdrop = new MicaBackdrop()
                    {
                        Kind = MicaKind.BaseAlt
                    };
                    break;
            }
        }

        // ──────────────────────────────────────────────
        //  步骤导航
        // ──────────────────────────────────────────────

        private void UpdateUI()
        {
            // 更新步骤指示器
            StepLabel.Text = $"步骤 {_currentStep}/{TotalSteps}";
            StepTitle.Text = StepTitles[_currentStep - 1];
            StepProgressBar.Width = ProgressWidths[_currentStep - 1];

            // 显示对应页面
            Page1.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
            Page2.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;

            // 按钮状态
            PrevBtn.IsEnabled = _currentStep > 1;
            NextBtn.Content = _currentStep == TotalSteps ? "完成" : "下一步";
            if (NextBtn.Content.ToString() == "完成")
            {
                
            }
            else
            {
                NextBtn.ClearValue(Button.BackgroundProperty);
                NextBtn.ClearValue(Button.ForegroundProperty);
            }
        }

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep > 1)
            {
                _currentStep--;
                UpdateUI();
            }
        }

        private async void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStep < TotalSteps)
            {
                _currentStep++;
                UpdateUI();
            }
            else
            {
                // 校验游戏路径
                string gamePath = GamePathText.Text;
                if (string.IsNullOrEmpty(gamePath) || gamePath == "未选择" || !Directory.Exists(gamePath))
                {
                    var dlg = new ContentDialog
                    {
                        Title = "请选择游戏目录",
                        Content = "请先选择 'Arknights Game' 文件夹后再继续。",
                        PrimaryButtonText = "去选择",
                        CloseButtonText = "取消",
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dlg.ShowAsync();
                    if (result == ContentDialogResult.Primary)
                    {
                        // 直接触发浏览逻辑
                        BrowseGameBtn_Click(sender, e);
                    }
                    return; // 阻止完成
                }

                // 校验通过，保存并跳转
                await SaveSetupConfig();
                OpenMainWindow();
            }
        }

        // ──────────────────────────────────────────────
        //  路径选择
        // ──────────────────────────────────────────────

        private async void BrowseGameBtn_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");
                InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null) return; // 用户取消

                string folderName = Path.GetFileName(folder.Path.TrimEnd('\\', '/'));
                if (folderName.Equals("Arknights Game", StringComparison.OrdinalIgnoreCase))
                {
                    GamePathText.Text = folder.Path;
                    GamePathText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Microsoft.UI.Colors.White);
                    return;
                }
                var dlg = new ContentDialog
                {
                    Title = "路径错误",
                    Content = "请选择名为 'Arknights Game' 的文件夹作为根目录。",
                    CloseButtonText = "重新选择",
                    XamlRoot = this.Content.XamlRoot
                };
                await dlg.ShowAsync();
            }
        }

        private async void BrowseMAABtn_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));

            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Name.Equals("MAA.exe", StringComparison.OrdinalIgnoreCase))
            {
                MAAPathText.Text = file.Path;
                MAAPathText.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                    Microsoft.UI.Colors.White);
            }
            else if (file != null)
            {
                var dlg = new ContentDialog
                {
                    Title = "提示",
                    Content = "请选择正确的 MAA.exe 文件",
                    CloseButtonText = "确定",
                    XamlRoot = this.Content.XamlRoot
                };
                await dlg.ShowAsync();
            }
        }

        // ──────────────────────────────────────────────
        //  保存配置 & 跳转主窗口
        // ──────────────────────────────────────────────

        private async Task SaveSetupConfig()
        {
            var cfg = ConfigService.Load();

            // 游戏路径
            string gamePath = GamePathText.Text;
            if (!string.IsNullOrEmpty(gamePath) && gamePath != "未选择" && Directory.Exists(gamePath))
                cfg.RootPath = gamePath;

            // MAA路径
            string maaPath = MAAPathText.Text;
            if (!string.IsNullOrEmpty(maaPath) && File.Exists(maaPath))
                cfg.MAA_Official = maaPath;

            if (ThemeLight.IsChecked == true) cfg.Theme = "Light";
            else if (ThemeDark.IsChecked == true) cfg.Theme = "Dark";
            else cfg.Theme = "Default";

            // 材质
            cfg.Material = MaterialCombo.SelectedIndex switch
            {
                0 => "None",
                1 => "Acrylic",
                2 => "Mica",
                3 => "MicaAlt",
                _ => "Acrylic"
            };

            // 默认账号
            if (cfg.Accounts.Count == 0)
            {
                cfg.Accounts["A1"] = "默认账号";
                cfg.DefaultAccount = "A1";
                Directory.CreateDirectory(Path.Combine(ConfigService.AccountBackupDir, "A1"));
            }

            // 标记向导已完成
            cfg.SetupCompleted = true;

            ConfigService.Save(cfg);
        }

        private void OpenMainWindow()
        {
            var mainWindow = new MainWindow();
            mainWindow.Activate();
            this.Close();
        }
    }
}
