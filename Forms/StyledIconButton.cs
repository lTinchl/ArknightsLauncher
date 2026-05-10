using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ArknightsLauncher.Forms
{
    internal class StyledIconButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public int CornerRadius { get; set; } = 8;
        public Color BorderColor { get; set; } = Color.FromArgb(212, 219, 229);
        public Color HoverBackColor { get; set; } = Color.FromArgb(238, 244, 252);
        public Color PressedBackColor { get; set; } = Color.FromArgb(224, 236, 249);
        public Color DisabledBackColor { get; set; } = Color.FromArgb(239, 242, 246);
        public Color DisabledForeColor { get; set; } = Color.FromArgb(145, 153, 165);
        public int IconSize { get; set; } = 38;

        public StyledIconButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            BackColor = Color.White;
            ForeColor = Color.FromArgb(24, 34, 48);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent?.BackColor ?? SystemColors.Control);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = CreateRoundRect(rect, CornerRadius);
            Color fill = !Enabled ? DisabledBackColor : _pressed ? PressedBackColor : _hover ? HoverBackColor : BackColor;
            Color textColor = Enabled ? ForeColor : DisabledForeColor;

            using (var brush = new SolidBrush(fill))
                e.Graphics.FillPath(brush, path);
            using (var pen = new Pen(BorderColor))
                e.Graphics.DrawPath(pen, path);

            int contentLeft = 16;
            int iconTop = (Height - IconSize) / 2;
            if (Image != null)
            {
                var imageRect = new Rectangle(contentLeft, iconTop, IconSize, IconSize);
                e.Graphics.DrawImage(Image, imageRect);
                contentLeft += IconSize + 12;
            }

            var textRect = new Rectangle(contentLeft, 0, Width - contentLeft - 14, Height);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textRect,
                textColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);

            if (Focused && ShowFocusCues)
            {
                var focusRect = Rectangle.Inflate(rect, -4, -4);
                ControlPaint.DrawFocusRectangle(e.Graphics, focusRect);
            }
        }

        private static GraphicsPath CreateRoundRect(Rectangle rect, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(rect.Left, rect.Top, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Top, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.Left, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
