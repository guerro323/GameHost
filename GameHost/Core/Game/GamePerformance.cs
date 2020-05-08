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
            if (!_TimeSpanByType.TryGetValue(title, out var span))
                return TimeSpan.Zero;

            return TimeSpan.FromTicks(span);
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
