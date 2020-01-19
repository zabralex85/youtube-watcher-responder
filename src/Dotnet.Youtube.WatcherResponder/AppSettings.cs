using System.Collections.Generic;

namespace Dotnet.Youtube.WatcherResponder
{
    public class AppSettings
    {
        public string AuthorDisplayName { get; set; }
        public List<string> YoutubeChannels { get; set; }
        public List<string> Reactions { get; set; }
    }
}