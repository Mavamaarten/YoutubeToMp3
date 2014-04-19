using System;
using System.Net;

namespace YouTube
{

    public class YoutubeDownloader
    {
        public event EventHandler<DownloadProgressChangedEventArgs> OnDownloadProgressChanged = delegate { };
        public event EventHandler<AudioInformation> OnDownloadCompleted = delegate { };
        public event EventHandler<Exception> OnDownloadFailed = delegate { };

        private readonly WebClient webClient;
        private AudioInformation audioInProgress;

        public YoutubeDownloader()
        {
            webClient = new WebClient();
            webClient.DownloadProgressChanged += webClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += webClient_DownloadFileCompleted;
        }

        private void webClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            OnDownloadProgressChanged(this, e);
        }

        private void webClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            webClient.Dispose();
            if (e.Error != null)
                OnDownloadFailed(this, e.Error);
            else
                OnDownloadCompleted(this, audioInProgress);
        }

        public void DownloadAudioAsync(AudioInformation audioInformation, FileFormat format, AudioBitrate bitrate, string destinationFileName)
        {
            if (!audioInformation.IsAvailableInFormatAndBitrate(format, bitrate))
                throw new QualityNotAvailableException();
            
            audioInProgress = audioInformation;

            var downloadLink = audioInformation.GetDownloadURL(format, bitrate);
            webClient.DownloadFileAsync(new Uri(downloadLink), destinationFileName);
        }

        private class QualityNotAvailableException : Exception { }
    }
}
