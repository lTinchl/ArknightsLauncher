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
    Bilibili  // B服
}

class Program
{
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

    public static void ExtractAndOverwrite(string targetRoot, string payloadFolder)
    {
        var asm = Assembly.GetExecutingAssembly();
        string ns = asm.GetName().Name; // 程序集命名空间
        string prefix = ns + "." + payloadFolder + ".";

        var resources = asm.GetManifestResourceNames().Where(r => r.StartsWith(prefix)).ToList();
        if (!resources.Any()) throw new Exception($"没有找到资源: {prefix}");

        foreach (var res in resources)
        {
            // 修正相对路径，保留扩展名
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

    class AppConfig { public string RootPath { get; set; } = ""; }

    class LaunchForm : Form
    {
        private readonly ServerType _serverType;

        public LaunchForm(ServerType serverType)
        {
            _serverType = serverType;

            Text = serverType == ServerType.Official ? "Arknights Launcher(官服)" : "Arknights Launcher(B服)";
            Icon = Program.LoadIcon(serverType == ServerType.Official ? "official.ico" : "bserver.ico");

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(320, 140);
            TopMost = true;

            Controls.Add(new Label
            {
                Text = "正在启动 Arknights…",
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
                string rootPath = Program.LoadRootPath();
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    rootPath = Program.SelectFolder();
                    if (string.IsNullOrEmpty(rootPath)) return;
                    Program.SaveRootPath(rootPath);
                }

                string payloadFolder = _serverType == ServerType.Official ? "Payload" : "Payload_B";

                await Task.Run(() =>
                {
                    Program.ExtractAndOverwrite(rootPath, payloadFolder);
                });

                Program.StartArknights(rootPath);
                await Task.Delay(5000); // 等待启动
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
            ClientSize = new Size(320, 180);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;

            Controls.Add(CreateServerButton("官服", Program.LoadIcon("official.ico"), new Point(20, 25), ServerType.Official));
            Controls.Add(CreateServerButton("B服", Program.LoadIcon("bserver.ico"), new Point(20, 95), ServerType.Bilibili));
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
                Size = new Size(280, 60),
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
    }
}
