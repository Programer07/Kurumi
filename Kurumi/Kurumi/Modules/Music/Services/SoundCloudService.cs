using Kurumi.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Kurumi.Modules.Music.Services
{
    public class SoundCloudService : IMusicProviderService
    {
        public string Title { get; }
        public int Duration { get; }
        public string Thumbnail { get; }
        public string FancyDuration { get; }

        public SoundCloudService(string Url)
        {
            SoundCloudAPI api = new SoundCloudAPI(Url);
            Title = api.Title;
            Duration = (int)api.Duration;
            FancyDuration = api.FancyDuration;
            Thumbnail = api.Image;
        }

        public static bool ValidUrl(string Url)
            => new SoundCloudAPI(Url).IsValidUrl();
        public static bool SoundCloudUrl(string Url)
        {
            if (!Uri.TryCreate(Url, UriKind.Absolute, out Uri url))
                return false;
            return url.Host == "soundcloud.com";
        }
    }


    //Soundcloud API doesn't work (https://developers.soundcloud.com/) so I made this.
    //This is the worst possible solution and very r/BadCode material.
    public class SoundCloudAPI
    {
        private HttpClient client = new HttpClient();
        private string URL;
        public string Title { get; private set; }
        public string Artist { get; private set; }
        public string Page { get; private set; }
        public string Image { get; private set; }
        public double Duration { get; private set; }
        public string FancyDuration { get; private set; }

        public SoundCloudAPI(string URL)
        {
            this.URL = URL;
            GetValues();
        }
        public SoundCloudAPI(string URL, bool Validation)
        {
            this.URL = URL;
        }

        public bool IsValidUrl()
        {
            try
            {
                client.DefaultRequestHeaders.Clear();
                client.AddFakeHeaders();
                HttpResponseMessage res = client.GetAsync(URL).Result;
                return (res.StatusCode == HttpStatusCode.OK);
            }
            catch (Exception)
            {
                return false;
            }
        }
        private Task GetValues()
        {
            this.Page = client.AddFakeHeaders().GetStringAsync(URL).Result;
            string Image = null;
            try
            {
                Image = this.Page.Split($"<meta property={'"'}og:image{'"'} content={'"'}")[1].Split($"{'"'}>")[0];
                this.Image = Image;
            }
            catch { }
            if (Image == null)
                this.Image = "https://store-images.s-microsoft.com/image/apps.59943.14398308773733109.80b09e5a-a6b5-437c-9a13-b8b5a4188f2f.27f08ee5-31ea-4301-b10a-bdf515cd19c7?mode=scale&q=90&h=270&w=270&background=%230078D7";

            double Duration = 0;
            string FancyDuration = "-";
            try
            {
                string RawDuration = this.Page.Split($"<meta itemprop={'"'}duration{'"'} content={'"'}")[1].Split($"{'"'} />")[0];
                FancyDuration = RawDuration.Remove("PT").Remove("S").Replace("H", ":").Replace("M", ":");
                Duration = TimeSpan.Parse(FancyDuration).TotalSeconds;
            }
            catch { }
            this.Duration = Duration;
            this.FancyDuration = FancyDuration;
            string[] TitleArtist = this.Page.Split($"<title>")[1].Split("|")[0].Split(" by ");
            if (TitleArtist.Length < 2)
            {
                this.Title = "ERROR";
                this.Artist = "ERROR";
                return Task.CompletedTask;
            }
            this.Title = TitleArtist[0];
            this.Artist = TitleArtist[1];
            return Task.CompletedTask;
        }
    }
}