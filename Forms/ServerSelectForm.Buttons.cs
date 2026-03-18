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
            var btn = MakeGameButton(text, icon, pos, new Size(120, 60));

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

                _ = Task.Run(async () =>
                {
                    await Task.Delay(4000);
                    await launchForm.RunCore();
                    this.Invoke(() => this.Show());
                });
            };

            return btn;
        }

        private Button CreateMAAButton(string text, System.Drawing.Icon icon, Point pos, ServerType type)
        {
            var btn = MakeGameButton(text, icon, pos, new Size(120, 60));

            btn.Click += async (_, __) =>
            {
                var cfg = ConfigHelper.Load();
                string exePath = type == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;

                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    string title = type == ServerType.MAA_Official ? "请选择 MAA.exe (MAA-官)" : "请选择 MAA.exe (MAA-B)";
                    exePath = DialogHelper.SelectExe(title, "MAA 程序");
                    if (string.IsNullOrEmpty(exePath)) return;

                    if (type == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                    else cfg.MAA_Bilibili = exePath;
                    ConfigHelper.Save(cfg);
                }

                var launchForm = new LaunchForm(type);
                launchForm.Show();

                _ = Task.Run(async () =>
                {
                    await launchForm.RunCore();
                    this.Invoke(() => this.Show());
                });
            };

            return btn;
        }

        private Button CreateLinkButton(string text, System.Drawing.Icon icon, Point pos, int width, Action onClick)
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
                Size = new Size(width, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            originalBmp.Dispose();
            btn.Click += (_, __) => onClick();
            return btn;
        }

        private Button CreateAboutButton(string text, System.Drawing.Icon icon, Point pos)
        {
            var btn = CreateLinkButton(text, icon, pos, 80, () =>
            {
                using var f = new AboutForm();
                f.ShowDialog();
            });
            return btn;
        }

        private static Button MakeGameButton(string text, System.Drawing.Icon icon, Point pos, Size size)
        {
            return new Button
            {
                Text = text,
                Image = icon.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Size = size,
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };
        }
    }
}
