using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;

namespace ArknightsLauncher.Forms
{
    public partial class ServerSelectForm
    {
        private Button CreateServerButton(string text, System.Drawing.Icon icon, Point pos, ServerType type, Func<Button> getBServerBtn)
        {
            var btn = MakeGameButton(text, icon, pos, new Size(220, 76));

            btn.Click += async (_, __) =>
            {
                if (type == ServerType.Official && getBServerBtn != null)
                {
                    var cfg = ConfigHelper.Load();
                    if (cfg.IsFirstRun)
                    {
                        var bBtn = getBServerBtn();
                        if (bBtn != null) bBtn.Enabled = true;
                        cfg.IsFirstRun = false;
                        ConfigHelper.Save(cfg);
                    }
                }

                try { GameLauncher.KillArknightsProcesses(); }
                catch (Exception ex) { MessageBox.Show("关闭 Arknights 时出错:\n" + ex.Message, "错误"); }

                var cfg2 = ConfigHelper.Load();
                string rootPath = cfg2.RootPath;

                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    rootPath = DialogHelper.SelectFolder();
                    if (string.IsNullOrEmpty(rootPath)) return;
                    cfg2.RootPath = rootPath;
                    ConfigHelper.Save(cfg2);
                }

                var launchForm = new LaunchForm(type, rootPath);
                launchForm.Show();

                // 账号切换（仅官服）
                if (type == ServerType.Official)
                {
                    var selectedItem = officialCombo.SelectedItem as AccountItem;
                    if (selectedItem == null) return;

                    string sdkPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "AppData", "LocalLow", "Hypergryph", "Arknights");

                    if (!Directory.Exists(sdkPath))
                    {
                        _ = Task.Run(() => ImageMessageBox.Show(
                            "未找到 Arknights 数据目录，请先通过鹰角启动器启动一次游戏。", "Start.png", "提示"));
                    }
                    else
                    {
                        var sdkDir = System.Linq.Enumerable.FirstOrDefault(Directory.GetDirectories(sdkPath, "sdk_data_*"));
                        if (sdkDir == null)
                        {
                            _ = Task.Run(() => ImageMessageBox.Show(
                                "未找到 sdk_data_* 文件夹，请通过鹰角启动器启动一次进入账号输入界面再手动关闭进程", "Main.png", "提示"));
                        }
                        else
                        {
                            string backupFolder = Path.Combine(ConfigHelper.AccountBackupDir, selectedItem.Id);
                            if (Directory.Exists(backupFolder))
                            {
                                await Task.Delay(3000);
                                await GameLauncher.CopyDirectory(backupFolder, sdkDir);
                            }
                        }
                    }
                }

                await Task.Delay(4000);
                await launchForm.RunCore();

                if (ConfigHelper.Load().ExitAfterLaunch)
                    Application.Exit();
                else
                    this.Show();
            };

            return btn;
        }

        private Button CreateLinkedSoftwareButton(string text, System.Drawing.Icon icon, Point pos, ServerType type)
        {
            var btn = MakeGameButton(text, icon, pos, new Size(220, 76));

            btn.Click += async (_, __) =>
            {
                var cfg = ConfigHelper.Load();
                bool isOfficialLinkedSoftware = type == ServerType.LinkedSoftwareOfficial;
                if (!cfg.IsLinkedSoftwareEnabled(isOfficialLinkedSoftware))
                {
                    MessageBox.Show($"{(isOfficialLinkedSoftware ? "官服" : "B服")}联动软件开关已关闭，请在设置中开启。", "提示");
                    return;
                }

                var launchForm = new LaunchForm(type);
                launchForm.Show();

                await launchForm.RunCore();
                this.Show();
            };

            return btn;
        }

        private Button CreateLinkButton(string text, System.Drawing.Icon icon, Point pos, int width, Action onClick)
        {
            var btn = new StyledIconButton
            {
                Text = text,
                Image = IconToBitmap(icon, 24),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                Size = new Size(width, 36),
                Location = pos,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(35, 46, 64),
                BorderColor = Color.FromArgb(204, 213, 225),
                HoverBackColor = Color.FromArgb(238, 244, 252),
                PressedBackColor = Color.FromArgb(224, 236, 249),
                IconSize = 24,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            btn.Click += (_, __) => onClick();
            return btn;
        }

        private Button CreateAboutButton(string text, System.Drawing.Icon icon, Point pos)
        {
            var btn = CreateLinkButton(text, icon, pos, 132, () =>
            {
                using var f = new AboutForm();
                f.ShowDialog();
            });
            return btn;
        }

        private static Button MakeGameButton(string text, System.Drawing.Icon icon, Point pos, Size size)
        {
            var btn = new StyledIconButton
            {
                Text = text,
                Image = IconToBitmap(icon, 48),
                Font = new Font("Microsoft YaHei UI", 10.5f, FontStyle.Bold),
                Size = size,
                Location = pos,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(23, 33, 48),
                BorderColor = Color.FromArgb(201, 211, 224),
                HoverBackColor = Color.FromArgb(235, 243, 253),
                PressedBackColor = Color.FromArgb(220, 234, 250),
                IconSize = 48,
                AutoEllipsis = true,
                Cursor = Cursors.Hand
            };
            return btn;
        }
    }
}
