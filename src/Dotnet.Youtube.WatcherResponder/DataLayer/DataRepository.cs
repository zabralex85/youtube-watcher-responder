using System;
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
            _engine = new DBreezeEngine(path + @"\db.dat");
        }

        public bool Exists<T>(T obj)
        {
            if (obj is Video video)
            {
                using (var tran = _engine.GetTransaction())
                {
                    var exVideo = tran.Select<string, Models.Video>("video", video.VideoId);
                    return exVideo.Exists;
                }
            }
            else if (obj is VideoComment comment)
            {
                using (var tran = _engine.GetTransaction())
                {
                    var exVideo = tran.Select<string, Models.VideoComment>("video_comment", comment.Id);
                    return exVideo.Exists;
                }
            }

            return false;
        }
    }
}
