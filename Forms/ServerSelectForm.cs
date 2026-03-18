using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;

namespace ArknightsLauncher.Forms
{
    public partial class ServerSelectForm : Form
    {
        private Button _officialBtn;
        private Button _bServerBtn;
        private ComboBox officialCombo;
        private NotifyIcon _trayIcon;
        private bool _forceClose = false;

        public ServerSelectForm()
        {
            Text = "Arknights Launcher";
            Icon = ResourceHelper.LoadIcon("ArknightsLauncher.ico");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(395, 200);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;

            officialCombo = new ComboBox
            {
                Location = new Point(125, 36),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            ReloadAccounts();
            Controls.Add(officialCombo);

            // 账号管理按钮
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

            // 设置按钮
            var settingsBtn = new Button
            {
                Text = "设置",
                Image = ResizeBitmap(ResourceHelper.LoadIcon("Setting.ico").ToBitmap(), 20, 20),
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

            // 服务器按钮
            Button bServerBtn = null;
            _officialBtn = CreateServerButton("官服", ResourceHelper.LoadIcon("official.ico"), new Point(5, 35),  ServerType.Official, () => bServerBtn);
            Controls.Add(_officialBtn);

            _bServerBtn = CreateServerButton("B服",  ResourceHelper.LoadIcon("bserver.ico"),  new Point(5, 95),  ServerType.Bilibili, null);
            _bServerBtn.Enabled = !ConfigHelper.Load().IsFirstRun;
            Controls.Add(_bServerBtn);
            bServerBtn = _bServerBtn; // 修复闭包捕获

            Controls.Add(CreateMAAButton  ("MAA-官",   ResourceHelper.LoadIcon("MAA.ico"),              new Point(270, 35),  ServerType.MAA_Official));
            Controls.Add(CreateMAAButton  ("MAA-B",    ResourceHelper.LoadIcon("MAA.ico"),              new Point(270, 95),  ServerType.MAA_Bilibili));
            Controls.Add(CreateLinkButton ("PRTS Wiki",  ResourceHelper.LoadIcon("PRTS_WIKI.ico"),      new Point(5,   155), 80,  BrowserHelper.OpenPrtsWiki));
            Controls.Add(CreateLinkButton ("方舟工具箱", ResourceHelper.LoadIcon("Arknights_Toolbox.ico"), new Point(85,  155), 115, BrowserHelper.OpenToolbox));
            Controls.Add(CreateLinkButton ("方舟一图流", ResourceHelper.LoadIcon("Arknights_Yituliu.ico"), new Point(200, 155), 110, BrowserHelper.OpenYituliu));
            Controls.Add(CreateAboutButton("关于",      ResourceHelper.LoadIcon("Info.ico"),            new Point(310, 155)));

            // 启动时检查更新 & 自动启动
            this.Shown += OnShown;

            this.FormClosing += (_, e) =>
            {
                var c = ConfigHelper.Load();
                if (!_forceClose && c.ShowTrayIcon)
                {
                    e.Cancel = true;
                    this.Hide();
                }
            };

            InitTray();
        }

        private void ReloadAccounts()
        {
            officialCombo.Items.Clear();
            var cfg = ConfigHelper.Load();

            if (cfg.Accounts.Count == 0)
            {
                cfg.Accounts["A1"] = "默认账号";
                cfg.DefaultAccount = "A1";
                Directory.CreateDirectory(Path.Combine(ConfigHelper.AccountBackupDir, "A1"));
                ConfigHelper.Save(cfg);
            }

            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
                officialCombo.Items.Add(new AccountItem { Id = cfg.DefaultAccount, Remark = cfg.Accounts[cfg.DefaultAccount] + "⭐" });

            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue;
                officialCombo.Items.Add(new AccountItem { Id = acc.Key, Remark = acc.Value });
            }

            if (officialCombo.Items.Count > 0)
                officialCombo.SelectedIndex = 0;
        }

        private async void OnShown(object sender, EventArgs e)
        {
            try
            {
                var config = ConfigHelper.Load();
                var (hasUpdate, latestVersion, _) = await UpdateHelper.CheckForUpdateAsync();

                if (hasUpdate)
                {
                    if (config.LastNotifiedVersion != latestVersion)
                    {
                        MessageBox.Show($"发现新版本 v{latestVersion}，请在关于页面更新。", "发现新版本");
                        config.LastNotifiedVersion = latestVersion;
                        ConfigHelper.Save(config);
                    }
                    this.Text = "Arknights Launcher [New↑]";
                }
            }
            catch { }

            var autoCfg = ConfigHelper.Load();
            if (autoCfg.AutoLaunchOfficial)       _officialBtn.PerformClick();
            else if (autoCfg.AutoLaunchBilibili)  _bServerBtn.PerformClick();
        }

        public void UpdateTrayVisibility(bool visible)
        {
            if (_trayIcon != null) _trayIcon.Visible = visible;
        }

        internal static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using var g = Graphics.FromImage(resized);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, 0, 0, width, height);
            return resized;
        }
    }
}
