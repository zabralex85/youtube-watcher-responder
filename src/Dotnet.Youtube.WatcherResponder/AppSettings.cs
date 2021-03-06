using System.Collections.Generic;

namespace Dotnet.Youtube.WatcherResponder
{
    public class AppSettings
    {
        public int Timeout { get; set; }
        public string AuthorDisplayName { get; set; }
        public List<string> YoutubeChannels { get; set; }
        public List<string> Reactions { get; set; }
        public int VideosDaysFrom { get; set; }
        public bool CheckCommentOnVideo { get; set; }
        public bool CheckViaRss { get; set; }
        public string YoutubeRssLinkLeftPart { get; set; }
    }
}