using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
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

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

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
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择目标根目录",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.SelectedPath : "";
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


    public class AppConfig
    {
        public string RootPath { get; set; } = "";     // Arknights
        public string MAA_Official { get; set; } = ""; //MAA 官
        public string MAA_Bilibili { get; set; } = ""; //MAA B
    }


    class LaunchForm : Form
    {
        private readonly ServerType _serverType;

        public LaunchForm(ServerType serverType)
        {
            _serverType = serverType;

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

            Controls.Add(new Label
            {
                Text = serverType == ServerType.MAA_Official ||
                   serverType == ServerType.MAA_Bilibili
                ? "正在启动 MAA…"
                : "正在启动 Arknights…",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 11)
            });

            Shown += (_, __) => BeginInvoke(RunCore);
        }

        async void RunCore()
        {
            try
            {
                var cfg = Program.LoadConfig();

                // ===== MAA 分支 =====
                if (_serverType == ServerType.MAA_Official ||
                    _serverType == ServerType.MAA_Bilibili)
                {
                    string exePath = _serverType == ServerType.MAA_Official
                        ? cfg.MAA_Official
                        : cfg.MAA_Bilibili;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        exePath = Program.SelectExe("请选择 MAA.exe", "MAA 程序");
                        if (string.IsNullOrEmpty(exePath)) return;

                        if (_serverType == ServerType.MAA_Official)
                            cfg.MAA_Official = exePath;
                        else
                            cfg.MAA_Bilibili = exePath;

                        Program.SaveConfig(cfg);
                    }

                    Program.StartMAA(exePath);
                    await Task.Delay(2000);
                    return;
                }

                // ===== Arknights 分支 =====
                string rootPath = cfg.RootPath;

                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    rootPath = Program.SelectFolder();
                    if (string.IsNullOrEmpty(rootPath)) return;

                    cfg.RootPath = rootPath;
                    Program.SaveConfig(cfg);
                }

                string payloadFolder = _serverType == ServerType.Official
                    ? "Payload"
                    : "Payload_B";

                await Task.Run(() =>
                    Program.ExtractAndOverwrite(rootPath, payloadFolder));

                Program.StartArknights(rootPath);
                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "启动失败");
            }
            finally
            {
                Close();
            }
        }


    }

    class ServerSelectForm : Form
    {
        public ServerType SelectedServer { get; private set; }

        public ServerSelectForm()
        {
            Text = "Arknights Launcher";
            Icon = Program.LoadIcon("ArknightsLauncher.ico");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(320, 220);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(CreateServerButton("官服", Program.LoadIcon("official.ico"), new Point(20, 25), ServerType.Official));   // 官服
            Controls.Add(CreateServerButton("B服", Program.LoadIcon("bserver.ico"), new Point(20, 95), ServerType.Bilibili));     // B服
            Controls.Add(CreateServerButton("MAA-官", Program.LoadIcon("MAA.ico"), new Point(180, 25), ServerType.MAA_Official)); // MAA-官
            Controls.Add(CreateServerButton("MAA-B", Program.LoadIcon("MAA.ico"), new Point(180, 95), ServerType.MAA_Bilibili)); //  MAA-B
            Controls.Add(CreateServerButton_Git("By:Tinch", Program.LoadIcon("GitHub.ico"), new Point(100, 165) )); // Github
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

            btn.Click += (_, __) =>
            {
                SelectedServer = type;
                DialogResult = DialogResult.OK;
                Close();
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
}
