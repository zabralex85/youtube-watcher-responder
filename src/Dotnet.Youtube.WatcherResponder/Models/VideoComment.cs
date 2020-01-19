using System;

namespace Dotnet.Youtube.WatcherResponder.Models
{
    public class VideoComment
    {
        public string Id { get; set; }
        public string AuthorDisplayName { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string ETag { get; set; }
        public string TextOriginal { get; set; }
        public string VideoId { get; set; }
    }
}
