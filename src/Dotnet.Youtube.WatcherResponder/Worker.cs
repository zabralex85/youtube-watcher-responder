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
                var videos = await _youtubeClient.ListVideosAsync();
                foreach (var video in videos)
                {
                    if (_repository.CommentExists(video, _options.AuthorDisplayName)) continue;

                    _logger.LogInformation("New Video: {Id}-{Title}", video.VideoId, video.Title);

                    var videoComments = await _youtubeClient.ListCommentsAsync(video);
                    if (videoComments.Count == 0)
                    {
                        _logger.LogInformation("No Comments for : {VideoId}-{Title}", video.VideoId, video.Title);
                    }

                    bool authorCommentFound = false;
                    foreach (var comment in videoComments
                        .Where(comment => comment.AuthorDisplayName
                            .Contains(_options.AuthorDisplayName)))
                    {
                        authorCommentFound = true;
                        _repository.AddAuthorComment(video, comment);
                    }

                    if (!authorCommentFound)
                    {
                        var comment = await _youtubeClient.AddCommentForVideo(video);
                        _repository.AddAuthorComment(video, comment);
                    }
                }

                await Task.Delay(_options.Timeout * 1000, stoppingToken);
            }
        }
    }
}
