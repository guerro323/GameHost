using System;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.Utility.EntityQuery
{
    public ref struct FinalizedQuery
    {
        public Span<ComponentType> All;
        public Span<ComponentType> Or;
        public Span<ComponentType> None;
    }
}