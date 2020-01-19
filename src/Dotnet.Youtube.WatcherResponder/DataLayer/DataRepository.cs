using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DBreeze;
using Dotnet.Youtube.WatcherResponder.Models;

namespace Dotnet.Youtube.WatcherResponder.DataLayer
{
    public class DataRepository
    {
        private readonly DBreezeEngine _engine;

        public DataRepository()
        {
            var path = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            _engine = new DBreezeEngine(path + @"\data");
        }

        public bool CommentExists(Video video)
        {
            using (var tran = _engine.GetTransaction())
            {
                var exVideo = tran.Select<string, Models.VideoComment>("video_comment", video.VideoId);
                return exVideo.Exists;
            }
        }

        public void AddAuthorComment(Video video, VideoComment comment)
        {
            using (var t = _engine.GetTransaction())
            {
                //Documentation https://goo.gl/Kwm9aq
                //This line with a list of tables we need in case if we modify more than 1 table inside of transaction
                // t.SynchronizeTables("video_comment");

                //Documentation https://goo.gl/YtWnAJ
                t.ObjectInsert("video_comment", new DBreeze.Objects.DBreezeObject<Video>
                {
                    NewEntity = true,
                    Entity = video,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                    {
                        //to Get customer by ID
                        new DBreeze.Objects.DBreezeIndex(1, video.VideoId) {PrimaryIndex = true},
                    }
                }, false);

                //Documentation https://goo.gl/s8vtRG
                //Setting text search index. We will store text-search 
                //indexes concerning customers in table "TS_Customers".
                //Second parameter is a reference to the customer ID.
                // t.TextInsert("TS_Customers", video.VideoId.ToBytes(), video.VideoId);

                //Committing entry
                t.Commit();
            }
        }
    }
}
