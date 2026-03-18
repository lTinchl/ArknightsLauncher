using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using ArknightsLauncher.Helpers;

namespace ArknightsLauncher.Forms
{
    public class ImageMessageBox : Form
    {
        public ImageMessageBox(string message, Image image, string title = "提示")
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            int padding = 20;
            int btnHeight = 40;
            int targetWidth = 500;
            int targetHeight = 300;
            Image resizedImage = ResizeImage(image, targetWidth, targetHeight);

            ClientSize = new Size(
                System.Math.Max(resizedImage.Width + padding * 2, 300),
                resizedImage.Height + 70 + btnHeight);

            var picBox = new PictureBox
            {
                Image = resizedImage,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(resizedImage.Width, resizedImage.Height),
                Location = new Point((ClientSize.Width - resizedImage.Width) / 2, padding)
            };
            Controls.Add(picBox);

            var lbl = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10),
                Size = new Size(ClientSize.Width - padding * 2, 50),
                Location = new Point(padding, picBox.Bottom + 10)
            };
            Controls.Add(lbl);

            var btn = new Button
            {
                Text = "确定",
                Size = new Size(80, 30),
                Location = new Point((ClientSize.Width - 80) / 2, lbl.Bottom + 1),
                DialogResult = DialogResult.OK
            };
            Controls.Add(btn);
            AcceptButton = btn;
        }

        private static Image ResizeImage(Image img, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);
            destImage.SetResolution(img.HorizontalResolution, img.VerticalResolution);

            using var graphics = Graphics.FromImage(destImage);
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            using var wrapMode = new ImageAttributes();
            wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
            graphics.DrawImage(img, destRect, 0, 0, img.Width, img.Height, GraphicsUnit.Pixel, wrapMode);

            return destImage;
        }

        public static void Show(string message, string resourceName, string title = "提示")
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream($"ArknightsLauncher.Icons.{resourceName}");
            if (stream == null) { MessageBox.Show("未找到资源: " + resourceName); return; }

            Image img = Image.FromStream(stream);
            using var box = new ImageMessageBox(message, img, title);
            box.ShowDialog();
        }
    }
}
