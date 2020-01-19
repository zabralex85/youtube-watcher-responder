using System;
using System.Threading;

namespace Dotnet.Youtube.WatcherResponder.Utils
{
    public static class RandomProvider
    {
        private static int _seed = Environment.TickCount;

        private static readonly ThreadLocal<Random> RandomWrapper = new ThreadLocal<Random>(() =>
            new Random(Interlocked.Increment(ref _seed))
        );

        public static Random GetThreadRandom()
        {
            return RandomWrapper.Value;
        }
    }
}
