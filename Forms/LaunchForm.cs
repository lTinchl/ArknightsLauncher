using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;

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
                ServerType.Official    => "Arknights Launcher(官服)",
                ServerType.Bilibili    => "Arknights Launcher(B服)",
                ServerType.MAA_Official => "MAA(官服)",
                ServerType.MAA_Bilibili => "MAA(B服)",
                _                      => "Launcher"
            };

            Icon = serverType switch
            {
                ServerType.Official    => ResourceHelper.LoadIcon("official.ico"),
                ServerType.Bilibili    => ResourceHelper.LoadIcon("bserver.ico"),
                ServerType.MAA_Official => ResourceHelper.LoadIcon("MAA.ico"),
                ServerType.MAA_Bilibili => ResourceHelper.LoadIcon("MAA.ico"),
                _                      => SystemIcons.Application
            };

            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(320, 140);
            TopMost = true;

            statusLabel = new Label
            {
                Text = (_serverType == ServerType.MAA_Official || _serverType == ServerType.MAA_Bilibili)
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
                var cfg = ConfigHelper.Load();

                if (_serverType == ServerType.MAA_Official || _serverType == ServerType.MAA_Bilibili)
                {
                    string exePath = _serverType == ServerType.MAA_Official ? cfg.MAA_Official : cfg.MAA_Bilibili;

                    if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                    {
                        exePath = (string)this.Invoke((Func<string>)(() =>
                            DialogHelper.SelectExe("请选择 MAA.exe", "MAA 程序")));

                        if (string.IsNullOrEmpty(exePath)) return;

                        if (_serverType == ServerType.MAA_Official) cfg.MAA_Official = exePath;
                        else cfg.MAA_Bilibili = exePath;
                        ConfigHelper.Save(cfg);
                    }

                    GameLauncher.StartMAA(exePath);
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
    }
}
