using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Forms;
using System.Drawing;
using System.Threading.Tasks;
using System.Diagnostics;


class Program
{
    static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
            "ArknightsLauncher");

    static readonly string ConfigFile =
        Path.Combine(ConfigDir, "config.json");


    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new LaunchForm());
    }

    public static string SelectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "请选择目标根目录",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false
        };

        return dialog.ShowDialog() == DialogResult.OK
            ? dialog.SelectedPath
            : "";
    }
    public static string LoadRootPath()
    {
        if (!File.Exists(ConfigFile))
            return "";

        try
        {
            var json = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<AppConfig>(json)?.RootPath ?? "";
        }
        catch
        {
            return "";
        }
    }

    public static void SaveRootPath(string path)
    {
        Directory.CreateDirectory(ConfigDir);
        var json = JsonSerializer.Serialize(
            new AppConfig { RootPath = path },
            new JsonSerializerOptions { WriteIndented = true });

        File.WriteAllText(ConfigFile, json);
    }
    public static void ExtractAndOverwrite(string targetRoot)
    {
        var asm = Assembly.GetExecutingAssembly();

        foreach (var res in asm.GetManifestResourceNames())
        {
            if (!res.StartsWith("Payload/"))
                continue;

            string relative = res.Substring("Payload/".Length)
                                 .Replace('/', Path.DirectorySeparatorChar);

            string outputPath = Path.Combine(targetRoot, relative);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            using var input = asm.GetManifestResourceStream(res)!;
            using var output = new FileStream(outputPath, FileMode.Create);

            input.CopyTo(output);
        }
    }
    public static void StartArknights(string rootPath)
    {
        string exePath = Path.Combine(rootPath, "Arknights.exe");

        if (!File.Exists(exePath))
        {
            MessageBox.Show("未找到 Arknights.exe");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = rootPath,
            UseShellExecute = true
        });
    }



    class AppConfig
    {
        public string RootPath { get; set; } = "";
    }

    class LaunchForm : Form
    {
        public LaunchForm()
        {
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            Text = "Arknights Launcher(官服)";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 320;
            Height = 140;
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

        private async void RunCore()
        {
            try
            {
                string rootPath = Program.LoadRootPath();

                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                {
                    using var dialog = new FolderBrowserDialog
                    {
                        Description = "请选择目标根目录",
                        UseDescriptionForTitle = true,
                        ShowNewFolderButton = false
                    };

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                    {
                        Close();
                        return;
                    }

                    rootPath = dialog.SelectedPath;
                    Program.SaveRootPath(rootPath);
                }

                //替换文件
                await Task.Run(() =>
                {
                    Program.ExtractAndOverwrite(rootPath);
                });

                //启动游戏
                Program.StartArknights(rootPath);

                //等待5秒以确保游戏启动后再关闭启动器窗口
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "启动失败");
            }
            finally
            {
                Close();
            }
            string iconPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Arknights.ico"
);

            if (File.Exists(iconPath))
            {
                this.Icon = new Icon(iconPath);
            }

        }
    }
}



