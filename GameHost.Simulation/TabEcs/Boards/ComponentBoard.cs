using System;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.Boards
{
    public abstract class ComponentBoardBase : BoardWithRowCollectionBase
    {
        public readonly int Size;

        private (GameEntityHandle[] owner, PooledList<GameEntityHandle>[] references, byte h) column;

        public ComponentBoardBase(int size, int capacity) : base(capacity)
        {
            Size = size;

            column.owner = new GameEntityHandle[0];
            column.references = new PooledList<GameEntityHandle>[0];
        }

        public Span<GameEntityHandle> OwnerColumn => column.owner;

        protected virtual void OnResize()
        {
            Array.Resize(ref column.owner, (int) (Rows.MaxId + 1) * 2);

            var previousLength = column.references.Length;
            Array.Resize(ref column.references, (int) (Rows.MaxId + 1) * 2);
            for (var i = previousLength; i < column.references.Length; i++)
                column.references[i] = new PooledList<GameEntityHandle>();
        }

        public override void CreateRowBulk(Span<uint> rows)
        {
            base.CreateRowBulk(rows);
            if (Rows.MaxId >= column.owner.Length)
                OnResize();
        }

        public override uint CreateRow()
        {
            var row = base.CreateRow();
            if (Rows.MaxId >= column.owner.Length)
                OnResize();

            return row;
        }

        public virtual int AddReference(uint row, in GameEntityHandle entityHandle)
        {
            ref var list = ref GetColumn(row, ref column.references);
            list.Add(entityHandle);
            return list.Count;
        }

        public virtual int RemoveReference(uint row, in GameEntityHandle entityHandle)
        {
            ref var list = ref GetColumn(row, ref column.references);
            list.Remove(entityHandle);
            return list.Count;
        }

        public virtual Span<GameEntityHandle> GetReferences(uint row)
        {
            return GetColumn(row, ref column.references).Span;
        }
    }
}