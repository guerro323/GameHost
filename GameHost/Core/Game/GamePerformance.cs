using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GameHost.Core.Game
{
    public static class GamePerformance
    {
        private static ConcurrentDictionary<string, long> _TimeSpanByType = new ConcurrentDictionary<string, long>();

        public static void SetElapsedDelta(string title, TimeSpan elapsed)
        {
            _TimeSpanByType[title] = elapsed.Ticks;
        }

        public static TimeSpan Get(string title)
        {
            return TimeSpan.FromTicks(_TimeSpanByType[title]);
        }

        public static int GetFps(string title)
        {
            return (int)(1 / Get(title).TotalSeconds);
        }

        public static IReadOnlyDictionary<string, long> GetAll()
        {
            return _TimeSpanByType;
        }
    }
}
