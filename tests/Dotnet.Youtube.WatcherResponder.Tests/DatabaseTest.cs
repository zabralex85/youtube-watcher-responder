using Dotnet.Youtube.WatcherResponder.DataLayer;
using Dotnet.Youtube.WatcherResponder.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Dotnet.Youtube.WatcherResponder.Tests
{
    [TestClass]
    public class DatabaseTest
    {
        private readonly DataRepository _repository;
        private readonly VideoComment _comment;

        public DatabaseTest()
        {
            _repository = new DataRepository();
            _comment = new VideoComment()
            {
                AuthorDisplayName = "Name",
                TextOriginal = "Text",
                Id = "1-1-1",
                VideoId = "2-2-2"
            };
        }

        [TestMethod]
        public void InsertMethod()
        {
            _repository.AddAuthorComment(_comment);
        }

        [TestMethod]
        public void SelectMethod()
        {
            var result = _repository.CommentExists(_comment.VideoId);
            Assert.IsTrue(result);
        }
    }
}
