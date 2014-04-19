using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace YouTube
{
    public class AudioInformation : IEquatable<AudioInformation>
    {
        public string URL { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string[] Keywords { get; set; }
        public Image Thumbnail { get; set; }
        public Dictionary<Tuple<FileFormat, AudioBitrate>, string> AvailableQualities { get; set; }

        public bool IsAvailableInFormatAndBitrate(FileFormat format, AudioBitrate bitrate)
        {
            return AvailableQualities.Keys.Contains(Tuple.Create(format, bitrate));
        }

        public string GetDownloadURL(FileFormat format, AudioBitrate bitrate)
        {
            return IsAvailableInFormatAndBitrate(format, bitrate) ? AvailableQualities[Tuple.Create(format, bitrate)] : null;
        }

        public Tuple<FileFormat, AudioBitrate> GetHighestQualityTuple()
        {
            var result = Tuple.Create(FileFormat.WEBM, AudioBitrate.NOT_AUDIO);

            foreach (Tuple<FileFormat, AudioBitrate> availableQuality in AvailableQualities.Keys)
            {
                if (availableQuality.Item2 > result.Item2) result = availableQuality;
                if (availableQuality.Item1 > result.Item1 && availableQuality.Item2 >= result.Item2) result = availableQuality;
            }

            return result;
        }

        public bool Equals(AudioInformation other)
        {
            return URL.Equals(other.URL);
        }

        public override string ToString()
        {
            return string.Format("URL: {0}\nTitle: {1}\nDescription: {2}\nKeywords: {3}\nThumbnail: {4}", URL, Title, Description, string.Join(", ", Keywords), Thumbnail);
        }
    }
}
