﻿using System;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.Boards
{
	/// <summary>
	///     A container that contains archetype of components.
	/// </summary>
	/// <remarks>
	///     For fast access, it first require the sum of all components IDs (a bit like a hash), and then compare with existing
	///     archetypes.
	///     If an archetype match the sum, it will check for the length and then, an operation to check if they have the same
	///     components.
	/// </remarks>
	public class ArchetypeBoardContainer : BoardWithRowCollectionBase
    {
        private (uint[] sum, uint[][] componentTypes, PooledList<uint>[] entity, byte h) column;

        public ArchetypeBoardContainer(int capacity) : base(capacity)
        {
            column.componentTypes = new uint[0][];
            column.sum = new uint[0];
            column.entity = new PooledList<uint>[0];
        }

        public Span<EntityArchetype> Registered => MemoryMarshal.Cast<uint, EntityArchetype>(Rows.UsedRows);

        public override void CreateRowBulk(Span<uint> rows)
        {
            base.CreateRowBulk(rows);
            if (Rows.MaxId >= column.componentTypes.Length)
                OnResize();
        }

        public override uint CreateRow()
        {
            var row = base.CreateRow();
            if (Rows.MaxId >= column.componentTypes.Length)
                OnResize();

            return row;
        }

        protected virtual void OnResize()
        {
            var previousLength = column.componentTypes.Length;
            Array.Resize(ref column.componentTypes, (int) (Rows.MaxId + 1) * 2);
            Array.Resize(ref column.entity, (int) (Rows.MaxId + 1) * 2);
            for (var i = previousLength; i < column.componentTypes.Length; i++)
            {
                // Since there is pooling, we only create if needed
                column.componentTypes[i] ??= Array.Empty<uint>();
                column.entity[i] ??= new PooledList<uint>();
            }
        }

        public uint GetOrCreateRow(Span<uint> componentTypes, bool isOrdered)
        {
            if (!isOrdered)
                throw new NotImplementedException("Only ordered components is supported for now");

            uint sum = 0;
            for (var i = 0; i < componentTypes.Length; i++) sum += componentTypes[i];

            // it is possible to vectorize this?
            for (var i = 1; i < column.componentTypes.Length; i++)
            {
                if (column.sum[i] != sum)
                    continue;

                if (column.componentTypes[i]
                    .AsSpan()
                    .SequenceEqual(componentTypes))
                    return (uint) i;
            }

            return CreateArchetype(componentTypes, sum);
        }

        public uint CreateArchetype(Span<uint> componentTypes, uint sum = 0)
        {
            if (sum == 0)
                for (var i = 0; i < componentTypes.Length; i++)
                    sum += componentTypes[i];

            var row = CreateRow();
            GetColumn(row, ref column.sum) = sum;
            GetColumn(row, ref column.componentTypes) = componentTypes.ToArray();

            return row;
        }

        public void AddEntity(uint row, uint entity)
        {
            column.entity[row].Add(entity);
        }

        public void RemoveEntity(uint row, uint entity)
        {
            column.entity[row].Remove(entity);
        }

        public Span<uint> GetComponentTypes(uint row)
        {
            return column.componentTypes[row];
        }

        public Span<uint> GetEntities(uint row)
        {
#if DEBUG
            if (row >= column.entity.Length)
                throw new IndexOutOfRangeException($"{row} > {column.entity.Length}");
#endif
            return column.entity[row].Span;
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var list in column.entity)
                list.Dispose();
            column.entity = null;
            column.sum = null;
            column.componentTypes = null;
        }

        public override void Clear()
        {
            base.Clear();

            foreach (var list in column.entity)
                list.Clear();

            column.sum.AsSpan().Clear();
            column.componentTypes.AsSpan().Clear();
        }
    }
}