using DefaultEcs;
using ImTools;

namespace GameHost.Entities
{
    public struct WorldTime
    {
        public double Total;
        public float  Delta;
    }

    public interface IManagedWorldTime
    {
        public double Total { get; }
        public float Delta { get; }
    }

    public class ManagedWorldTime : IManagedWorldTime
    {
        public double Total { get; private set; }
        public float  Delta { get; private set; }

        public void Update(Entity source)
        {
            var wt = source.Get<WorldTime>();
            Total = wt.Total;
            Delta = wt.Delta;
        }
    }
}
