using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Music.Services
{
    public interface IMusicProviderService
    {
        string Title { get; }
        int Duration { get; }
        string Thumbnail { get; }
    }
}