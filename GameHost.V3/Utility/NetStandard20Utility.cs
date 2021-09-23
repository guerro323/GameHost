using System.Collections.Concurrent;

namespace GameHost.V3.Utility
{
    public static class NetStandard20Utility
    {
#if !NETSTANDARD2_1
        public static void Clear<T>(this ConcurrentBag<T> concurrentBag)
        {
            while (!concurrentBag.IsEmpty)
                concurrentBag.TryTake(out _);
        }
#endif
    }
}