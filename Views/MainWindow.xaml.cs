using ArknightsLauncher.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace ArknightsLauncher.Views
{
    public partial class MainWindow : HandyControl.Controls.Window
    {
        private List<LinkedAppOption> linkedApps = new();
        private bool ready;

        public MainWindow()
        {
            InitializeComponent();
            PrtsIcon.Source = AppIconHelper.FromResource("PRTS_WIKI.ico");
            YituliuIcon.Source = AppIconHelper.FromResource("Arknights_Yituliu.ico");
            ArkToolsIcon.Source = AppIconHelper.FromResource("Arknights_Toolbox.ico");
            ready = true;
            RefreshConfiguration();
            AddLog("启动器已就绪");
            AddLog("等待操作…");
        }

        private void RefreshConfiguration()
        {
            var config = ConfigHelper.Load();
            var accounts = config.Accounts.Select(pair => new AccountItem { Id = pair.Key, Remark = pair.Value }).ToList();
            accounts.Insert(0, new AccountItem { Id = "", Remark = "当前账号" });
            AccountComboBox.ItemsSource = accounts;
            AccountComboBox.SelectedItem = accounts.FirstOrDefault(a => a.Id == config.DefaultAccount) ?? accounts[0];
            AccountComboBox.IsEnabled = OfficialServerRadio.IsChecked == true;
            linkedApps = config.GetLinkedSoftwareItems(OfficialServerRadio.IsChecked == true)
                            .Select(item => new LinkedAppOption { Name = item.Name, ExePath = item.Path }).ToList();
            LinkedAppsList.ItemsSource = linkedApps;
            LinkedAppsHint.Text = linkedApps.Count == 0 ? "尚未配置联动程序，请在设置中添加" : "选择联动程序（可多选）";
            UpdateLinkedSummary();
            bool installed = File.Exists(Path.Combine(config.RootPath, "Arknights.exe"));
            ClientStatusDot.Fill = installed ? Brushes.LimeGreen : Brushes.SlateGray;
            LocalClientText.Text = installed ? "已找到客户端" : "未找到客户端";
            ResourceVersionText.Text = "资源版本： —";
            if (installed)
            {
                var version = FileVersionInfo.GetVersionInfo(Path.Combine(config.RootPath, "Arknights.exe")).FileVersion;
                LocalClientText.Text += string.IsNullOrWhiteSpace(version) ? "" : "  v" + version;
            }
        }

        private void ServerChanged(object sender, RoutedEventArgs e)
        {
            if (!ready) return;
            RefreshConfiguration();
        }
        private void LinkedAppChanged(object sender, RoutedEventArgs e)
        {
            // The checkbox event may precede the binding source update.
            Dispatcher.BeginInvoke(new Action(UpdateLinkedSummary));
        }
        private void UpdateLinkedSummary() => LinkedAppsSummary.Text = linkedApps.Any(a => a.IsSelected)
            ? $"已选择 {linkedApps.Count(a => a.IsSelected)} 项" : "未选择";

        private void AddLog(string message, bool error = false)
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 5) };
            paragraph.Inlines.Add(new Run($"[{DateTime.Now:HH:mm:ss}]  "));
            paragraph.Inlines.Add(new Run(error ? "[错误]  " : "[信息]  ") { Foreground = (Brush)FindResource(error ? "ErrorLogBrush" : "InfoLogBrush") });
            paragraph.Inlines.Add(new Run(message));
            LogTextBox.Document.Blocks.Add(paragraph);
            LogTextBox.ScrollToEnd();
        }
        private void ClearLog_Click(object sender, RoutedEventArgs e) => LogTextBox.Document.Blocks.Clear();
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            using var form = new SettingsForm();
            form.ShowDialog();
            RefreshConfiguration();
        }
        private void Accounts_Click(object sender, RoutedEventArgs e)
        {
            using var form = new AccountManagerForm();
            form.ShowDialog();
            RefreshConfiguration();
        }
        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            var path = ConfigHelper.Load().RootPath;
            if (!Directory.Exists(path)) { AddLog("请先在设置中配置游戏目录。", true); return; }
            OpenTarget(path);
        }
        private void Tool_Click(object sender, RoutedEventArgs e)
        {
            string url = ((Button)sender).Tag.ToString() switch
            {
                "skland" => "https://www.skland.com/",
                "prts" => "https://prts.wiki/",
                "yituliu" => "https://ark.yituliu.cn/",
                _ => "https://arktools.github.io/"
            };
            OpenTarget(url);
        }
        private void OpenTarget(string target)
        {
            try { Process.Start(new ProcessStartInfo(target) { UseShellExecute = true }); }
            catch (Exception ex) { AddLog(ex.Message, true); }
        }
        private async void Launch_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigHelper.Load();
            if (!File.Exists(Path.Combine(config.RootPath, "Arknights.exe")))
            {
                AddLog("未找到 Arknights.exe，请先在设置中配置游戏目录。", true);
                return;
            }
            LaunchButton.IsEnabled = false;
            try
            {
                bool official = OfficialServerRadio.IsChecked == true;
                var selected = AccountComboBox.SelectedItem as AccountItem;
                if (official && selected != null && !string.IsNullOrEmpty(selected.Id))
                {
                    // Account restoration remains in the existing account manager until migrated.
                    AddLog("请先通过账号管理切换到所选账号，再选择“当前账号”启动。", true);
                    return;
                }
                AddLog(official ? "正在启动官服…" : "正在启动 B服…");
                using var launch = new LaunchForm(official ? ServerType.Official : ServerType.Bilibili, config.RootPath);
                launch.Show();
                await launch.RunCore();
                foreach (var app in linkedApps.Where(a => a.IsSelected))
                {
                    GameLauncher.StartLinkedSoftware(app.ExePath);
                    AddLog($"已启动联动程序：{app.Name}");
                }
                AddLog("启动流程已结束");
            }
            catch (Exception ex) { AddLog(ex.Message, true); }
            finally { LaunchButton.IsEnabled = true; }
        }
        public sealed class LinkedAppOption
        {
            public string Name { get; set; } = "";
            public string ExePath { get; set; } = "";
            public bool IsSelected { get; set; }
        }
    }
}

