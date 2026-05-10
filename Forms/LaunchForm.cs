using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;

namespace ArknightsLauncher.Forms
{
    public class LaunchForm : Form
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
                ServerType.LinkedSoftwareOfficial => "官服联动软件",
                ServerType.LinkedSoftwareBilibili => "B服联动软件",
                _ => "Launcher"
            };

            Icon = serverType switch
            {
                ServerType.Official => ResourceHelper.LoadIcon("official.ico"),
                ServerType.Bilibili => ResourceHelper.LoadIcon("bserver.ico"),
                ServerType.LinkedSoftwareOfficial => ResourceHelper.LoadIcon("MAA.ico"),
                ServerType.LinkedSoftwareBilibili => ResourceHelper.LoadIcon("MAA.ico"),
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
                Text = IsLinkedSoftware(_serverType)
                       ? $"正在启动{GetLinkedSoftwareScopeName(_serverType)}联动软件…"
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
                var cfg = ConfigHelper.Load();

                if (IsLinkedSoftware(_serverType))
                {
                    bool isOfficialLinkedSoftware = _serverType == ServerType.LinkedSoftwareOfficial;
                    if (!cfg.IsLinkedSoftwareEnabled(isOfficialLinkedSoftware))
                    {
                        MessageBox.Show($"{(isOfficialLinkedSoftware ? "官服" : "B服")}联动软件开关已关闭，请在设置中开启。", "提示");
                        return;
                    }

                    var softwares = cfg.GetLinkedSoftwareItems(isOfficialLinkedSoftware);
                    if (softwares.Count == 0)
                    {
                        softwares = SelectAndSaveLinkedSoftware(isOfficialLinkedSoftware);
                        if (softwares.Count == 0) return;
                    }

                    GameLauncher.StartLinkedSoftwares(softwares);
                    await Task.Delay(1000);
                }
                else
                {
                    string rootPath = _rootPath;

                    if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    {
                        rootPath = (string)this.Invoke((Func<string>)(() =>
                            DialogHelper.SelectFolder()));

                        if (string.IsNullOrEmpty(rootPath)) return;

                        cfg.RootPath = rootPath;
                        ConfigHelper.Save(cfg);
                    }

                    string zipResourceName = _serverType == ServerType.Official ? "Payload.zip" : "Payload_B.zip";
                    await Task.Delay(1000);
                    await Task.Run(() => ResourceHelper.ExtractAndOverwrite(rootPath, zipResourceName));

                    GameLauncher.StartArknights(rootPath);
                    bool isOfficial = _serverType == ServerType.Official;
                    if (cfg.IsLinkedSoftwareEnabled(isOfficial))
                        GameLauncher.StartLinkedSoftwares(cfg.GetLinkedSoftwareItems(isOfficial), requireAny: false);

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

        private bool IsLinkedSoftware(ServerType serverType)
        {
            return serverType == ServerType.LinkedSoftwareOfficial
                || serverType == ServerType.LinkedSoftwareBilibili;
        }

        private string GetLinkedSoftwareScopeName(ServerType serverType)
        {
            return serverType == ServerType.LinkedSoftwareBilibili ? "B服" : "官服";
        }

        private System.Collections.Generic.List<LinkedSoftwareItem> SelectAndSaveLinkedSoftware(bool isOfficial)
        {
            string scopeName = isOfficial ? "官服" : "B服";
            string exePath = (string)this.Invoke((Func<string>)(() =>
                DialogHelper.SelectExe($"请选择{scopeName}联动软件", "可执行文件")));
            if (string.IsNullOrEmpty(exePath))
                return new System.Collections.Generic.List<LinkedSoftwareItem>();

            var cfg = ConfigHelper.Load();
            cfg.NormalizeLinkedSoftwares(isOfficial);
            var target = isOfficial ? cfg.LinkedSoftwaresOfficial : cfg.LinkedSoftwaresBilibili;
            if (!target.Exists(item => string.Equals(item.Path, exePath, StringComparison.OrdinalIgnoreCase)))
            {
                target.Add(new LinkedSoftwareItem
                {
                    Name = Path.GetFileNameWithoutExtension(exePath),
                    Path = exePath
                });
                ConfigHelper.Save(cfg);
            }

            return cfg.GetLinkedSoftwareItems(isOfficial);
        }
    }
}
