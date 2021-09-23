using System;

namespace GameHost.V3.Threading.V2.Apps
{
    public interface IReadOnlyDomainWorker
    {
        string Name { get; }

        TimeSpan OptimalDeltaTarget { get; }
        TimeSpan Elapsed { get; }
        TimeSpan Delta { get; }
        float Performance { get; }
    }
}