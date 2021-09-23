using System;

namespace GameHost.Simulation.TabEcs
{
    public abstract class GameWorldBoardBase : IDisposable
    {
        public readonly GameWorld World;

        public GameWorldBoardBase(GameWorld gameWorld)
        {
            World = gameWorld;
        }

        public abstract void Dispose();
    }
}