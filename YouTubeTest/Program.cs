using System;
using System.Net;
using Nito.AsyncEx;
using YouTube;

namespace YouTubeTest
{
    class Program
    {
        private static readonly YoutubeService youtubeService = new YoutubeService();

        public static void Main(string[] args)
        {
            AsyncContext.Run(() => MainAsync());
        }

        static async void MainAsync()
        {
            VideoInformation video = await youtubeService.FetchVideoInformation("https://www.youtube.com/watch?v=K1VLaXoRRdk");
            Console.WriteLine(video.ToString());

            // Show all available qualities
            foreach (Tuple<VideoInformation.VideoResolution, VideoInformation.VideoFormat> aq in video.AvailableQualities.Keys)
            {
                Console.WriteLine(aq);
            }

            // Add the event handlers for downloading
            youtubeService.OnDownloadCompleted += OnDownloadCompleted;
            youtubeService.OnDownloadProgressChanged += OnDownloadProgressChanged;
            youtubeService.OnDownloadFailed += OnDownloadFailed;

            // Download the video
            youtubeService.DownloadVideoAsync(video, VideoInformation.VideoResolution._240p, VideoInformation.VideoFormat.MP4, "C:\\Users\\Maarten\\Desktop\\test.mp4");
                
            // Don't immediately close the window
            Console.ReadKey();
        }

        private static void OnDownloadFailed(Exception ex)
        {
            Console.WriteLine("Download failed! {0}", ex.Message);
        }

        private static void OnDownloadProgressChanged(VideoInformation video, DownloadProgressChangedEventArgs e)
        {
            Console.WriteLine("Download progress: {0}/{1} MB", e.BytesReceived / 1024 / 1024, e.TotalBytesToReceive / 1024 / 1024);
        }

        public static void OnDownloadCompleted(VideoInformation video)
        {
            Console.WriteLine("Download completed!");
        }
    }
}
