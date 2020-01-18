using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dotnet.Youtube.WatcherResponder.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;

namespace Dotnet.Youtube.WatcherResponder.Clients
{
    public class YoutubeClient
    {
        private UserCredential _credential;
        private readonly string _channelId;
        private YouTubeService _youtubeService;

        public YoutubeClient(string channelId)
        {
            _channelId = channelId;
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

        public async Task<List<Models.Video>> ListVideosAsync()
        {
            while (_credential == null || _youtubeService == null)
            {
                Thread.Sleep(1000);
            }

            List<Models.Video> videos = new List<Models.Video>();
            ChannelsResource.ListRequest channelsListRequest = _youtubeService.Channels.List("contentDetails");
            channelsListRequest.Mine = true;

            ChannelListResponse channelsListResponse = await channelsListRequest.ExecuteAsync();

            foreach (Channel channel in channelsListResponse.Items)
            {
                if(channel.Id != _channelId)
                    continue;

                string uploadsListId = channel.ContentDetails.RelatedPlaylists.Uploads;

                string nextPageToken = "";
                while (nextPageToken != null)
                {
                    var playlistItemsListRequest = _youtubeService.PlaylistItems.List("snippet");
                    playlistItemsListRequest.PlaylistId = uploadsListId;
                    playlistItemsListRequest.MaxResults = 50;
                    playlistItemsListRequest.PageToken = nextPageToken;

                    var playlistItemsListResponse = await playlistItemsListRequest.ExecuteAsync();

                    foreach (var playlistItem in playlistItemsListResponse.Items)
                    {
                        videos.Add(new Models.Video
                        {
                            ListId = uploadsListId,
                            Title = playlistItem.Snippet.Title,
                            VideoId = playlistItem.Snippet.ResourceId.VideoId
                        });
                    }

                    nextPageToken = playlistItemsListResponse.NextPageToken;
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
                request.MaxResults = 50;
                request.VideoId = video.VideoId;
                request.PageToken = nextPageToken;

                CommentThreadListResponse response = null;

                try
                {
                    response = await request.ExecuteAsync();
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Request had insufficient authentication scope"))
                    {
                        await _credential.RevokeTokenAsync(CancellationToken.None);
                        await Init();

                        response = await request.ExecuteAsync();
                    }
                }

                if (response != null)
                {
                    foreach (var thread in response.Items)
                    {
                        resultList.Add(new VideoComment
                        {
                            Id = thread.Snippet.TopLevelComment.Id,
                            AuthorDisplayName = thread.Snippet.TopLevelComment.Snippet.AuthorDisplayName,
                            PublishedAt = thread.Snippet.TopLevelComment.Snippet.PublishedAt,
                            ETag = thread.Snippet.TopLevelComment.Snippet.ETag,
                            HtmlTextDisplay = thread.Snippet.TopLevelComment.Snippet.TextDisplay,
                            TextOriginal = thread.Snippet.TopLevelComment.Snippet.TextOriginal,
                            VideoId = video.VideoId
                        });
                    }

                    nextPageToken = response.NextPageToken;
                }
            }

            return resultList;
        }
    }
}