﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Youtube.WatcherResponder.Models;
using Dotnet.Youtube.WatcherResponder.Utils;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Dotnet.Youtube.WatcherResponder.Clients
{
    public class YoutubeClient
    {
        private readonly ILogger<YoutubeClient> _logger;
        private UserCredential _credential;
        private YouTubeService _youtubeService;
        private readonly AppSettings _settings;
        private readonly Random _random;

        public YoutubeClient(ILogger<YoutubeClient> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;
            _random = RandomProvider.GetThreadRandom();

            Task.Run(async () => await Init());
        }

        private async Task Init()
        {
            using (var stream = new FileStream("client_secrets.json", FileMode.Open, FileAccess.Read))
            {
                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[]
                    {
                        YouTubeService.Scope.YoutubeForceSsl
                    },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("Dotnet.Youtube.WatcherResponder")
                );

                _youtubeService = new YouTubeService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = this.GetType().ToString()
                });
            }
        }

        public async Task<List<Models.Video>> ListVideosByChannelsAsync()
        {
            List<Models.Video> videos = new List<Models.Video>();

            while (_credential == null || _youtubeService == null)
            {
                Thread.Sleep(1000);
            }

            _logger.LogInformation(JsonSerializer.Serialize(_settings));

            foreach (var channel in _settings.YoutubeChannels)
            {
                var request = _youtubeService.Channels.List("contentDetails");
                request.Id = channel;

                ChannelListResponse response = null;
                try
                {
                    _logger.LogInformation("_youtubeService.Channels.List");
                    response = await request.ExecuteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);

                    if (e.Message.Contains("Request had insufficient authentication scope"))
                    {
                        await _credential.RevokeTokenAsync(CancellationToken.None);
                        await Init();

                        _logger.LogInformation("_youtubeService.Channels.List");
                        response = await request.ExecuteAsync();
                    }
                }

                if (response == null) continue;

                foreach (var channelItem in response.Items)
                {
                    string uploadsListId = channelItem.ContentDetails.RelatedPlaylists.Uploads;

                    string nextPageToken = "";
                    while (nextPageToken != null)
                    {
                        var playlistItemsListRequest = _youtubeService.PlaylistItems.List("snippet");
                        playlistItemsListRequest.PlaylistId = uploadsListId;
                        playlistItemsListRequest.MaxResults = 50;
                        playlistItemsListRequest.PageToken = nextPageToken;

                        _logger.LogInformation("playlistItemsListResponse");
                        var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                        videos.AddRange(playlistItemsListResponse.Items.Select(video => new Models.Video
                        {
                            ListId = uploadsListId, 
                            Title = video.Snippet.Title,
                            VideoId = video.Snippet.ResourceId.VideoId,
                            ChannelId = video.Snippet.ChannelId
                        }));

                        nextPageToken = playlistItemsListResponse.NextPageToken;
                    }
                }
            }

            return videos;
        }

        public async Task<List<Models.VideoComment>> ListCommentsAsync(Models.Video video)
        {
            var resultList = new List<VideoComment>();

            string nextPageToken = "";
            while (nextPageToken != null)
            {
                var request = _youtubeService.CommentThreads.List("snippet");
                request.MaxResults = 100;
                request.VideoId = video.VideoId;
                request.TextFormat = CommentThreadsResource.ListRequest.TextFormatEnum.PlainText;
                request.PageToken = nextPageToken;

                CommentThreadListResponse response = null;

                try
                {
                    _logger.LogInformation("list comment");
                    response = await request.ExecuteAsync();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);

                    if (e.Message.Contains("Request had insufficient authentication scope"))
                    {
                        await _credential.RevokeTokenAsync(CancellationToken.None);
                        await Init();

                        _logger.LogInformation("list comment");
                        response = await request.ExecuteAsync();
                    }
                }

                if (response == null) continue;

                resultList.AddRange(response.Items.Select(thread => new VideoComment
                {
                    Id = thread.Snippet.TopLevelComment.Id,
                    AuthorDisplayName = thread.Snippet.TopLevelComment.Snippet.AuthorDisplayName,
                    PublishedAt = thread.Snippet.TopLevelComment.Snippet.PublishedAt,
                    ETag = thread.Snippet.TopLevelComment.Snippet.ETag,
                    TextOriginal = thread.Snippet.TopLevelComment.Snippet.TextOriginal,
                    VideoId = video.VideoId
                }));

                nextPageToken = response.NextPageToken;
            }

            return resultList;
        }

        public async Task<VideoComment> AddCommentForVideo(Models.Video video)
        {
            string reaction;

            if (_settings.Reactions == null)
            {
                reaction = "Best ever video!";
            }
            else
            {
                if (_settings.Reactions.Count != 0)
                {
                    reaction = _settings.Reactions[_random.Next(0, _settings.Reactions.Count - 1)];
                }
                else
                {
                    reaction = "Best ever video!";
                }
            }

            var request = _youtubeService.CommentThreads.Insert(
                new CommentThread
                {
                    Snippet = new CommentThreadSnippet
                    {
                        ChannelId = video.ChannelId,
                        VideoId = video.VideoId,
                        TopLevelComment = new Comment
                        {
                            Snippet = new CommentSnippet
                            {
                                TextOriginal = reaction
                            }
                        }
                    }
                }, "snippet");

            CommentThread response;

            try
            {
                _logger.LogTrace("Added comment {reaction}", reaction);
                response = await request.ExecuteAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message);
                return null;
            }

            return new VideoComment
            {
                Id = response.Snippet.TopLevelComment.Id,
                AuthorDisplayName = response.Snippet.TopLevelComment.Snippet.AuthorDisplayName,
                PublishedAt = response.Snippet.TopLevelComment.Snippet.PublishedAt,
                ETag = response.Snippet.TopLevelComment.Snippet.ETag,
                TextOriginal = response.Snippet.TopLevelComment.Snippet.TextOriginal,
                VideoId = video.VideoId
            };
        }

        public async Task<IEnumerable<Models.Video>> ListVideosBySearchAsync(DateTime fromDate)
        {
            List<Models.Video> videos = new List<Models.Video>();

            while (_credential == null || _youtubeService == null)
            {
                Thread.Sleep(1000);
            }

            _logger.LogTrace(JsonSerializer.Serialize(_settings));


            foreach (var channel in _settings.YoutubeChannels)
            {
                var request = _youtubeService.Search.List("snippet");
                request.ChannelId = channel;
                request.Order = SearchResource.ListRequest.OrderEnum.Date;
                request.PublishedAfter = fromDate;

                SearchListResponse response = null;
                try
                {
                    _logger.LogTrace("Analyze channel {channel}", channel);
                    response = await request.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);

                    if (ex.Message.Contains("Request had insufficient authentication scope"))
                    {
                        await _credential.RevokeTokenAsync(CancellationToken.None);
                        await Init();

                        response = await request.ExecuteAsync();
                    }
                }

                if (response == null) continue;

                videos.AddRange(response.Items
                    .Where(s => !string.IsNullOrEmpty(s.Id.VideoId))
                    .Select(video => new Models.Video
                    {
                        ListId = video.Id.PlaylistId,
                        Title = video.Snippet.Title,
                        VideoId = video.Id.VideoId,
                        ChannelId = video.Snippet.ChannelId
                    }));
            }


            return videos;
        }
    }
}