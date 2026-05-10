using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "关于";
            Size = new Size(400, 340);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var picBox = new PictureBox
            {
                Image = ResizeBitmap(System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(), 64, 64),
                Size = new Size(64, 64),
                Location = new Point(160, 20),
                SizeMode = PictureBoxSizeMode.StretchImage
            };

            var labelName = new Label
            {
                Text = "Arknights Launcher",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 100)
            };
            labelName.Left = (ClientSize.Width - labelName.PreferredWidth) / 2;

            var labelVersion = new Label
            {
                Text = $"版本 v{AppInfo.CurrentVersion}",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                AutoSize = true,
                Location = new Point(0, 135)
            };
            labelVersion.Left = (ClientSize.Width - labelVersion.PreferredWidth) / 2;

            var labelDesc = new Label
            {
                Text = "By:Tinch",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(0, 160)
            };
            labelDesc.Left = (ClientSize.Width - labelDesc.PreferredWidth) / 2;

            var githubBmp = ResourceHelper.LoadIcon("GitHub.ico").ToBitmap();
            var linkGitHub = new Button
            {
                Text = "GitHub 主页",
                Image = ResizeBitmap(githubBmp, 25, 25),
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleCenter,
                TextImageRelation = TextImageRelation.ImageBeforeText,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.RoyalBlue,
                AutoSize = true,
                Location = new Point(128, 182),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                Padding = new Padding(9, 0, 0, 0)
            };
            linkGitHub.FlatAppearance.BorderSize = 0;
            linkGitHub.FlatAppearance.MouseOverBackColor = Color.Transparent;
            linkGitHub.FlatAppearance.MouseDownBackColor = Color.Transparent;
            linkGitHub.Click += (_, __) => BrowserHelper.OpenGitHub();

            var btnUpdate = new Button
            {
                Text = "检查更新",
                Size = new Size(100, 30),
                Location = new Point(0, 225),
                FlatStyle = FlatStyle.System,
                Cursor = Cursors.Hand
            };
            btnUpdate.Left = (ClientSize.Width - btnUpdate.Width) / 2;

            btnUpdate.Click += async (_, __) =>
            {
                btnUpdate.Enabled = false;
                btnUpdate.Text = "检查中...";
                try
                {
                    var (hasUpdate, latestVersion, downloadUrl) = await UpdateHelper.CheckForUpdateAsync();
                    if (hasUpdate)
                    {
                        var result = MessageBox.Show(
                            $"发现新版本 v{latestVersion}，是否打开 GitHub Releases 手动下载？",
                            "有新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (result == DialogResult.Yes)
                        {
                            BrowserHelper.Open(downloadUrl);
                        }
                    }
                    else
                    {
                        MessageBox.Show("当前已是最新版本！", "检查更新");
                    }
                }
                catch (TaskCanceledException)
                {
                    if (MessageBox.Show("连接超时，是否打开 GitHub Releases 手动下载？", "检查更新失败",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                        BrowserHelper.OpenGitHubReleases();
                }
                catch (HttpRequestException ex)
                {
                    MessageBox.Show($"请求失败：{ex.Message}\n状态码：{ex.StatusCode}", "调试信息");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"检查更新失败：{ex.Message}", "错误");
                }
                finally
                {
                    btnUpdate.Text = "检查更新";
                    btnUpdate.Enabled = true;
                }
            };

            Controls.AddRange(new Control[]
            {
                picBox, labelName, labelVersion, labelDesc, linkGitHub,
                btnUpdate
            });
        }

        private static Bitmap ResizeBitmap(Bitmap bmp, int width, int height)
        {
            var resized = new Bitmap(width, height);
            using var g = Graphics.FromImage(resized);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.DrawImage(bmp, 0, 0, width, height);
            return resized;
        }
    }
}
