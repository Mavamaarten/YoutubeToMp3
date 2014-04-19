using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace YouTubeDownloader.UI.Controls
{
    sealed class IndeterminateProgressbar : Control
    {
        private readonly List<int> positions = new List<int>();
        private readonly Timer tmrAnimation = new Timer {Interval = 5, Enabled = false};
        private readonly Timer tmrAddPosition = new Timer {Interval = 500, Enabled = true};

        public Color ProgressColor { get; set; }
        public Color InactiveColor { get; set; }

        public IndeterminateProgressbar()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            ProgressColor = Color.FromArgb(40, 190, 245);
            InactiveColor = Color.FromArgb(40, 40, 40);
            tmrAnimation.Tick += tmrAnimation_Tick;
            tmrAddPosition.Tick += tmrAddPosition_Tick;
            if (!DesignMode) tmrAnimation.Start();
        }

        void tmrAddPosition_Tick(object sender, EventArgs e)
        {
            positions.Add(1);
        }

        void tmrAnimation_Tick(object sender, EventArgs e)
        {
            if (DesignMode) tmrAnimation.Stop();
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] += 2 + Math.Abs(positions[i]) / 50;
                if (positions[i] > Width) positions.RemoveAt(i);
            }
            Invalidate();
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            if (Enabled)
            {
                positions.Clear();
                positions.AddRange(new[] { Width / 10, Width / 3, Width / 2, (int)(Width * 0.7) });
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled)
            {
                e.Graphics.Clear(BackColor);
                foreach (int i in positions)
                {
                    e.Graphics.DrawLine(new Pen(Brushes.Black, 4f), i, 0, i, Height);
                }
            }
            else e.Graphics.Clear(InactiveColor);
            
            base.OnPaint(e);
        }
    }
}
