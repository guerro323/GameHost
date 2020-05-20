using System;
using DefaultEcs;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.Entities
{
    public struct SingletonComponent<T>
    {
    }

    public struct WorldTime
    {
        public TimeSpan Total;
        public TimeSpan Delta;
    }

    public interface IManagedWorldTime
    {
        public TimeSpan Total { get; }
        public TimeSpan Delta { get; }
    }

    public class ManagedWorldTime : IManagedWorldTime
    {
        public TimeSpan Total { get; private set; }
        public TimeSpan Delta { get; private set; }

        public void Update(Entity source)
        {
            var wt = source.Get<WorldTime>();
            Total = wt.Total;
            Delta = wt.Delta;
        }
    }
}
