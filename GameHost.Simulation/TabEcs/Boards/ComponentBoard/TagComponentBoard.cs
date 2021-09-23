using System;

namespace GameHost.Simulation.TabEcs.Boards.ComponentBoard
{
    public class TagComponentBoard : ComponentBoardBase
    {
        public TagComponentBoard(int capacity) : base(0, capacity)
        {
        }

        internal static class Default<T>
        {
            [ThreadStatic] public static T V;
        }
    }
}