using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using ArknightsLauncher.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ArknightsLauncher
{
    public sealed partial class AboutDialog : ContentDialog
    {
        public AboutDialog()
        {
            this.InitializeComponent();
            VersionLabel.Text = $"v{UpdateService.CurrentVersion}";
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            BtnUpdate.IsEnabled = false;
            BtnUpdate.Content = "检查中…";
            UpdateStatusText.Text = "";
            DownloadProgress.Visibility = Visibility.Collapsed;

            bool useChina = SourceCombo.SelectedIndex == 1;

            try
            {
                var (hasUpdate, latestVersion, downloadUrl) = await UpdateService.CheckAsync(useChina);

                if (!hasUpdate)
                {
                    UpdateStatusText.Text = "当前已是最新版本！";
                    return;
                }

                // 询问是否下载
                var confirm = new ContentDialog
                {
                    Title = "发现新版本",
                    Content = $"发现新版本 v{latestVersion}，是否立即下载？",
                    PrimaryButtonText = "下载并安装",
                    CloseButtonText = "稍后",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await confirm.ShowAsync() != ContentDialogResult.Primary) return;

                // 下载
                DownloadProgress.Visibility = Visibility.Visible;
                BtnUpdate.Content = "下载中…";

                var progress = new Progress<UpdateProgress>(p =>
                {
                    DownloadProgress.Value = p.Percent;
                    UpdateStatusText.Text = p.Message;
                });

                await UpdateService.DownloadAndInstallAsync(downloadUrl, progress);
            }
            catch (TaskCanceledException)
            {
                var dlg = new ContentDialog
                {
                    Title = "连接超时",
                    Content = "无法连接到更新服务器，是否打开浏览器手动下载？",
                    PrimaryButtonText = "打开浏览器",
                    CloseButtonText = "取消",
                    XamlRoot = this.XamlRoot
                };
                if (await dlg.ShowAsync() == ContentDialogResult.Primary)
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://github.com/lTinchl/ArknightsLauncher",
                        UseShellExecute = true
                    });
            }
            catch (HttpRequestException ex)
            {
                UpdateStatusText.Text = $"请求失败：{ex.Message}";
            }
            catch (Exception ex)
            {
                UpdateStatusText.Text = $"错误：{ex.Message}";
            }
            finally
            {
                BtnUpdate.Content = "检查更新";
                BtnUpdate.IsEnabled = true;
            }
        }
    }
}
