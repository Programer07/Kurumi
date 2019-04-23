using System;
using System.Collections.Generic;
using System.Text;

namespace Kurumi.Modules.Music
{
    public interface IMusicProviderService
    {
        string Title { get; }
        int Duration { get; }
        string Thumbnail { get; }
    }
}