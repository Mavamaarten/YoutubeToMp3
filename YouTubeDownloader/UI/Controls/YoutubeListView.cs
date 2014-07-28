using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using YouTube;
using YouTubeDownloader.Properties;

namespace YouTubeDownloader.UI.Controls
{
    public sealed class YoutubeListView : FlowLayoutPanel
    {
        public event ItemClickedEventHandler ItemClicked;
        public delegate void ItemClickedEventHandler(AudioItem Item, int Index, MouseEventArgs mouseEvtArgs);
        private AudioItem _LastClickedAudioItem;

        public YoutubeListView()
        {
            FlowDirection = FlowDirection.TopDown;
            AutoScroll = true;
            BackColor = Color.Gainsboro;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (ContextMenuStrip == null) return;
            ContextMenuStrip.Opening += ContextMenuStrip_Opening;
        }

        void ContextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (((ContextMenuStrip)sender).SourceControl == this) e.Cancel = true;
        }

        public void Sort()
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(Sort));
                return;
            }

            var sortedControls = new List<AudioItem>(Controls.OfType<AudioItem>());
            sortedControls.Sort();
            foreach (var control in sortedControls)
                control.SendToBack();
        }

        public AudioItem AddItem(AudioInformation _Audio)
        {
            AudioItem Item = new AudioItem(_Audio)
            {
                Margin = new Padding(7, 10, 7, 4),
                ContextMenuStrip = ContextMenuStrip,
                DownloadStatus = AudioItem.DownloadStatuses.NotDownloaded
            };
            
            Item.MouseUp += onItemMouseUp;
            Item.MouseDown += onItemMouseDown;

            Invoke(new MethodInvoker(() =>
            {
                Controls.Add(Item);
                OnResize(null);

                Sort();
            }));

            return Item;
        }

        public void RemoveItem(AudioInformation _Audio)
        {
            if (InvokeRequired) Invoke(new MethodInvoker(() => RemoveItem(_Audio)));
            foreach (AudioItem control in Controls.OfType<AudioItem>().Where(control => control._Audio.Equals(_Audio)))
            {
                Controls.Remove(control);
            }
        }

        public AudioItem FindItemByVideo(AudioInformation _Audio)
        {
            if (InvokeRequired) Invoke(new MethodInvoker(() => FindItemByVideo(_Audio)));
            return Controls.OfType<AudioItem>().FirstOrDefault(control => control._Audio.Equals(_Audio));
        }

        public void ClearAllItems()
        {
            Controls.Clear();
        }

        public List<AudioItem> SelectedItems()
        {
            return Controls.OfType<AudioItem>().Where(C => (C).Checked).ToList();
        }

        public List<AudioInformation> SelectedVideos()
        {
            return (from C in Controls.OfType<AudioItem>() where (C).Checked select (C)._Audio).ToList();
        }

        public List<AudioItem> Items()
        {
            return Controls.OfType<AudioItem>().ToList();
        }

        private void onItemMouseUp(object sender, MouseEventArgs e)
        {
            AudioItem audioItem = (AudioItem)sender;

            if (ItemClicked != null) ItemClicked(audioItem, audioItem.Index, e);
            if (audioItem.ContextMenuStrip == null) return;
            if (e.Button == MouseButtons.Right) audioItem.ContextMenuStrip.Show(audioItem, e.Location);
        }

        private void onItemMouseDown(object sender, MouseEventArgs e)
        {
            AudioItem audioItem = (AudioItem)sender;

            if (e.Button != MouseButtons.Left) return;

            if (_LastClickedAudioItem != null && ModifierKeys == Keys.Shift)
            {
                if (_LastClickedAudioItem.Index < audioItem.Index)
                {
                    for (int i = _LastClickedAudioItem.Index; i < audioItem.Index; i++)
                    {
                        ((AudioItem)Controls[i]).Checked = true;
                    }
                }
                else
                {
                    for (int i = _LastClickedAudioItem.Index; i > audioItem.Index; i--)
                    {
                        ((AudioItem)Controls[i]).Checked = true;
                    }
                }
                audioItem.Checked = true;
            }
            else if (ModifierKeys == Keys.Control | (SelectedItems().Count == 1 && SelectedItems().Contains(audioItem)))
            {
                audioItem.Checked = !audioItem.Checked;
            }
            else
            {
                foreach (AudioItem c in Items())
                {
                    c.Checked = false;
                }
                audioItem.Checked = true;
            }

            Invalidate(true);
            _LastClickedAudioItem = audioItem;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            OnClick(e);
            foreach (AudioItem ci in SelectedItems())
            {
                ci.Checked = false;
            }
            Invalidate(true);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            if (Controls.Count == 0) return;
            Control LastControl = Controls[Controls.Count - 1];
            foreach (Control C in Controls)
            {
                if (LastControl.Bottom >= Height - 2)
                {
                    C.Width = Width - C.Margin.Left - C.Margin.Right - 18;
                }
                else
                {
                    C.Width = Width - C.Margin.Right - 7;
                }
            }
            HorizontalScroll.Visible = false;
            base.OnResize(eventargs);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            if (e.Control && e.KeyCode == Keys.A)
            {
                foreach (AudioItem ci in Items())
                {
                    ci.Checked = true;
                }
                Invalidate(true);
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                foreach (AudioItem ci in Items())
                {
                    ci.Checked = false;
                }
                Invalidate(true);
            }
        }

        public sealed class AudioItem : Control, IComparable<AudioItem>
        {

            private enum MouseStates
            {
                None = 0,
                Over = 1,
                Down = 2
            }

            public enum DownloadStatuses
            {
                NotDownloaded = 0,
                Downloading = 1,
                Converting = 2,
                Completed = 3,
                Error = 4
            }

            private MouseStates _MouseState = MouseStates.None;
            private double _AnimationValue;
            private readonly Timer tmrAnimation;
            private float downloadProgress;
            private DownloadStatuses downloadStatus;

            public AudioInformation _Audio { get; private set; }
            public bool Checked { get; set; }
            public float DownloadProgress {
                get { return downloadProgress; }
                set
                {
                    downloadProgress = value;
                    Invalidate();
                }
            }
            public DownloadStatuses DownloadStatus
            {
                get { return downloadStatus; }
                set
                {
                    downloadStatus = value;
                    Invalidate();
                }
            }

            public int Index
            {
                get
                {
                    YoutubeListView youtubeListView = (YoutubeListView)Parent;
                    if (youtubeListView == null) return -1;
                    List<AudioItem> items = youtubeListView.Items();
                    for (int i = 0; i < items.Count; i++)
                    {
                        if (items[i].Equals(this)) return i;
                    }
                    return -1;
                }
            }

            public AudioItem(AudioInformation _Audio)
            {
                this._Audio = _Audio;
                BackColor = Color.FromArgb(60, 60, 60);
                ForeColor = Color.Black;
                Font = new Font("Segoe UI", 10);
                Size = new Size(100, 66);
                MinimumSize = new Size(390, 0);
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
                SetStyle(ControlStyles.Selectable, false);

                tmrAnimation = new Timer
                {
                    Interval = 5,
                    Enabled = false
                };
                tmrAnimation.Tick += tmrAnimation_OnTick;
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                _MouseState = MouseStates.Down;
                tmrAnimation.Start();
                if (e.Button == MouseButtons.Right)
                {
                    YoutubeListView youtubeListView = (YoutubeListView)Parent;
                    if (!youtubeListView.SelectedItems().Contains(this))
                    {
                        foreach (AudioItem ci in youtubeListView.SelectedItems())
                        {
                            ci.Checked = false;
                        }
                        Checked = true;
                    }
                    youtubeListView.Invalidate(true);
                }
                Invalidate();
                base.OnMouseDown(e);
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                _MouseState = MouseStates.Over;
                tmrAnimation.Start();
                Invalidate();
                if (FindForm().Focused) Parent.Focus();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                _MouseState = MouseStates.None;
                tmrAnimation.Start();
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                _MouseState = MouseStates.Over;
                tmrAnimation.Start();
                Invalidate();
                base.OnMouseUp(e);
            }

            private void tmrAnimation_OnTick(object sender, EventArgs e)
            {
                switch (_MouseState)
                {
                    case MouseStates.None:
                        if (_AnimationValue > 0)
                        {
                            _AnimationValue -= 0.1;
                        }
                        else
                        {
                            _AnimationValue = 0.0;
                            tmrAnimation.Stop();
                        }
                        break;
                    case MouseStates.Over:
                        if (_AnimationValue < 0.7)
                        {
                            _AnimationValue += 0.1;
                        }
                        else
                        {
                            if (_AnimationValue > 0.7 & Math.Abs(_AnimationValue - 0.7) > 0.1)
                            {
                                _AnimationValue -= 0.1;
                            }
                            else
                            {
                                _AnimationValue = 0.7;
                                tmrAnimation.Stop();
                            }
                        }
                        break;
                    case MouseStates.Down:
                        if (_AnimationValue < 1.5)
                        {
                            _AnimationValue += 0.1;
                        }
                        else
                        {
                            _AnimationValue = 1.5;
                            tmrAnimation.Stop();
                        }
                        break;
                }
                if (_AnimationValue < 0)
                    _AnimationValue = 0;
                Invalidate();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                if (_AnimationValue < 0)
                    _AnimationValue = 0;

                e.Graphics.Clear(BackColor);
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(Convert.ToInt32(20 * _AnimationValue), Color.Black)), new Rectangle(0, 0, Width, Height));

                if (downloadStatus == DownloadStatuses.Downloading || downloadStatus == DownloadStatuses.Converting)
                {
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(15, Color.White)), new Rectangle(0, 0, (int) ((double) Width*downloadProgress/100), Height - 2));
                    e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(15, Color.Black)), new Rectangle((int)((double)Width * downloadProgress / 100), 0, Width - (int)((double)Width * downloadProgress / 100), Height - 2));
                }

                e.Graphics.DrawLine(new Pen(Color.FromArgb((int)(120 + _AnimationValue * 15), Color.Black), 2), new Point(0, Height - 1), new Point(Width, Height - 1));
                if (downloadStatus == DownloadStatuses.NotDownloaded)
                {
                    e.Graphics.DrawRectangle(new Pen(Color.FromArgb(100, 100, 100)), new Rectangle(Width - 40, (Height / 2) - 10, 16, 16));
                    if (Checked)
                    {
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(10, Color.White)), new Point(0, 0), new Point(Width, 0)); //Top
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(10, Color.White)), new Point(0, 1), new Point(0, Height - 3)); //Left
                        e.Graphics.DrawLine(new Pen(Color.FromArgb(10, Color.White)), new Point(Width - 1, 1), new Point(Width - 1, Height - 3)); //Right

                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(15, 200, 200, 200)), new Rectangle(0, 0, Width - 1, Height - 1));
                        e.Graphics.DrawImage(Resources.holo_tick, new Rectangle(Width - 37, (Height / 2) - 9, 16, 13));
                    }
                }

                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(10, 10, 10)), new Rectangle(9, 9, 82, 47));
                e.Graphics.DrawString(_Audio.Title, new Font("Segoe UI Light", 14), Brushes.Gainsboro, new Rectangle(99, 8, Width - 142, 30), new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                e.Graphics.DrawImage(_Audio.Thumbnail, new Rectangle(10, 10, 80, 45));

                switch (downloadStatus)
                {
                    case DownloadStatuses.NotDownloaded:
                        e.Graphics.DrawString(_Audio.URL, new Font("Segoe UI", 10), new SolidBrush(Color.FromArgb(125, 125, 125)), new Rectangle(100, 34, Width - 142, 16), new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                        break;
                    case DownloadStatuses.Downloading:
                        e.Graphics.DrawImage(Resources.downloading, new Point(100, 34));
                        e.Graphics.DrawString(String.Format("Downloading... {0}% (Step 1 of 2)", DownloadProgress * 2), new Font("Segoe UI", 10), new SolidBrush(Color.FromArgb(180, 180, 180)), new Point(120, 33));
                        break;
                    case DownloadStatuses.Converting:
                        e.Graphics.DrawImage(Resources.converting, new Point(100, 34));
                        e.Graphics.DrawString("Converting... (Step 2 of 2)", new Font("Segoe UI", 10), new SolidBrush(Color.FromArgb(180, 180, 180)), new Point(120, 33));
                        break;
                    case DownloadStatuses.Completed:
                        e.Graphics.DrawImage(Resources.done, new Point(100, 34));
                        e.Graphics.DrawString(_Audio.URL, new Font("Segoe UI", 10), new SolidBrush(Color.FromArgb(125, 125, 125)), new Rectangle(120, 34, Width - 162, 16), new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                        break;
                    case DownloadStatuses.Error:
                        e.Graphics.DrawImage(Resources.error, new Point(100, 34));
                        e.Graphics.DrawString(String.Format("{0} - failed", _Audio.URL), new Font("Segoe UI", 10), new SolidBrush(Color.FromArgb(125, 125, 125)), new Rectangle(120, 34, Width - 162, 16), new StringFormat { Trimming = StringTrimming.EllipsisCharacter });
                        break;
                }

            }

            public int CompareTo(AudioItem other)
            {
                return _Audio.CompareTo(other._Audio);
            }
        }

    }
}
