using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;
using ArknightsLauncher.Models;

namespace ArknightsLauncher.Forms
{
    public class SettingsForm : Form
    {
        private Panel _navPanel;
        private Panel _contentPanel;
        private Panel _pagePath;
        private Panel _pageLinkedSoftware;
        private Panel _pageSkland;
        private Panel _pageSoftware;

        public SettingsForm()
        {
            Text = "设置";
            Size = new Size(560, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(245, 245, 245);

            BuildLayout();
            BuildPagePath();
            BuildPageLinkedSoftware();
            BuildPageSkland();
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

            string[] pages = { "路径设置", "联动软件", "森空岛签到", "软件设置" };
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
            _pageLinkedSoftware.Visible = pageName == "联动软件";
            _pageSkland.Visible = pageName == "森空岛签到";
            _pageSoftware.Visible = pageName == "软件设置";
        }

        private void BuildPagePath()
        {
            _pagePath = new Panel { Dock = DockStyle.Fill, Visible = false };
            var cfg = ConfigHelper.Load();

            AddTitle(_pagePath, "路径设置");
            AddPathRow(_pagePath, "游戏根目录：", cfg.RootPath, 50,
                newPath => { var c = ConfigHelper.Load(); c.RootPath = newPath; ConfigHelper.Save(c); }, isFolder: true);

            _contentPanel.Controls.Add(_pagePath);
        }

        private void BuildPageLinkedSoftware()
        {
            _pageLinkedSoftware = new Panel { Dock = DockStyle.Fill, Visible = false };
            var cfg = ConfigHelper.Load();
            cfg.NormalizeLinkedSoftwares();
            ConfigHelper.Save(cfg);

            AddTitle(_pageLinkedSoftware, "联动软件");

            AddLinkedSoftwareSection(_pageLinkedSoftware, "官服联动软件", 44, true);
            AddLinkedSoftwareSection(_pageLinkedSoftware, "B服联动软件", 180, false);
            _contentPanel.Controls.Add(_pageLinkedSoftware);
        }

        private void AddLinkedSoftwareSection(Panel page, string title, int y, bool isOfficial)
        {
            var cfg = ConfigHelper.Load();

            page.Controls.Add(new Label
            {
                Text = title,
                AutoSize = true,
                Location = new Point(0, y + 3),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 45, 45)
            });

            var chkEnable = new CheckBox
            {
                Text = "启用",
                AutoSize = true,
                Location = new Point(125, y + 2),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.IsLinkedSoftwareEnabled(isOfficial),
                Cursor = Cursors.Hand
            };
            page.Controls.Add(chkEnable);

            var list = new ListBox
            {
                Size = new Size(380, 72),
                Location = new Point(0, y + 30),
                Font = new Font("Segoe UI", 8.5f),
                HorizontalScrollbar = true
            };
            page.Controls.Add(list);

            void ReloadLinkedSoftwareList()
            {
                list.Items.Clear();
                foreach (var item in ConfigHelper.Load().GetLinkedSoftwareItems(isOfficial))
                    list.Items.Add(item);
            }
            ReloadLinkedSoftwareList();

            chkEnable.CheckedChanged += (_, __) =>
            {
                var c = ConfigHelper.Load();
                if (isOfficial) c.EnableLinkedSoftwareOfficial = chkEnable.Checked;
                else c.EnableLinkedSoftwareBilibili = chkEnable.Checked;
                ConfigHelper.Save(c);
            };

            var btnAdd = new Button
            {
                Text = "添加...",
                Size = new Size(72, 26),
                Location = new Point(0, y + 110),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnAdd.Click += (_, __) =>
            {
                string exePath = DialogHelper.SelectExe($"请选择{title}", "可执行文件");
                if (string.IsNullOrEmpty(exePath)) return;

                var c = ConfigHelper.Load();
                c.NormalizeLinkedSoftwares(isOfficial);
                var target = isOfficial ? c.LinkedSoftwaresOfficial : c.LinkedSoftwaresBilibili;
                if (!target.Any(item => string.Equals(item.Path, exePath, System.StringComparison.OrdinalIgnoreCase)))
                {
                    target.Add(new LinkedSoftwareItem
                    {
                        Name = Path.GetFileNameWithoutExtension(exePath),
                        Path = exePath
                    });
                    ConfigHelper.Save(c);
                }
                ReloadLinkedSoftwareList();
            };

            var btnRemove = new Button
            {
                Text = "删除",
                Size = new Size(72, 26),
                Location = new Point(82, y + 110),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };
            btnRemove.Click += (_, __) =>
            {
                if (list.SelectedItem is not LinkedSoftwareItem selected) return;

                var c = ConfigHelper.Load();
                c.NormalizeLinkedSoftwares(isOfficial);
                var target = isOfficial ? c.LinkedSoftwaresOfficial : c.LinkedSoftwaresBilibili;
                target.RemoveAll(item => string.Equals(item.Path, selected.Path, System.StringComparison.OrdinalIgnoreCase));
                ConfigHelper.Save(c);
                ReloadLinkedSoftwareList();
            };

            page.Controls.AddRange(new Control[] { btnAdd, btnRemove });
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
                Size = new Size(360, 1),
                Location = new Point(0, 26),
                BackColor = Color.FromArgb(220, 220, 220)
            });
        }

        private void BuildPageSkland()
        {
            _pageSkland = new Panel { Dock = DockStyle.Fill, Visible = false };
            var cfg = ConfigHelper.Load();
            string tokenText = cfg.SklandToken;
            bool tokenVisible = cfg.ShowSklandToken;

            AddTitle(_pageSkland, "森空岛签到");

            _pageSkland.Controls.Add(new Label
            {
                Text = "SKYLAND_TOKEN：",
                AutoSize = true,
                Location = new Point(0, 50),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            });

            var txtToken = new TextBox
            {
                Text = tokenVisible ? tokenText : MaskTokenText(tokenText),
                ReadOnly = !tokenVisible,
                Size = new Size(338, 50),
                Location = new Point(0, 68),
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            txtToken.TextChanged += (_, __) =>
            {
                if (!tokenVisible) return;
                tokenText = txtToken.Text;
                var c = ConfigHelper.Load();
                c.SklandToken = tokenText;
                ConfigHelper.Save(c);
            };

            var btnToggleToken = new Button
            {
                Text = tokenVisible ? "🙈" : "👁",
                Size = new Size(36, 26),
                Location = new Point(344, 68),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9f)
            };

            btnToggleToken.Click += (_, __) =>
            {
                if (tokenVisible)
                    tokenText = txtToken.Text;

                tokenVisible = !tokenVisible;
                txtToken.ReadOnly = !tokenVisible;
                txtToken.Text = tokenVisible ? tokenText : MaskTokenText(tokenText);
                btnToggleToken.Text = tokenVisible ? "🙈" : "👁";

                var c = ConfigHelper.Load();
                c.SklandToken = tokenText;
                c.ShowSklandToken = tokenVisible;
                ConfigHelper.Save(c);
            };

            void SaveTokenText()
            {
                var c = ConfigHelper.Load();
                c.SklandToken = tokenText;
                ConfigHelper.Save(c);
            }

            void AppendToken(string token)
            {
                if (string.IsNullOrWhiteSpace(token))
                    return;

                var tokens = tokenText
                    .Replace("\r", "\n")
                    .Replace(';', '\n')
                    .Replace(',', '\n')
                    .Split('\n', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries)
                    .ToList();

                if (!tokens.Any(item => string.Equals(item, token, System.StringComparison.Ordinal)))
                    tokens.Add(token);

                tokenText = string.Join(System.Environment.NewLine, tokens);
                txtToken.Text = tokenVisible ? tokenText : MaskTokenText(tokenText);
                SaveTokenText();
            }

            var btnQrLogin = new Button
            {
                Text = "扫码获取",
                Size = new Size(82, 28),
                Location = new Point(0, 128),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9f)
            };

            btnQrLogin.Click += (_, __) =>
            {
                using var form = new SklandQrLoginForm();
                if (form.ShowDialog(this) == DialogResult.OK)
                    AppendToken(form.Token);
            };

            var txtPhone = new TextBox
            {
                Size = new Size(116, 24),
                Location = new Point(0, 166),
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "手机号"
            };

            var txtCode = new TextBox
            {
                Size = new Size(82, 24),
                Location = new Point(124, 166),
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "验证码"
            };

            var btnSendCode = new Button
            {
                Text = "发送验证码",
                Size = new Size(88, 26),
                Location = new Point(214, 165),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };

            var btnCodeLogin = new Button
            {
                Text = "验证码登录",
                Size = new Size(88, 26),
                Location = new Point(310, 165),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };

            var txtPassword = new TextBox
            {
                Size = new Size(206, 24),
                Location = new Point(0, 200),
                Font = new Font("Segoe UI", 9f),
                PlaceholderText = "密码",
                UseSystemPasswordChar = true
            };

            var btnPasswordLogin = new Button
            {
                Text = "账号密码登录",
                Size = new Size(98, 26),
                Location = new Point(214, 199),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };

            var btnSign = new Button
            {
                Text = "立即签到",
                Size = new Size(82, 28),
                Location = new Point(92, 128),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 9f)
            };

            var chkAutoSign = new CheckBox
            {
                Text = "启动时签到",
                AutoSize = true,
                Location = new Point(188, 132),
                Font = new Font("Segoe UI", 9f),
                Checked = cfg.AutoSklandSignOnStartup,
                Cursor = Cursors.Hand
            };
            chkAutoSign.CheckedChanged += (_, __) =>
            {
                var c = ConfigHelper.Load();
                c.AutoSklandSignOnStartup = chkAutoSign.Checked;
                ConfigHelper.Save(c);
            };

            var txtResult = new TextBox
            {
                Size = new Size(380, 86),
                Location = new Point(0, 240),
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical
            };

            btnSendCode.Click += async (_, __) =>
            {
                btnSendCode.Enabled = false;
                txtResult.Text = "正在发送验证码...";
                try
                {
                    await SklandSignHelper.SendPhoneCodeAsync(txtPhone.Text.Trim());
                    txtResult.Text = "验证码已发送";
                }
                catch (System.Exception ex)
                {
                    txtResult.Text = ex.Message;
                }
                finally
                {
                    btnSendCode.Enabled = true;
                }
            };

            btnCodeLogin.Click += async (_, __) =>
            {
                btnCodeLogin.Enabled = false;
                txtResult.Text = "正在通过验证码获取 token...";
                try
                {
                    AppendToken(await SklandSignHelper.LoginByPhoneCodeAsync(txtPhone.Text.Trim(), txtCode.Text.Trim()));
                    txtResult.Text = "token 获取成功";
                }
                catch (System.Exception ex)
                {
                    txtResult.Text = ex.Message;
                }
                finally
                {
                    btnCodeLogin.Enabled = true;
                }
            };

            btnPasswordLogin.Click += async (_, __) =>
            {
                btnPasswordLogin.Enabled = false;
                txtResult.Text = "正在通过账号密码获取 token...";
                try
                {
                    AppendToken(await SklandSignHelper.LoginByPasswordAsync(txtPhone.Text.Trim(), txtPassword.Text));
                    txtResult.Text = "token 获取成功";
                }
                catch (System.Exception ex)
                {
                    txtResult.Text = ex.Message;
                }
                finally
                {
                    btnPasswordLogin.Enabled = true;
                }
            };

            btnSign.Click += async (_, __) =>
            {
                btnSign.Enabled = false;
                txtResult.Text = "正在签到...";
                try
                {
                    if (tokenVisible)
                        tokenText = txtToken.Text;
                    txtResult.Text = await SklandSignHelper.SignAsync(tokenText);
                }
                catch (System.Exception ex)
                {
                    txtResult.Text = ex.Message;
                }
                finally
                {
                    btnSign.Enabled = true;
                }
            };

            _pageSkland.Controls.AddRange(new Control[]
            {
                txtToken, btnToggleToken, btnQrLogin, btnSign, chkAutoSign, txtPhone, txtCode,
                btnSendCode, btnCodeLogin, txtPassword, btnPasswordLogin, txtResult
            });
            _contentPanel.Controls.Add(_pageSkland);
        }

        private static string MaskTokenText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "";

            var lines = value
                .Replace("\r", "\n")
                .Split('\n')
                .Select(line =>
                {
                    string token = line.Trim();
                    if (token.Length == 0) return "";
                    if (token.Length <= 8) return new string('●', token.Length);
                    return token.Substring(0, 4) + new string('●', 8) + token.Substring(token.Length - 4);
                });

            return string.Join(System.Environment.NewLine, lines);
        }

        private void AddTextRow(Panel page, string label, string currentValue, int y,
            System.Action<string> onSave)
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
                Size = new Size(232, 24),
                Location = new Point(120, y),
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                MaxLength = 10
            };
            txt.TextChanged += (_, __) => onSave(txt.Text);
            page.Controls.Add(txt);
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
                Location = new Point(120, y),
                Font = new Font("Segoe UI", 9f),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(txt);

            var btn = new Button
            {
                Text = "浏览...",
                Size = new Size(52, 24),
                Location = new Point(320, y),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System,
                Font = new Font("Segoe UI", 8.5f)
            };
            btn.Click += (_, __) =>
            {
                string newPath = isFolder ? DialogHelper.SelectFolder() : DialogHelper.SelectExe(label, "可执行文件");
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
