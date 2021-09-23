﻿using System.Runtime.CompilerServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Boards;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.Utility.EntityQuery
{
    public unsafe struct EntityQueryEnumerator
    {
        public ArchetypeBoardContainer Board;
        public PooledList<uint> Inner;
        public int InnerIndex;
        public int InnerSize;

        private GameEntityHandle current;

        public ref GameEntityHandle Current => ref *(GameEntityHandle*)Unsafe.AsPointer(ref current);

        private int index;
        private bool canMove;

        private bool MoveInnerNext()
        {
            index = 0;

            InnerIndex++;
            canMove = InnerIndex < InnerSize;

            return canMove;
        }

        public bool MoveNext()
        {
            var inner = Inner.Span;
            while (true)
            {
                // If the user set Current to default, this mean we need to decrease the index
                if (current == default)
                    index--;

                if (!canMove && !MoveInnerNext())
                    return false;

                var entitySpan = Board.GetEntities(inner[InnerIndex]);
                if (entitySpan.Length <= index)
                {
                    canMove = false;
                    continue;
                }

                current = new GameEntityHandle(entitySpan[index++]);
                return true;
            }
        }

        public EntityQueryEnumerator GetEnumerator()
        {
            return this;
        }

        public GameEntityHandle First
        {
            get
            {
                while (MoveNext())
                    return Current;
                return default;
            }
        }
    }
}