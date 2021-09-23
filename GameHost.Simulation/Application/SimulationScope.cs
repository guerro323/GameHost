using DefaultEcs;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.EntitySystem;
using GameHost.V3;

namespace GameHost.Simulation.Application
{
    public class SimulationScope : Scope
    {
        public readonly World World;
        public readonly GameWorld GameWorld;

        public SimulationScope(Scope parent) : base(new ChildScopeContext(parent.Context))
        {
            Context.Register(World = new World());
            Context.Register(GameWorld = new GameWorld());
        }

        public override void Dispose()
        {
            base.Dispose();

            World.Dispose();
            GameWorld.Dispose();
        }
    }
}