using System.Windows.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher.Forms
{
    public partial class ServerSelectForm
    {
        private void InitTray()
        {
            var cfg = ConfigHelper.Load();

            _trayIcon = new NotifyIcon
            {
                Icon = ResourceHelper.LoadIcon("ArknightsLauncher.ico"),
                Text = "Arknights Launcher",
                Visible = cfg.ShowTrayIcon
            };

            var ctxMenu = new ContextMenuStrip();

            var launchItem = new ToolStripMenuItem("启动游戏");

            var officialItem = new ToolStripMenuItem("官服");
            officialItem.Click += (_, __) =>
            {
                this.Show(); this.WindowState = FormWindowState.Normal; this.Activate();
                _officialBtn.PerformClick();
            };

            var bServerItem = new ToolStripMenuItem("B服");
            bServerItem.Enabled = _bServerBtn.Enabled;
            bServerItem.Click += (_, __) =>
            {
                this.Show(); this.WindowState = FormWindowState.Normal; this.Activate();
                _bServerBtn.PerformClick();
            };

            launchItem.DropDownItems.Add(officialItem);
            launchItem.DropDownItems.Add(bServerItem);
            ctxMenu.Items.Add(launchItem);
            ctxMenu.Items.Add("显示", null, (_, __) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); });
            ctxMenu.Items.Add("退出", null, (_, __) => { _forceClose = true; Application.Exit(); });

            _trayIcon.ContextMenuStrip = ctxMenu;
            _trayIcon.DoubleClick += (_, __) => { this.Show(); this.WindowState = FormWindowState.Normal; this.Activate(); };

            this.Resize += (_, __) =>
            {
                var c = ConfigHelper.Load();
                if (c.ShowTrayIcon && c.MinimizeToTray && this.WindowState == FormWindowState.Minimized)
                    this.Hide();
            };

            this.FormClosed += (_, __) => { _trayIcon.Visible = false; _trayIcon.Dispose(); };
        }
    }
}
