using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NReco.VideoConverter;
using YouTube;
using YouTubeDownloader.UI.Controls;

namespace YouTubeDownloader.UI
{
    public partial class frmMain : Form
    {
        private int videosProcessing;

        public frmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            addURLfromClipboard();
        }

        //private async void addURLfromClipboard()
        //{
        //    string clipboardURL = Clipboard.GetText();
        //    if (!clipboardURL.StartsWith("https://www.youtube.com/watch?v=")) return;
        //    videosProcessing++;
        //    progressbar.Enabled = true;
        //    AudioInformation _Audio = await YoutubeService.FetchAudioInformation(clipboardURL);
        //    if (_LstYoutubes.FindItemByVideo(_Audio) == null) _LstYoutubes.AddItem(_Audio);
        //    videosProcessing--;
        //    if (videosProcessing == 0) progressbar.Enabled = false;
        //}

        private async void addURLfromClipboard()
        {
            Action incrementVideoProcessing = () =>
            {
                Interlocked.Increment(ref videosProcessing);
                progressbar.Invoke(new MethodInvoker(() => progressbar.Enabled = true));
            };

            Action decrementVideoProcessing = () =>
            {
                Interlocked.Decrement(ref videosProcessing);
                if (videosProcessing == 0)
                    progressbar.Invoke(new MethodInvoker(() => progressbar.Enabled = false));
            };

            Action<AudioInformation> addToList = (audioInformation) =>
            {
                incrementVideoProcessing();

                if (_LstYoutubes.FindItemByVideo(audioInformation) == null)
                    _LstYoutubes.AddItem(audioInformation);

                decrementVideoProcessing();
            };

            var clipboardText = Clipboard.GetText();
            if (clipboardText.StartsWith("https://www.youtube.com/watch?v="))
            {
                incrementVideoProcessing();

                var audioInformation = await YoutubeService.FetchAudioInformation(clipboardText);
                if (audioInformation != null) 
                    addToList(audioInformation);

                decrementVideoProcessing();
            }
            else if (clipboardText.StartsWith("https://www.youtube.com/playlist?list="))
            {
                incrementVideoProcessing();

                await YoutubeService.FetchPlaylistInformation(clipboardText, addToList, decrementVideoProcessing);
            }
        }

        private void frmMain_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.V && e.Control) addURLfromClipboard();
        }

        private void btnAddVideo_Click(object sender, EventArgs e)
        {
            addURLfromClipboard();
        }

        private void btnRemoveSelected_Click(object sender, EventArgs e)
        {
            foreach (AudioInformation audio in _LstYoutubes.SelectedVideos())
            {
                _LstYoutubes.RemoveItem(audio);
            }
        }

        private void btnDownloadSelected_Click(object sender, EventArgs ea)
        {
            foreach (YoutubeListView.AudioItem selectedItem in _LstYoutubes.SelectedItems())
            {
                if (selectedItem.DownloadStatus != YoutubeListView.AudioItem.DownloadStatuses.NotDownloaded) continue;

                var invalidChars = Path.GetInvalidFileNameChars();
                string fixedTitle = new string(selectedItem._Audio.Title.Where(x => !invalidChars.Contains(x)).ToArray());

                YoutubeDownloader youtubeDownloader = new YoutubeDownloader();
                youtubeDownloader.OnDownloadProgressChanged += (s, e) =>
                {
                    selectedItem.DownloadStatus = YoutubeListView.AudioItem.DownloadStatuses.Downloading;
                    selectedItem.DownloadProgress = e.ProgressPercentage * 0.5f;
                };
                youtubeDownloader.OnDownloadFailed += (s, ex) =>
                {
                    selectedItem.DownloadStatus = YoutubeListView.AudioItem.DownloadStatuses.Error;
                    MessageBox.Show(String.Format("An error has occured.\n{0}", ex.ToString()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                };
                youtubeDownloader.OnDownloadCompleted += (s, video) =>
                {
                    selectedItem.DownloadStatus = YoutubeListView.AudioItem.DownloadStatuses.Converting;

                    FFMpegConverter ffMpeg = new FFMpegConverter();
                    ffMpeg.ConvertProgress += (ss, progress) =>
                    {
                        selectedItem.DownloadProgress = 50 + (float)((progress.Processed.TotalMinutes / progress.TotalDuration.TotalMinutes) * 50);
                    };

                    FileStream fileStream = new FileStream(Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + fixedTitle + ".mp3", FileMode.Create);

                    ffMpeg.LogReceived += async (ss, log) =>
                    {
                        if (!log.Data.StartsWith("video:0kB")) return;

                        Invoke(new MethodInvoker(() =>
                        {
                            selectedItem.DownloadStatus = YoutubeListView.AudioItem.DownloadStatuses.Completed;
                        }));

                        await Task.Delay(1000);
                        fileStream.Close();
                    };

                    new Thread(() => ffMpeg.ConvertMedia(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + fixedTitle + ".mp4",
                        "mp4",
                        fileStream,
                        "mp3",
                        new ConvertSettings { AudioCodec = "libmp3lame" }
                        )).Start(); 
                };

                var highestQualityAvailable = selectedItem._Audio.GetHighestQualityTuple();
                youtubeDownloader.DownloadAudioAsync(selectedItem._Audio, highestQualityAvailable.Item1, highestQualityAvailable.Item2, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\" + fixedTitle + ".mp4");
            }
        }

        class ValueObserver<T> : IObserver<T>
        {
            private readonly Action<T> _actionOnNext;
            private readonly Action<Exception> _actionOnException;
            private readonly Action _actionOnCompleted;

            public ValueObserver(Action<T> actionOnNext = null, Action<Exception> actionOnException = null, Action actionOncompleted = null)
            {
                _actionOnNext = actionOnNext;
                _actionOnException = actionOnException;
                _actionOnCompleted = actionOncompleted;
            }

            public void OnNext(T value)
            {
                if (_actionOnNext != null)
                    _actionOnNext(value);
            }

            public void OnError(Exception error)
            {
                if (_actionOnException != null)
                    _actionOnException(error);
            }

            public void OnCompleted()
            {
                if (_actionOnCompleted != null)
                    _actionOnCompleted();
            }
        }
    }
}