using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DBreeze;
using DBreeze.Utils;

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

        public bool CommentExists(string videoId)
        {
            bool exists = false;

            using (var t = _engine.GetTransaction())
            {
                foreach (var doc in t.TextSearch("TS_Video").BlockAnd(videoId).GetDocumentIDs())
                {
                    var obj = t.Select<byte[], byte[]>("VideoComment", 1.ToIndex(doc)).ObjectGet<Models.VideoComment>();
                    if (obj != null)
                    {
                        Console.WriteLine(obj.Entity.Id + " " + obj.Entity.VideoId);
                        exists = true;
                    }
                }
            }

            return exists;
        }

        public void AddAuthorComment(Models.VideoComment comment)
        {
            using (var t = _engine.GetTransaction())
            {
                //Documentation https://goo.gl/Kwm9aq
                //This line with a list of tables we need in case if we modify more than 1 table inside of transaction
                // t.SynchronizeTables("video_comment");

                //Documentation https://goo.gl/YtWnAJ
                t.ObjectInsert("VideoComment", new DBreeze.Objects.DBreezeObject<Models.VideoComment>
                {
                    NewEntity = true,
                    Entity = comment,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                    {
                        //to Get customer by ID
                        new DBreeze.Objects.DBreezeIndex(1, comment.Id) {PrimaryIndex = true},
                    }
                }, false);

                //Documentation https://goo.gl/s8vtRG
                //Setting text search index. We will store text-search 
                //indexes concerning customers in table "TS_Customers".
                //Second parameter is a reference to the customer ID.
                t.TextInsert("TS_Video", comment.VideoId.ToBytes(), comment.VideoId);

                //Committing entry
                t.Commit();
            }
        }
    }
}
