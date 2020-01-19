using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Youtube.WatcherResponder.Clients;
using Dotnet.Youtube.WatcherResponder.DataLayer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dotnet.Youtube.WatcherResponder
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly YoutubeClient _youtubeClient;
        private readonly DataRepository _repository;
        private readonly AppSettings _options;

        public Worker(IServiceProvider serviceProvider, ILogger<Worker> logger, IOptions<AppSettings> options)
        {
            _options = options.Value;
            _logger = logger;
            _youtubeClient = (YoutubeClient) serviceProvider.GetService(typeof(YoutubeClient));
            _repository = (DataRepository)serviceProvider.GetService(typeof(DataRepository));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var videos = await _youtubeClient.ListVideosBySearchAsync(fromDate:DateTime.UtcNow.AddDays(-1 * _options.VideosDaysFrom));
                foreach (var video in videos)
                {
                    if (_repository.CommentExists(video.VideoId))
                    {
                        _logger.LogInformation("Comment found on video {VideoId}", video.VideoId);
                        continue;
                    }

                    bool authorCommentFound = false;

                    if (_options.CheckCommentOnVideo)
                    {
                        var videoComments = await _youtubeClient.ListCommentsAsync(video);
                        if (videoComments.Count == 0)
                        {
                            _logger.LogInformation("No Comments for : {VideoId}-{Title}", video.VideoId, video.Title);
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

                        _logger.LogInformation("Comment added on video {VideoId}, {TextOriginal}", video.VideoId, comment.TextOriginal);
                    }
                }

                await Task.Delay(_options.Timeout * 1000, stoppingToken);
            }
        }
    }
}
