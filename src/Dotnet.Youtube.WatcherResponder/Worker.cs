using System;
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

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> options)
        {
            _logger = logger;
            _youtubeClient = new YoutubeClient(options);
            _repository = new DataRepository();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //while (!stoppingToken.IsCancellationRequested)
            //{
                var videos = await _youtubeClient.ListVideosAsync();
                foreach (var video in videos)
                {
                    if (!_repository.Exists(video))
                    {
                        _logger.LogWarning("New Video: {Id}-{Title}", video.VideoId, video.Title);

                        var videoComments = await _youtubeClient.ListCommentsAsync(video);
                        if (videoComments.Count == 0)
                        {
                            _logger.LogInformation("No Comments for : {VideoId}-{Title}", video.VideoId, video.Title);
                        }

                        foreach (var comment in videoComments)
                        {
                            if (_repository.Exists(comment))
                            {
                                _logger.LogWarning("New Comment: {Id}, TextOriginal: {TextOriginal}", comment.Id, comment.TextOriginal);
                            }
                            else
                            {
                                _logger.LogInformation("Old Comment: {Id}, TextOriginal: {TextOriginal}", comment.Id, comment.TextOriginal);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Old Video: {Id}-{Title}", video.VideoId, video.Title);
                    }
                }

            //    await Task.Delay(30000, stoppingToken);
            //}
        }
    }
}
