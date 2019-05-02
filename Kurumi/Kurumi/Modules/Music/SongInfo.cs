using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Kurumi.Modules.Music.Services;

namespace Kurumi.Modules.Music
{
    public class YoutubeSongInfo : ISongInfo
    {
        public string Title { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public string StreamUrl { get; private set; }
        public string FormatedDuration { get; private set; }
        public int Duration { get; private set; }
        public MusicProvider Provider { get; private set; }
        public IUser User { get; private set; }

        public YoutubeSongInfo(string Url, IUser user)
        {
            YoutubeService service = new YoutubeService(Url);
            Title = service.Title;
            ThumbnailUrl = service.Thumbnail;
            Duration = service.Duration;
            FormatedDuration = service.ToFancyDuration();
            StreamUrl = Url;
            Provider = MusicProvider.Youtube;
            User = user;
        }

        public bool Equals(ISongInfo other) 
            => StreamUrl == other.StreamUrl;
    }

    public class SoundCloudSongInfo : ISongInfo
    {
        public string Title { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public string StreamUrl { get; private set; }
        public string FormatedDuration { get; private set; }
        public int Duration { get; private set; }
        public MusicProvider Provider { get; private set; }
        public IUser User { get; private set; }

        public SoundCloudSongInfo(string Url, IUser user)
        {
            SoundCloudService service = new SoundCloudService(Url);
            Title = service.Title;
            ThumbnailUrl = service.Thumbnail;
            Duration = service.Duration;
            FormatedDuration = service.FancyDuration;
            StreamUrl = Url;
            Provider = MusicProvider.SoundCloud;
            User = user;
        }

        public bool Equals(ISongInfo other)
            => StreamUrl == other.StreamUrl;
    }

    public class ListenMoeSongInfo : ISongInfo
    {
        public string Title { get; private set; }
        public string ThumbnailUrl { get; private set; }
        public string StreamUrl { get; private set; }
        public string FormatedDuration { get; private set; }
        public int Duration { get; private set; }
        public MusicProvider Provider { get; private set; }
        public IUser User { get; private set; }

        public ListenMoeSongInfo(IUser user)
        {
            Title = "Listen.Moe radio";
            ThumbnailUrl = "https://pbs.twimg.com/profile_images/792630035832266752/mmry-djk_400x400.jpg";
            StreamUrl = "https://listen.moe/stream";
            Duration = -1;
            FormatedDuration = "-";
            Provider = MusicProvider.ListenMoe;
            User = user;
        }
        public bool Equals(ISongInfo other)
            => StreamUrl == other.StreamUrl;
    }
}