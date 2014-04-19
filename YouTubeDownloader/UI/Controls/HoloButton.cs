using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace YouTubeDownloader.UI.Controls
{
    sealed class HoloButton : Control
    {
        #region Createround

            public GraphicsPath CreateRound(int x, int y, int width, int height, int radius)
            {
                Rectangle r = new Rectangle(x, y, width, height);
                return CreateRound(r, radius);
            }

            public GraphicsPath CreateRound(Rectangle r, int radius)
            {
                GraphicsPath CreateRoundPath = new GraphicsPath(FillMode.Winding);
                CreateRoundPath.AddArc(r.X, r.Y, radius, radius, 180F, 90F);
                CreateRoundPath.AddArc(r.Right - radius, r.Y, radius, radius, 270.0F, 90.0F);
                CreateRoundPath.AddArc(r.Right - radius, r.Bottom - radius, radius, radius, 0.0F, 90.0F);
                CreateRoundPath.AddArc(r.X, r.Bottom - radius, radius, radius, 90.0F, 90.0F);
                CreateRoundPath.CloseFigure();
                return CreateRoundPath;
            }

        #endregion

        private readonly Timer tmrAnimation;
        private double AnimationValue;
        private MouseStates _MouseState = MouseStates.None;

        public Image Image { get; set; }
        public Size ImageSize { get; set; }

        private enum MouseStates
        {
            None = 0,
            Over = 1,
            Down = 2
        }

        public HoloButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Font = new Font("Segoe UI", 10);
            AnimationValue = 0.0;

            tmrAnimation = new Timer { Interval = 7, Enabled = true };
            tmrAnimation.Tick += tmrAnimation_Tick;
        }


        protected override void OnMouseEnter(EventArgs e)
        {
            _MouseState = MouseStates.Over;
            tmrAnimation.Start();
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _MouseState = MouseStates.None;
            tmrAnimation.Start();
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _MouseState = MouseStates.Down;
            tmrAnimation.Start();
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            _MouseState = MouseStates.Over;
            tmrAnimation.Start();
            Invalidate();
            base.OnMouseUp(e);
        }

        private void tmrAnimation_Tick(object sender, EventArgs e)
        {
            switch (_MouseState)
            {
                case MouseStates.None:
                    if (AnimationValue > 0)
                    {
                        AnimationValue -= 0.1;
                    }
                    else
                    {
                        AnimationValue = 0.0;
                        tmrAnimation.Stop();
                    }
                    break;
                case MouseStates.Over:
                    if (AnimationValue < 0.7)
                    {
                        AnimationValue += 0.1;
                    }
                    else
                    {
                        if (AnimationValue > 0.7 & Math.Abs(AnimationValue - 0.7) > 0.1)
                        {
                            AnimationValue -= 0.1;
                        }
                        else
                        {
                            AnimationValue = 0.7;
                            tmrAnimation.Stop();
                        }
                    }
                    break;
                case MouseStates.Down:
                    if (AnimationValue < 1.8)
                    {
                        AnimationValue += 0.1;
                    }
                    else
                    {
                        AnimationValue = 1.8;
                        tmrAnimation.Stop();
                    }
                    break;
            }
            if (AnimationValue < 0)
                AnimationValue = 0;
            Invalidate();
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            if (AnimationValue < 0) AnimationValue = 0;
            
            e.Graphics.Clear(BackColor);
            e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

            GraphicsPath buttonPath = CreateRound(0, 0, Width - 1, Height - 1, 3);

            e.Graphics.FillPath(Brushes.WhiteSmoke, buttonPath);
            e.Graphics.FillPath(new SolidBrush(Color.FromArgb(Convert.ToInt32(25 * AnimationValue), Color.Black)), buttonPath);
            e.Graphics.DrawPath(new Pen(Color.FromArgb(Convert.ToInt32(20 * AnimationValue), Color.Black)), buttonPath);
            e.Graphics.DrawLine(new Pen(Color.FromArgb((int)(60 + AnimationValue * 15), Color.Black), 2), new Point(0, Height - 1), new Point(Width, Height - 1));

            if (Image != null)
            {
                e.Graphics.DrawImage(Image, new Rectangle(new Point(7, 7), ImageSize));
                e.Graphics.DrawString(Text, Font, Brushes.Black, new Rectangle(ImageSize.Width + 15, 5, Width - ImageSize.Width - 22, Height - 11), new StringFormat {LineAlignment = StringAlignment.Center});
            }
            else
            {
                e.Graphics.DrawString(Text, Font, Brushes.Black, new Rectangle(10, 5, Width - 21, Height - 11), new StringFormat { LineAlignment = StringAlignment.Center });
            }

            base.OnPaint(e);
        }

    }
}
