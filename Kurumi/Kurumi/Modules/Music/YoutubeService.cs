using Discord;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Kurumi.Common;
using Kurumi.Common.Extensions;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace Kurumi.Modules.Music
{
    public class YoutubeService : IMusicProviderService
    {
        private readonly JObject Data;
        private readonly JObject ContentData;
        private readonly string Url;

        public string Title => (string)Data["items"][0]["snippet"]["title"];
        public string RawDuration => (string)ContentData["items"][0]["contentDetails"]["duration"];
        public int Duration => Convert.ToInt32(XmlConvert.ToTimeSpan(RawDuration).TotalSeconds);
        public string Thumbnail => $"https://img.youtube.com/vi/{VideoId(Url)}/0.jpg";

        public YoutubeService(string Url, bool LoadInfo = true)
        {
            if (LoadInfo)
            {
                this.Url = Url;

                //Get data
                string RequestUrl = "https://www.googleapis.com/youtube/v3/videos?part=snippet&id="
                    + VideoId(Url)
                    + "&key="
                    + Config.YoutubeApiKey;
                GetData(RequestUrl, ref Data);

                //Get content data
                RequestUrl = "https://www.googleapis.com/youtube/v3/videos?part=contentDetails&id="
                    + VideoId(Url)
                    + "&key="
                    + Config.YoutubeApiKey;
                GetData(RequestUrl, ref ContentData);
            }
        }
        private void GetData(string RequestUrl, ref JObject Data)
        {
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(RequestUrl);
            HttpWebResponse Response = (HttpWebResponse)Request.GetResponse();
            StreamReader Reader = new StreamReader(Response.GetResponseStream());
            Data = JObject.Parse(Reader.ReadToEnd());
        }
        public string ToFancyDuration()
        {
            TimeSpan t = TimeSpan.FromSeconds(Duration);
            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                            t.Hours,
                            t.Minutes,
                            t.Seconds);
        }


        public static async Task<List<ISongInfo>> Search(string Keyword, IUser user)
        {
            //Setup
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = Config.YoutubeApiKey,
                ApplicationName = "Kurumi_Bot#3030"
            });

            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = Keyword;

            //Search
            int MaxRes = 20;
            int SearchCount = 1;
            Search:
            searchListRequest.MaxResults = MaxRes; //Only need 10 but this will include live streams, channels, playlists

            var searchListResponse = await searchListRequest.ExecuteAsync();

            //Get videos
            List<string> VideoResults = new List<string>();
            foreach (var res in searchListResponse.Items)
            {
                if (res.Id.Kind == "youtube#video")
                    VideoResults.Add(res.Id.VideoId);
            }

            //Check if there are 10 results
            if(VideoResults.Count < 10 && SearchCount < 4) //No more videos or there were a lot of live streams, etc. Try again with more
            {
                MaxRes = 10 - VideoResults.Count + 10;
                SearchCount++;
                goto Search;
            }


            //tbh I didn't think this throught, I wanted to return only urls but I have to create new YoutubeServices for all so I might as well return song info

            //Convert 10 songs to SongInfo
            List<ISongInfo> Songs = new List<ISongInfo>();
            for (int i = 0; i < VideoResults.Count && i < 10; i++)
            {
                Songs.Add(new YoutubeSongInfo($"https://www.youtube.com/watch?v={VideoResults[i]}", user));
            }

            return Songs;
        }
        //This only gets the id, it doesn't check if its valid or not.
        public static string VideoId(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri Yuri)) //Yuri is the purest form of love
                return null;

            string Host = Yuri.Host.Remove("www."); //Get the hostname and ignore www.
            switch (Host)
            {
                case "youtube.com": //.com is fine because all country specific domain (for example: youtube.hu) redirects to youtube.com
                    if (Yuri.PathAndQuery.StartsWith("/watch?v=")) //video url
                        return Yuri.Query.Remove("?v="); //Get the id from the query
                    break;
                case "youtu.be":
                    return Yuri.AbsolutePath.Substring(1); //Remove the /
            }
            return null;
        }
        public static bool ValidVideo(string Url)
        {
            string ReqUrl = $"https://www.youtube.com/oembed?format=json&url={Url}"; //Returns 404 when not found
            HttpWebRequest Request = (HttpWebRequest)WebRequest.Create(ReqUrl);
            return ((HttpWebResponse)Request.GetResponse()).StatusCode != HttpStatusCode.NotFound;
        }
    }
}