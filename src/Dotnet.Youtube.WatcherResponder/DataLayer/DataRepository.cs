using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
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

            //Setting default serializer for DBreeze
            //DBreeze.Utils.CustomSerializator.ByteArraySerializator = ProtobufSerializer.SerializeProtobuf;
            //DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator = ProtobufSerializer.DeserializeProtobuf;

            DBreeze.Utils.CustomSerializator.ByteArraySerializator = (object o) =>
            {
                return JsonSerializer.Serialize(o).To_UTF8Bytes();
            };

            DBreeze.Utils.CustomSerializator.ByteArrayDeSerializator = (byte[] bt, Type t) =>
            {
                return JsonSerializer.Deserialize(bt, t);
            };

        }

        public bool CommentExists(string videoId)
        {
            bool exists = false;

            using (var t = _engine.GetTransaction())
            {
                t.SynchronizeTables("VideoComment");

                var obj = t
                    .Select<byte[], byte[]>("VideoComment", 1.ToIndex(videoId))
                    .ObjectGet<Models.VideoComment>();

                if (obj != null)
                {
                    exists = true;
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
                t.SynchronizeTables("VideoComment");

                //Documentation https://goo.gl/YtWnAJ
                t.ObjectInsert("VideoComment", new DBreeze.Objects.DBreezeObject<Models.VideoComment>
                {
                    NewEntity = true,
                    Entity = comment,
                    Indexes = new List<DBreeze.Objects.DBreezeIndex>
                    {
                        new DBreeze.Objects.DBreezeIndex(1, comment.VideoId) {PrimaryIndex = true},
                    }
                }, false);


                //Committing entry
                t.Commit();
            }
        }
    }

    //[ProtoBuf.ProtoContract]
    //public class Customer
    //{
    //    [ProtoBuf.ProtoMember(1, IsRequired = true)]
    //    public long Id { get; set; }

    //    [ProtoBuf.ProtoMember(2, IsRequired = true)]
    //    public string Name { get; set; }
    //}

    //public class ProtobufSerializer
    //{
    //    public static byte[] SerializeProtobuf(object arg)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public static object DeserializeProtobuf(byte[] arg1, Type arg2)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
