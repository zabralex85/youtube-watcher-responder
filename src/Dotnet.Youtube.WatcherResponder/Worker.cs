using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Youtube.WatcherResponder.Clients;
using Dotnet.Youtube.WatcherResponder.DataLayer;
using Dotnet.Youtube.WatcherResponder.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dotnet.Youtube.WatcherResponder
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly YoutubeClient _youtubeClient;
        private readonly RssClient _rssClient;
        private readonly DataRepository _repository;
        private readonly AppSettings _options;

        public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger, IOptions<AppSettings> options)
        {
            _options = options.Value;
            _logger = logger;
            _rssClient = (RssClient)serviceProvider.GetService(typeof(RssClient));
            _youtubeClient = (YoutubeClient) serviceProvider.GetService(typeof(YoutubeClient));
            _repository = (DataRepository)serviceProvider.GetService(typeof(DataRepository));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime fromDate = DateTime.UtcNow.AddDays(-1 * _options.VideosDaysFrom);

                _logger.LogInformation("Start Searching for new videos from {fromDate}", fromDate);

                IEnumerable<Video> videos = null;

                if (_options.CheckViaRss) 
                    videos = await _rssClient.ListVideosBySearchAsync(fromDate);

                if (videos == null || !_options.CheckViaRss)
                    videos = await _youtubeClient.ListVideosBySearchAsync(fromDate);

                foreach (var video in videos)
                {
                    bool authorCommentFound = false;

                    if (_repository.CommentExists(video.VideoId))
                    {
                        _logger.LogTrace("Comment found on video {VideoId}", video.VideoId);
                        authorCommentFound = true;
                    }

                    if (!authorCommentFound && _options.CheckCommentOnVideo)
                    {
                        var videoComments = await _youtubeClient.ListCommentsAsync(video);
                        if (videoComments.Count == 0)
                        {
                            _logger.LogTrace("No Comments for : {VideoId}-{Title}", video.VideoId, video.Title);
                        }

                        foreach (var comment in videoComments
                            .Where(comment => comment.AuthorDisplayName
                                .Contains(_options.AuthorDisplayName)))
                        {
                            authorCommentFound = true;
                            _repository.AddAuthorComment(comment);
                        }
                    }

                    if (!authorCommentFound)
                    {
                        var comment = await _youtubeClient.AddCommentForVideo(video);
                        _repository.AddAuthorComment(comment);

                        _logger.LogTrace("Comment added on video {VideoId}, {TextOriginal}", video.VideoId, comment.TextOriginal);
                    }
                }

                _logger.LogInformation("End Searching for new videos from {fromDate}", fromDate);
                await Task.Delay(_options.Timeout * 1000, stoppingToken);
            }
        }
    }
}
