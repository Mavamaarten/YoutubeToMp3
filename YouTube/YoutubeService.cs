using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Net.Http;

namespace YouTube
{
    public static class YoutubeService
    {
        private const string URL_PATTERN = "<meta property=\"og:url\" content=\"(.*)\">";
        private const string TITLE_PATTERN = "<meta property=\"og:title\" content=\"(.*)\">";
        private const string DESCRIPTION_PATTERN = "<meta property=\"og:description\" content=\"(.*)\">";
        private const string THUMBNAIL_PATTERN = "<meta property=\"og:image\" content=\"(.*)\">";     

        public static async Task<AudioInformation> FetchAudioInformation(string URL)
        {
            using (var http = new HttpClient())
            {
                string response = await http.GetStringAsync(URL);
                return await ParseVideoFromHTML(response);
            }
        }

        private static async Task<AudioInformation> ParseVideoFromHTML(string html)
        {
            try
            {
                // Parse basic video information from the HTML
                string url = new Regex(URL_PATTERN).Match(html).Groups[1].Value;
                string title = new Regex(TITLE_PATTERN).Match(html).Groups[1].Value;
                string description = new Regex(DESCRIPTION_PATTERN).Match(html).Groups[1].Value;
                string thumbnailURL = new Regex(THUMBNAIL_PATTERN).Match(html).Groups[1].Value;

                // Make sure that the title and description don't contain any HTML-escaped characters like &amp;
                title = WebUtility.HtmlDecode(title);
                description = WebUtility.HtmlDecode(description);

                if (url.Contains("&")) url = url.Split('&')[0]; // If the URL contains more stuff in the query string, get rid of it

                // Separate the JSON string, which is what we need for the download URLs and qualities
                string jsonString = html.Split(new[] { "ytplayer.config = " }, StringSplitOptions.None)[1];
                jsonString = jsonString.Split(new []{"};"}, StringSplitOptions.None)[0] + "}";

                // Parse video information from the JSON
                dynamic json = new JavaScriptSerializer().Deserialize<object>(jsonString);
                string[] keywords = json["args"]["keywords"].Split(',');
                string[] adaptive_fmts = json["args"]["adaptive_fmts"].Split(new[] { "," }, StringSplitOptions.None);

                // Create a dictionary with different qualities, formats and URL's
                Dictionary<Tuple<FileFormat, AudioBitrate>, string> availableQualities = new Dictionary<Tuple<FileFormat, AudioBitrate>, string>();
                foreach (string stream in adaptive_fmts)
                {
                    if (!stream.Contains("url=")) continue;
                    if (!stream.Contains("itag=")) continue;

                    string formatString = Uri.UnescapeDataString(ParseFieldFromQueryString("type", stream));
                    if(!IsAudio(formatString)) continue;

                    string videoURL = Uri.UnescapeDataString(ParseFieldFromQueryString("url", stream));
                    string itag = Uri.UnescapeDataString(ParseFieldFromQueryString("itag", stream));
                    AudioBitrate bitrate = ParseBitrate(itag);
                    FileFormat format = ParseFormat(formatString);

                    Tuple<FileFormat, AudioBitrate> qualityTuple = Tuple.Create(format, bitrate);
                    if(!availableQualities.ContainsKey(qualityTuple)) availableQualities.Add(qualityTuple, videoURL);

                }

                // Download the thumbnail
                Image thumbnail;
                using (var http = new HttpClient())
                {
                    thumbnail = Image.FromStream(new MemoryStream(await http.GetByteArrayAsync(thumbnailURL)));
                }

                // Create the video instance
                AudioInformation audioInformation = new AudioInformation
                {
                    URL = url,
                    Title = title,
                    Description = description,
                    Keywords = keywords,
                    Thumbnail = thumbnail,
                    AvailableQualities = availableQualities
                };

                foreach (Tuple<FileFormat, AudioBitrate> qualityTuple in availableQualities.Keys)
                {
                    Console.WriteLine(qualityTuple.ToString());
                }

                // And return it :)
                return audioInformation;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private static string ParseFieldFromQueryString(string field, string input)
        {
            string result = input.Split(new[] { field + "=" }, StringSplitOptions.None)[1];
            if (result.Contains("&")) result = result.Split('&')[0];
            if (result.Contains(",")) result = result.Split(',')[0];
            return result;
        }

        private static FileFormat ParseFormat(string formatString)
        {
            switch (formatString.ToUpper().Split('/')[1].Split(';')[0])
            {
                case "MP4":
                    return FileFormat.MP4;
                case "WEBM":
                    return FileFormat.WEBM;
                default:
                    Console.WriteLine("Failed to parse format: {0}", formatString);
                    return FileFormat.MP4;
            }
        }

        private static AudioBitrate ParseBitrate(string itag)
        {
            switch (itag)
            {
                case "139":
                    return AudioBitrate._48;
                case "140":
                case "171":
                    return AudioBitrate._128;
                case "172":
                    return AudioBitrate._192;
                case "141":
                    return AudioBitrate._256;
                default:
                    return AudioBitrate.NOT_AUDIO;
            }
        }

        private static bool IsAudio(string formatString)
        {
            switch (formatString.ToLower().Split('/')[0])
            {
                case "audio":
                    return true;
                default:
                    return false;
            }
        }

    }
}
