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
        private Button _linkedOfficialBtn;
        private Button _linkedBilibiliBtn;
        private ComboBox officialCombo;
        private NotifyIcon _trayIcon;
        private bool _forceClose = false;

        public ServerSelectForm()
        {
            Text = "Arknights Launcher";
            Icon = ResourceHelper.LoadIcon("ArknightsLauncher.ico");
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(620, 330);
            MinimumSize = new Size(636, 369);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            BackColor = Color.FromArgb(246, 248, 251);
            Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular);

            officialCombo = new ComboBox
            {
                Location = new Point(155, 21),
                Size = new Size(128, 28),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            ReloadAccounts();
            Controls.Add(officialCombo);

            // 账号管理按钮
            var manageBtn = new StyledIconButton
            {
                Text = "账号管理",
                Size = new Size(128, 34),
                Location = new Point(16, 16),
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                IconSize = 0,
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
            var settingsBtn = new StyledIconButton
            {
                Text = "设置",
                Image = IconToBitmap(ResourceHelper.LoadIcon("Setting.ico"), 20),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Microsoft YaHei UI", 9f, FontStyle.Regular),
                Size = new Size(108, 34),
                Location = new Point(496, 16),
                Padding = new Padding(8, 0, 0, 0),
                IconSize = 20,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            settingsBtn.Click += (_, __) =>
            {
                using var f = new SettingsForm();
                f.ShowDialog();
                RefreshLinkedSoftwareButtons();
            };
            Controls.Add(settingsBtn);

            // 服务器按钮
            Button bServerBtn = null;
            AddSectionTitle("游戏启动", new Point(16, 66));
            AddSectionTitle("联动软件", new Point(384, 66));

            _officialBtn = CreateServerButton("官服", ResourceHelper.LoadIcon("official.ico"), new Point(16, 90),  ServerType.Official, () => bServerBtn);
            Controls.Add(_officialBtn);

            _bServerBtn = CreateServerButton("B服",  ResourceHelper.LoadIcon("bserver.ico"),  new Point(16, 178),  ServerType.Bilibili, null);
            _bServerBtn.Enabled = !ConfigHelper.Load().IsFirstRun;
            Controls.Add(_bServerBtn);
            bServerBtn = _bServerBtn; // 修复闭包捕获

            var cfg = ConfigHelper.Load();
            _linkedOfficialBtn = CreateLinkedSoftwareButton(cfg.GetLinkedSoftwareButtonName(true), ResourceHelper.LoadIcon("MAA.ico"), new Point(384, 90), ServerType.LinkedSoftwareOfficial);
            Controls.Add(_linkedOfficialBtn);
            _linkedBilibiliBtn = CreateLinkedSoftwareButton(cfg.GetLinkedSoftwareButtonName(false), ResourceHelper.LoadIcon("MAA.ico"), new Point(384, 178), ServerType.LinkedSoftwareBilibili);
            Controls.Add(_linkedBilibiliBtn);
            RefreshLinkedSoftwareButtons();
            Controls.Add(CreateLinkButton ("PRTS Wiki",  ResourceHelper.LoadIcon("PRTS_WIKI.ico"),      new Point(16,  278), 124, BrowserHelper.OpenPrtsWiki));
            Controls.Add(CreateLinkButton ("方舟工具箱", ResourceHelper.LoadIcon("Arknights_Toolbox.ico"), new Point(152, 278), 148, BrowserHelper.OpenToolbox));
            Controls.Add(CreateLinkButton ("方舟一图流", ResourceHelper.LoadIcon("Arknights_Yituliu.ico"), new Point(312, 278), 148, BrowserHelper.OpenYituliu));
            Controls.Add(CreateAboutButton("关于",      ResourceHelper.LoadIcon("Info.ico"),            new Point(472, 278)));

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

        private void AddSectionTitle(string text, Point location)
        {
            Controls.Add(new Label
            {
                Text = text,
                AutoSize = true,
                Location = location,
                Font = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 91, 108)
            });
        }

        private static void StyleUtilityButton(Button btn)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = Color.White;
            btn.ForeColor = Color.FromArgb(28, 39, 55);
            btn.FlatAppearance.BorderColor = Color.FromArgb(204, 213, 225);
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(238, 244, 252);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(224, 236, 249);
        }

        private void RefreshLinkedSoftwareButtons()
        {
            var cfg = ConfigHelper.Load();
            if (_linkedOfficialBtn != null)
            {
                _linkedOfficialBtn.Text = cfg.GetLinkedSoftwareButtonName(true);
                _linkedOfficialBtn.Enabled = cfg.IsLinkedSoftwareEnabled(true);
            }
            if (_linkedBilibiliBtn != null)
            {
                _linkedBilibiliBtn.Text = cfg.GetLinkedSoftwareButtonName(false);
                _linkedBilibiliBtn.Enabled = cfg.IsLinkedSoftwareEnabled(false);
            }
        }

        internal static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using var g = Graphics.FromImage(resized);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, 0, 0, width, height);
            return resized;
        }

        internal static Bitmap IconToBitmap(System.Drawing.Icon icon, int size)
        {
            using var sizedIcon = new System.Drawing.Icon(icon, size, size);
            return sizedIcon.ToBitmap();
        }
    }
}
