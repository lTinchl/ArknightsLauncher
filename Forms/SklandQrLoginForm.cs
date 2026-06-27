using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher.Forms
{
    public class SklandQrLoginForm : Form
    {
        private readonly PictureBox _qrBox;
        private readonly TextBox _scanUrlText;
        private readonly Label _statusLabel;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public string Token { get; private set; } = "";

        public SklandQrLoginForm()
        {
            Text = "森空岛扫码登录";
            Size = new Size(360, 430);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(250, 250, 250);

            _statusLabel = new Label
            {
                Text = "正在创建扫码登录...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(18, 16),
                Size = new Size(306, 24),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(60, 60, 60)
            };

            _qrBox = new PictureBox
            {
                Location = new Point(60, 52),
                Size = new Size(220, 220),
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };

            _scanUrlText = new TextBox
            {
                Location = new Point(18, 286),
                Size = new Size(306, 48),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 8.5f)
            };

            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(232, 348),
                Size = new Size(92, 30),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.System
            };
            btnCancel.Click += (_, __) => Close();

            Controls.AddRange(new Control[] { _statusLabel, _qrBox, _scanUrlText, btnCancel });
            Shown += async (_, __) => await StartLoginAsync();
            FormClosing += (_, __) => _cts.Cancel();
        }

        private async Task StartLoginAsync()
        {
            try
            {
                var login = await SklandSignHelper.CreateScanLoginAsync();
                _scanUrlText.Text = login.ScanUrl;
                string qrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=320x320&data="
                    + Uri.EscapeDataString(login.ScanUrl);
                _qrBox.LoadAsync(qrUrl);

                await PollTokenAsync(login.ScanId, _cts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _statusLabel.Text = ex.Message;
            }
        }

        private async Task PollTokenAsync(string scanId, CancellationToken cancellationToken)
        {
            DateTime deadline = DateTime.Now.AddSeconds(180);
            while (DateTime.Now < deadline)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    Token = await SklandSignHelper.QueryScanLoginTokenAsync(scanId);
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }
                catch (SklandScanPendingException ex)
                {
                    _statusLabel.Text = ex.Message;
                }

                await Task.Delay(2000, cancellationToken);
            }

            _statusLabel.Text = "扫码登录超时，请重新打开扫码登录";
        }
    }
}
