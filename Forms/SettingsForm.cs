using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher.Forms
{
    public class SettingsForm : Form
    {
        private Panel _navPanel;
        private Panel _contentPanel;
        private Panel _pagePath;
        private Panel _pageSoftware;

        public SettingsForm()
        {
            Text = "设置";
            Size = new Size(530, 240);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 245, 245);

            BuildLayout();
            BuildPagePath();
            BuildPageSoftware();
            ShowPage("路径设置");
        }

        private void BuildLayout()
        {
            _navPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 100,
                BackColor = Color.White
            };
            _navPanel.Paint += (_, e) =>
                e.Graphics.DrawLine(new System.Drawing.Pen(Color.FromArgb(220, 220, 220)),
                    _navPanel.Width - 1, 0, _navPanel.Width - 1, _navPanel.Height);

            string[] pages = { "路径设置", "软件设置" };
            for (int i = 0; i < pages.Length; i++)
            {
                string pageName = pages[i];
                var navBtn = new Button
                {
                    Text = pageName,
                    Size = new Size(100, 36),
                    Location = new Point(0, i * 36),
                    FlatStyle = FlatStyle.Flat,
                    ForeColor = Color.FromArgb(60, 60, 60),
                    BackColor = Color.Transparent,
                    Font = new Font("Segoe UI", 9.5f),
                    Cursor = Cursors.Hand,
                    Tag = pageName,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                navBtn.FlatAppearance.BorderSize = 0;
                navBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
                navBtn.Click += (_, __) => ShowPage(pageName);
                _navPanel.Controls.Add(navBtn);
            }

            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(250, 250, 250),
                Padding = new Padding(18, 12, 18, 12)
            };

            Controls.Add(_contentPanel);
            Controls.Add(_navPanel);
        }

        private void ShowPage(string pageName)
        {
            foreach (Control c in _navPanel.Controls)
            {
                if (c is Button btn)
                {
                    bool active = (string)btn.Tag == pageName;
                    btn.BackColor = active ? Color.FromArgb(0, 122, 204) : Color.Transparent;
                    btn.ForeColor = active ? Color.White : Color.FromArgb(60, 60, 60);
                }
            }

            _pagePath.Visible     = pageName == "路径设置";
            _pageSoftware.Visible = pageName == "软件设置";
        }

        private void BuildPagePath()
        {
            _pagePath = new Panel { Dock = DockStyle.Fill, Visible = false };
            var cfg = ConfigHelper.Load();

            AddTitle(_pagePath, "路径设置");
            AddPathRow(_pagePath, "游戏根目录：", cfg.RootPath, 50,
                newPath => { var c = ConfigHelper.Load(); c.RootPath = newPath; ConfigHelper.Save(c); }, isFolder: true);
            AddPathRow(_pagePath, "MAA（官服）：", cfg.MAA_Official, 90,
                newPath => { var c = ConfigHelper.Load(); c.MAA_Official = newPath; ConfigHelper.Save(c); }, isFolder: false);
            AddPathRow(_pagePath, "MAA（B服）：", cfg.MAA_Bilibili, 130,
                newPath => { var c = ConfigHelper.Load(); c.MAA_Bilibili = newPath; ConfigHelper.Save(c); }, isFolder: false);

            _contentPanel.Controls.Add(_pagePath);
        }

        private void AddTitle(Panel page, string title)
        {
            page.Controls.Add(new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 30, 30),
                AutoSize = true,
                Location = new Point(0, 4)
            });
            page.Controls.Add(new Panel
            {
                Size = new Size(320, 1),
                Location = new Point(0, 26),
                BackColor = Color.FromArgb(220, 220, 220)
            });
        }

        private void AddPathRow(Panel page, string label, string currentValue, int y,
            System.Action<string> onSave, bool isFolder)
        {
            page.Controls.Add(new Label
            {
                Text = label,
                AutoSize = true,
                Location = new Point(0, y + 2),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            });

            var txt = new TextBox
            {
                Text = currentValue,
                ReadOnly = true,
                Size = new Size(195, 24),
                Location = new Point(100, y),
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(txt);

            var btn = new Button
            {
                Text = "浏览...",
                Size = new Size(52, 24),
                Location = new Point(300, y),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };
            btn.Click += (_, __) =>
            {
                string newPath = isFolder ? DialogHelper.SelectFolder() : DialogHelper.SelectExe(label, "");
                if (string.IsNullOrEmpty(newPath)) return;
                txt.Text = newPath;
                onSave(newPath);
            };
            page.Controls.Add(btn);
        }

        private void BuildPageSoftware()
        {
            _pageSoftware = new Panel { Dock = DockStyle.Fill, Visible = false };
            var cfg = ConfigHelper.Load();

            AddTitle(_pageSoftware, "软件设置");

            var chkShowTray = new CheckBox
            {
                Text = "显示托盘图标(隐藏至托盘)",
                AutoSize = true,
                Location = new Point(0, 68),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.ShowTrayIcon,
                Cursor = Cursors.Hand
            };

            var chkMinToTray = new CheckBox
            {
                Text = "最小化时隐藏至托盘",
                AutoSize = true,
                Location = new Point(0, 95),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.MinimizeToTray,
                Enabled = cfg.ShowTrayIcon,
                Cursor = Cursors.Hand
            };

            chkShowTray.CheckedChanged += (_, __) =>
            {
                var c = ConfigHelper.Load();
                c.ShowTrayIcon = chkShowTray.Checked;
                if (!chkShowTray.Checked) chkMinToTray.Checked = false;
                ConfigHelper.Save(c);
                chkMinToTray.Enabled = chkShowTray.Checked;

                var mainForm = Application.OpenForms.OfType<ServerSelectForm>().FirstOrDefault();
                mainForm?.UpdateTrayVisibility(chkShowTray.Checked);
            };

            chkMinToTray.CheckedChanged += (_, __) =>
            {
                var c = ConfigHelper.Load();
                c.MinimizeToTray = chkMinToTray.Checked;
                ConfigHelper.Save(c);
            };

            _pageSoftware.Controls.Add(new Label
            {
                Text = "启动时自动打开：",
                AutoSize = true,
                Location = new Point(0, 44),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            });

            var chkAutoOfficial = new CheckBox
            {
                Text = "官服",
                AutoSize = true,
                Location = new Point(120, 44),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.AutoLaunchOfficial,
                Cursor = Cursors.Hand
            };

            var chkAutoBilibili = new CheckBox
            {
                Text = "B服",
                AutoSize = true,
                Location = new Point(175, 44),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.AutoLaunchBilibili,
                Cursor = Cursors.Hand
            };

            chkAutoOfficial.CheckedChanged += (_, __) =>
            {
                if (chkAutoOfficial.Checked) chkAutoBilibili.Checked = false;
                var c = ConfigHelper.Load();
                c.AutoLaunchOfficial = chkAutoOfficial.Checked;
                ConfigHelper.Save(c);
            };

            chkAutoBilibili.CheckedChanged += (_, __) =>
            {
                if (chkAutoBilibili.Checked) chkAutoOfficial.Checked = false;
                var c = ConfigHelper.Load();
                c.AutoLaunchBilibili = chkAutoBilibili.Checked;
                ConfigHelper.Save(c);
            };

            var chkExitAfterLaunch = new CheckBox
            {
                Text = "游戏启动后自动关闭本软件",
                AutoSize = true,
                Location = new Point(0, 122),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.ExitAfterLaunch,
                Cursor = Cursors.Hand
            };

            chkExitAfterLaunch.CheckedChanged += (_, __) =>
            {
                var c = ConfigHelper.Load();
                c.ExitAfterLaunch = chkExitAfterLaunch.Checked;
                ConfigHelper.Save(c);
            };

            _pageSoftware.Controls.AddRange(new Control[]
            {
                chkShowTray, chkMinToTray, chkAutoOfficial, chkAutoBilibili, chkExitAfterLaunch
            });

            _contentPanel.Controls.Add(_pageSoftware);
        }
    }
}
