using Discord;
using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Music
{
    public interface ISongInfo : IEquatable<ISongInfo>
    {
        string Title { get; }
        string ThumbnailUrl { get; }
        string StreamUrl { get; }
        string FormatedDuration { get; }
        int Duration { get; }
        MusicProvider Provider { get; }
        IUser User { get;  }
    }
}