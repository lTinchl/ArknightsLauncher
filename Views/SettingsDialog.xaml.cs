using System.IO;
using ArknightsLauncher.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace ArknightsLauncher
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public SettingsDialog()
        {
            this.InitializeComponent();
            LoadCurrentSettings();

            // 保存按钮点击
            this.PrimaryButtonClick += SettingsDialog_PrimaryButtonClick;
        }

        private void LoadCurrentSettings()
        {
            var cfg = ConfigService.Load();

            // 路径
            GamePathText.Text = string.IsNullOrEmpty(cfg.RootPath) ? "未设置" : cfg.RootPath;
            MAAPathText.Text = string.IsNullOrEmpty(cfg.MAA_Official) ? "未设置" : cfg.MAA_Official;

            // 主题
            switch (cfg.Theme)
            {
                case "Light": ThemeLight.IsChecked = true; break;
                case "Dark": ThemeDark.IsChecked = true; break;
                default: ThemeSystem.IsChecked = true; break;
            }

            // 材质
            MaterialCombo.SelectedIndex = cfg.Material switch
            {
                "None" => 0,
                "Acrylic" => 1,
                "Mica" => 2,
                "MicaAlt" => 3,
                _ => 1
            };

            // 语言
            LangCN.IsChecked = true;
        }

        private void SettingsDialog_PrimaryButtonClick(ContentDialog sender,
            ContentDialogButtonClickEventArgs args)
        {
            var cfg = ConfigService.Load();

            // 路径
            string gamePath = GamePathText.Text;
            if (gamePath != "未设置" && Directory.Exists(gamePath))
                cfg.RootPath = gamePath;

            string maaPath = MAAPathText.Text;
            if (maaPath != "未设置" && File.Exists(maaPath))
                cfg.MAA_Official = maaPath;

            // 主题
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

            ConfigService.Save(cfg);
        }

        private async void BrowseGameBtn_Click(object sender, RoutedEventArgs e)
        {
            while (true)
            {
                var picker = new FolderPicker();
                picker.FileTypeFilter.Add("*");

                // 获取窗口句柄——ContentDialog 没有 WindowNative，需要从 XamlRoot 拿
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                    (Application.Current as App)?.MainWindowInstance);
                InitializeWithWindow.Initialize(picker, hwnd);

                var folder = await picker.PickSingleFolderAsync();
                if (folder == null) return;

                if (folder.Name.Equals("Arknights Game",
                        System.StringComparison.OrdinalIgnoreCase))
                {
                    GamePathText.Text = folder.Path;
                    GamePathText.Foreground =
                        new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.White);
                    return;
                }

                var dlg = new ContentDialog
                {
                    Title = "路径错误",
                    Content = "请选择名为 'Arknights Game' 的文件夹。",
                    CloseButtonText = "重新选择",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
            }
        }

        private async void BrowseMAABtn_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".exe");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(
                (Application.Current as App)?.MainWindowInstance);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null && file.Name.Equals("MAA.exe",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                MAAPathText.Text = file.Path;
            }
            else if (file != null)
            {
                var dlg = new ContentDialog
                {
                    Title = "提示",
                    Content = "请选择正确的 MAA.exe 文件",
                    CloseButtonText = "确定",
                    XamlRoot = this.XamlRoot
                };
                await dlg.ShowAsync();
            }
        }
    }
}