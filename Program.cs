using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

enum ServerType
{
    Official, // 官服
    Bilibili,  // B服
    MAA_Official,  // MAA 官
    MAA_Bilibili,  // MAA B
    GitHub
}



class Program
{
    const string GitHubUrl = "https://github.com/lTinchl/ArknightsLauncher";

    static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                     "ArknightsLauncher");

    static readonly string ConfigFile = Path.Combine(ConfigDir, "config.json");

    static readonly string AccountBackupDir = Path.Combine(ConfigDir, "AccountBackups");

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Directory.CreateDirectory(AccountBackupDir);

        using var selectForm = new ServerSelectForm();
        if (selectForm.ShowDialog() != DialogResult.OK) return;

        Application.Run(new LaunchForm(selectForm.SelectedServer));
    }



    public static Icon LoadIcon(string name)
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{name}");
        return stream != null ? new Icon(stream) : SystemIcons.Application;
    }

    public static string SelectFolder()
    {
        while (true)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "请选择 Arknights 根目录",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };

            if (dialog.ShowDialog() != DialogResult.OK)
                return ""; // 用户取消

            string selectedPath = dialog.SelectedPath;
            string folderName = Path.GetFileName(selectedPath.TrimEnd(Path.DirectorySeparatorChar));

            if (folderName.Equals("Arknights Game", StringComparison.OrdinalIgnoreCase))
            {
                return selectedPath; // 选择正确，返回路径
            }
            else
            {
                MessageBox.Show("请选择名为 'Arknights Game' 的文件夹作为根目录", "错误");
                // 循环再次弹出选择框
            }
        }
    }

    public static string SelectExe(string title, string filterName)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = "MAA.exe|MAA.exe",
            CheckFileExists = true,
            CheckPathExists = true,
            Multiselect = false
        };

        return dialog.ShowDialog() == DialogResult.OK
            ? dialog.FileName
            : "";
    }


    public static string LoadRootPath()
    {
        if (!File.Exists(ConfigFile)) return "";
        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json)?.RootPath ?? "";
        }
        catch { return ""; }
    }



    public static void SaveRootPath(string path)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(new AppConfig { RootPath = path },
                                            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigFile, json);
    }

    public static AppConfig LoadConfig()
    {
        if (!File.Exists(ConfigFile)) return new AppConfig();
        try
        {
            return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigFile))
                   ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public static void SaveConfig(AppConfig cfg)
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigFile,
            JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true }));
    }


    public static void ExtractAndOverwrite(string targetRoot, string payloadFolder)
    {
        var asm = Assembly.GetExecutingAssembly();
        string ns = asm.GetName().Name; // 程序集命名空间
        string prefix = ns + "." + payloadFolder + ".";

        var resources = asm.GetManifestResourceNames().Where(r => r.StartsWith(prefix)).ToList();
        if (!resources.Any()) throw new Exception($"没有找到资源: {prefix}");

        foreach (var res in resources)
        {

            string relativePath = res.Substring(prefix.Length);
            int lastDot = relativePath.LastIndexOf('.');
            if (lastDot != -1)
            {
                string pathWithoutExt = relativePath.Substring(0, lastDot).Replace('.', Path.DirectorySeparatorChar);
                string ext = relativePath.Substring(lastDot);
                relativePath = pathWithoutExt + ext;
            }

            string outPath = Path.Combine(targetRoot, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            using var input = asm.GetManifestResourceStream(res)!;
            using var output = new FileStream(outPath, FileMode.Create);
            input.CopyTo(output);
        }
    }


    public static void StartArknights(string rootPath)
    {
        string exePath = Path.Combine(rootPath, "Arknights.exe");
        if (!File.Exists(exePath)) throw new Exception("未找到 Arknights.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = rootPath,
            UseShellExecute = true
        });
    }

    public static void StartMAA(string exePath)
    {
        if (!File.Exists(exePath))
            throw new Exception("未找到 MAA.exe");

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = Path.GetDirectoryName(exePath)!,
            UseShellExecute = true
        });
    }

    public static void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GitHubUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("无法打开浏览器\n" + ex.Message, "错误");
        }
    }

    public static void BackupAccount(string accountName)
    {
        // 获取 LocalLow 路径
        string sdkPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Hypergryph", "Arknights"
        );

        string target = Path.Combine(AccountBackupDir, accountName);

        var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
        if (sdkDir == null) return;

        if (Directory.Exists(target)) Directory.Delete(target, true);
        CopyDirectory(sdkDir, target);
    }


    public static void RestoreAccount(string accountName)
    {
        string sdkPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            @"Low\Hypergryph\Arknights"
        );
        string backupDir = Path.Combine(AccountBackupDir, accountName);

        var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
        if (sdkDir != null) Directory.Delete(sdkDir, true);

        if (!Directory.Exists(backupDir)) return;

        CopyDirectory(backupDir, sdkPath);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));
        }

        foreach (var file in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            string targetFile = file.Replace(sourceDir, targetDir);
            File.Copy(file, targetFile, true);
        }
    }


    public class AppConfig
    {
        public string RootPath { get; set; } = "";     // Arknights
        public string MAA_Official { get; set; } = ""; //MAA 官
        public string MAA_Bilibili { get; set; } = ""; //MAA B

        public Dictionary<string, string> Accounts { get; set; }
        = new Dictionary<string, string>();

        public string DefaultAccount { get; set; } = "";
    }


    class LaunchForm : Form
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
                ServerType.MAA_Official => "MAA(官服)",
                ServerType.MAA_Bilibili => "MAA(B服)",
                _ => "Launcher"
            };

            Icon = serverType switch
            {
                ServerType.Official => Program.LoadIcon("official.ico"),
                ServerType.Bilibili => Program.LoadIcon("bserver.ico"),
                ServerType.MAA_Official => Program.LoadIcon("MAA.ico"),
                ServerType.MAA_Bilibili => Program.LoadIcon("MAA.ico"),
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
                Text = serverType == ServerType.MAA_Official || serverType == ServerType.MAA_Bilibili
                       ? "正在启动 MAA…"
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
                var cfg = Program.LoadConfig();

                if (_serverType == ServerType.MAA_Official || _serverType == ServerType.MAA_Bilibili)
                {
                    string exePath = _serverType == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        exePath = Program.SelectExe("请选择 MAA.exe", "MAA 程序");
                        if (string.IsNullOrEmpty(exePath)) return;

                        if (_serverType == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                        else cfg.MAA_Bilibili = exePath;

                        Program.SaveConfig(cfg);
                    }

                    Program.StartMAA(exePath);
                    await Task.Delay(1000); // 显示启动动画
                }
                else
                {
                    string rootPath = _rootPath;
                    if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    {
                        rootPath = Program.SelectFolder();
                        if (string.IsNullOrEmpty(rootPath)) return;

                        cfg.RootPath = rootPath;
                        Program.SaveConfig(cfg);
                    }

                    string payloadFolder = _serverType == ServerType.Official ? "Payload" : "Payload_B";
                    await Task.Run(() => Program.ExtractAndOverwrite(rootPath, payloadFolder));

                    Program.StartArknights(rootPath);
                    await Task.Delay(2500); // 等待动画显示
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "启动失败");
            }
            finally
            {
                this.Invoke(() => this.Close()); // 完成后自动关闭启动窗口
            }
        }
    }


    class AccountItem
    {
        public string Id { get; set; }
        public string Remark { get; set; }
        public override string ToString() => Remark;
    }


    class ServerSelectForm : Form
    { 
        private ComboBox officialCombo;
        public ServerType SelectedServer { get; private set; }

        private void ReloadAccounts()
        {
            officialCombo.Items.Clear();
            var cfg = Program.LoadConfig();

            if (cfg.Accounts.Count == 0)
            {
                string defaultId = "A1";
                string defaultRemark = "默认账号";
                cfg.Accounts[defaultId] = defaultRemark;
                cfg.DefaultAccount = defaultId;

                // 创建备份文件夹
                Directory.CreateDirectory(Path.Combine(Program.AccountBackupDir, defaultId));

                Program.SaveConfig(cfg);
            }

            // 默认账号优先
            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
            {
                var defaultItem = new AccountItem
                {
                    Id = cfg.DefaultAccount,
                    Remark = cfg.Accounts[cfg.DefaultAccount] + "⭐"
                };
                officialCombo.Items.Add(defaultItem);
            }

            // 添加剩余账号
            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue;
                officialCombo.Items.Add(new AccountItem
                {
                    Id = acc.Key,
                    Remark = acc.Value
                });
            }

            // 默认选中第一个
            if (officialCombo.Items.Count > 0)
                officialCombo.SelectedIndex = 0;
        }

        public ServerSelectForm()
        {
            Text = "Arknights Launcher";
            Icon = Program.LoadIcon("ArknightsLauncher.ico");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(340, 210);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            officialCombo = new ComboBox
            {
                Location = new Point(125, 36),
                Size = new Size(80, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var cfg = Program.LoadConfig();

            ReloadAccounts();

            // 设置默认账号
            if (!string.IsNullOrEmpty(cfg.DefaultAccount))
                officialCombo.Text = cfg.DefaultAccount;
            Controls.Add(officialCombo);

            var manageBtn = new Button
            {
                Text = "账号管理",
                Size = new Size(120, 30),
                Location = new Point(0, 5),
                Cursor = Cursors.Hand
            };

            manageBtn.Click += (_, __) =>
            {
                using var f = new AccountManagerForm();
                f.ShowDialog();

                ReloadAccounts();
            };

            Controls.Add(manageBtn);


            Controls.Add(CreateServerButton("官服", Program.LoadIcon("official.ico"), new Point(0, 35), ServerType.Official));   // 官服
            Controls.Add(CreateServerButton("B服", Program.LoadIcon("bserver.ico"), new Point(0, 95), ServerType.Bilibili));     // B服
            Controls.Add(CreateServerButton_MAA("MAA-官", Program.LoadIcon("MAA.ico"), new Point(220, 35), ServerType.MAA_Official)); // MAA-官
            Controls.Add(CreateServerButton_MAA("MAA-B", Program.LoadIcon("MAA.ico"), new Point(220, 95), ServerType.MAA_Bilibili)); //  MAA-B
            Controls.Add(CreateServerButton_Git("By:Tinch", Program.LoadIcon("GitHub.ico"), new Point(110, 160))); // Github
        }

    private Button CreateServerButton(string text, Icon icon, Point pos, ServerType type)
    {
        var btn = new Button
        {
            Text = text,
            Image = icon.ToBitmap(),
            ImageAlign = ContentAlignment.MiddleLeft,
            TextAlign = ContentAlignment.MiddleCenter,
            TextImageRelation = TextImageRelation.ImageBeforeText,
            Font = new Font("Segoe UI", 11, FontStyle.Regular),
            Size = new Size(120, 60),
            Location = pos,
            FlatStyle = FlatStyle.Standard,
            Padding = new Padding(10, 0, 0, 0),
            Cursor = Cursors.Hand
        };

        btn.Click += async (_, __) =>
        {
            SelectedServer = type;

            try
            {
                foreach (var proc in Process.GetProcessesByName("Arknights"))
                {
                    proc.Kill();
                    proc.WaitForExit(); // 等待进程真正结束，避免替换文件报错
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("关闭 Arknights 时出错:\n" + ex.Message, "错误");
            }

            var cfg = Program.LoadConfig();
            string rootPath = cfg.RootPath;

            // 如果没有配置或者目录不存在，就弹出选择文件夹
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
            {
                rootPath = Program.SelectFolder();
                if (string.IsNullOrEmpty(rootPath)) return; // 用户取消

                cfg.RootPath = rootPath;
                Program.SaveConfig(cfg);
            }


            // 立即显示启动窗口
            var launchForm = new LaunchForm(type, rootPath);
            launchForm.Show();

            if (type == ServerType.Official)
            {
                var selectedItem = officialCombo.SelectedItem as AccountItem;
                if (selectedItem == null) return;

                string selectedAccount = selectedItem.Id;
                string backupFolder = Path.Combine(Program.AccountBackupDir, selectedAccount);

                string sdkPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Hypergryph", "Arknights"
                );


                // 找到现有 sdk_data_* 文件夹
                var sdkDir = Directory.GetDirectories(sdkPath, "sdk_data_*").FirstOrDefault();
                if (sdkDir == null)
                    {
                        MessageBox.Show("未找到 sdk_data_* 文件夹，请先通过鹰角启动器启动一次 Arknights (官服)", "错误");
                        return;
                    }

                    if (Directory.Exists(backupFolder))
                    {
                        await Task.Delay(3000);              // 等待进程完全清除
                    CopyDirectory(backupFolder, sdkDir); // 复制 A1 备份到 sdk_data_*
                    }
            }

            // 异步执行启动逻辑
            _ = Task.Run(async () =>
            {
                await Task.Delay(4000);         // 等待进程完全清除
                await launchForm.RunCore(); // 在 LaunchForm 里做文件替换 + 启动
                this.Invoke(() => this.Show());  // 回到主窗口
            });
        };

        return btn;
    }

        private Button CreateServerButton_MAA(string text, Icon icon, Point pos, ServerType type)
        {
            var btn = new Button
            {
                Text = text,
                Image = icon.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                Size = new Size(120, 60),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btn.Click += async (_, __) =>
            {
                SelectedServer = type;
                var cfg = Program.LoadConfig();

                // UI 线程选择 MAA.exe
                string exePath = type == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;
                if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                {
                    string title = type == ServerType.MAA_Official ? "请选择 MAA.exe (MAA-官)" : "请选择 MAA.exe (MAA-B)";
                    exePath = Program.SelectExe(title, "MAA 程序");
                    if (string.IsNullOrEmpty(exePath)) return;

                    if (type == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                    else cfg.MAA_Bilibili = exePath;

                    Program.SaveConfig(cfg);
                }

                // 显示启动动画窗口
                var launchForm = new LaunchForm(type);
                launchForm.Show();

                // 异步执行启动逻辑
                _ = Task.Run(async () =>
                {
                    await launchForm.RunCore();
                    this.Invoke(() => this.Show());
                });
            };

            return btn;
        }



        private Button CreateServerButton_Git(string text, Icon icon, Point pos)
        {
            var btn = new Button
            {
                Text = text,
                Image = icon.ToBitmap(),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Size = new Size(120, 40),
                Location = pos,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(10, 0, 0, 0),
                Cursor = Cursors.Hand
            };

            btn.Click += (_, __) =>
            {
                Program.OpenGitHub();
            };

            return btn;
        }
    }
    class AccountManagerForm : Form
    {
        private ListBox listBox;
        private Button btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename;
        private Label hintLabel;

        public AccountManagerForm()
        {
            Text = "账号管理";
            Size = new Size(350, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            hintLabel = new Label
            {
                Text = "双击列表项可重命名",
                Dock = DockStyle.Top,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            Controls.Add(hintLabel);

            listBox = new ListBox { Dock = DockStyle.Top, Height = 250 };
            listBox.DoubleClick += RenameAccount;
            Controls.Add(listBox);

            btnAdd = new Button { Text = "新增", Width = 70 };
            btnDelete = new Button { Text = "删除", Width = 70 };
            btnBackup = new Button { Text = "备份当前", Width = 90 };
            btnSetDefault = new Button { Text = "设为默认", Width = 90 };
            btnRename = new Button { Text = "重命名", Width = 90 };

            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 60 };
            panel.Controls.AddRange(new Control[] { btnAdd, btnDelete, btnBackup, btnSetDefault, btnRename });

            Controls.Add(listBox);
            Controls.Add(panel);

            btnAdd.Click += AddAccount;
            btnDelete.Click += DeleteAccount;
            btnBackup.Click += BackupCurrent;
            btnSetDefault.Click += SetDefault;
            btnRename.Click += RenameAccount;

            LoadAccounts();
        }

        private void LoadAccounts()
        {
            listBox.Items.Clear();
            var cfg = Program.LoadConfig();

            if (!string.IsNullOrEmpty(cfg.DefaultAccount) && cfg.Accounts.ContainsKey(cfg.DefaultAccount))
            {
                var defaultItem = new AccountItem
                {
                    Id = cfg.DefaultAccount,
                    Remark = cfg.Accounts[cfg.DefaultAccount] + " ⭐"
                };
                listBox.Items.Add(defaultItem);
            }

            foreach (var acc in cfg.Accounts)
            {
                if (acc.Key == cfg.DefaultAccount) continue; // 已添加默认账号
                listBox.Items.Add(new AccountItem
                {
                    Id = acc.Key,
                    Remark = acc.Value
                });
            }
        }

        private void AddAccount(object sender, EventArgs e)
        {
            string remark = Microsoft.VisualBasic.Interaction.InputBox("请输入账号备注", "新增账号");
            if (string.IsNullOrWhiteSpace(remark)) return;

            var cfg = Program.LoadConfig();
            int index = 1;
            while (cfg.Accounts.ContainsKey("A" + index)) index++;
            string id = "A" + index;

            cfg.Accounts[id] = remark;
            Program.SaveConfig(cfg);

            Directory.CreateDirectory(Path.Combine(Program.AccountBackupDir, id));
            LoadAccounts();
        }

        private void RenameAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            string newRemark = Microsoft.VisualBasic.Interaction.InputBox($"修改 {selectedItem.Remark.Replace(" ⭐", "")}",
                                                                          "重命名",
                                                                          selectedItem.Remark.Replace(" ⭐", ""));
            if (string.IsNullOrWhiteSpace(newRemark)) return;

            var cfg = Program.LoadConfig();
            cfg.Accounts[selectedItem.Id] = newRemark;
            Program.SaveConfig(cfg);
            LoadAccounts();
        }

        private void DeleteAccount(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;
            var cfg = Program.LoadConfig();

            if (cfg.Accounts.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个账号", "提示");
                return;
            }

            if (MessageBox.Show($"确认删除 {selectedItem.Remark.Replace(" ⭐", "")}？", "确认", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            cfg.Accounts.Remove(selectedItem.Id);
            if (cfg.DefaultAccount == selectedItem.Id) cfg.DefaultAccount = "";
            Program.SaveConfig(cfg);

            string backupDir = Path.Combine(Program.AccountBackupDir, selectedItem.Id);
            if (Directory.Exists(backupDir)) Directory.Delete(backupDir, true);

            LoadAccounts();
        }

        private void SetDefault(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            var cfg = Program.LoadConfig();
            cfg.DefaultAccount = selectedItem.Id;
            Program.SaveConfig(cfg);

            LoadAccounts();
        }

        private void BackupCurrent(object sender, EventArgs e)
        {
            var selectedItem = listBox.SelectedItem as AccountItem;
            if (selectedItem == null) return;

            Program.BackupAccount(selectedItem.Id);
            MessageBox.Show("备份完成", "成功");
        }
    }
}
