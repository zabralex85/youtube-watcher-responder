using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CodeHollow.FeedReader;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dotnet.Youtube.WatcherResponder.Clients
{
    public class RssClient
    {
        private readonly ILogger<RssClient> _logger;
        private readonly AppSettings _settings;

        public RssClient(ILogger<RssClient> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
        }

        public async Task<IEnumerable<Models.Video>> ListVideosBySearchAsync(DateTime fromDate)
        {
            List<Models.Video> videos = new List<Models.Video>();

            _logger.LogTrace(JsonSerializer.Serialize(_settings));

            foreach (var channel in _settings.YoutubeChannels)
            {
                var response = await FeedReader.ReadAsync(_settings.YoutubeRssLinkLeftPart + channel);

                videos.AddRange(response.Items
                    .Where(s => !string.IsNullOrEmpty(s.Id) && s.PublishingDate >= fromDate)
                    .Select(video => new Models.Video
                    {
                        ListId = null,
                        Title = video.Title,
                        VideoId = video.Id.Replace("yt:video:", ""),
                        ChannelId = channel
                    }));
            }

            return videos;
        }
    }
}
