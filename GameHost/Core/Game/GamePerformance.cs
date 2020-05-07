using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace GameHost.Core.Game
{
    public static class GamePerformance
    {
        private static ConcurrentDictionary<string, long> _TimeSpanByType = new ConcurrentDictionary<string, long>();

        public static void Set(string title, TimeSpan elapsed)
        {
            _TimeSpanByType[title] = elapsed.Ticks;
        }

        public static TimeSpan Get(string title)
        {
            return TimeSpan.FromTicks(_TimeSpanByType[title]);
        }

        public static IReadOnlyDictionary<string, long> GetAll()
        {
            return _TimeSpanByType;
        }
    }
}
