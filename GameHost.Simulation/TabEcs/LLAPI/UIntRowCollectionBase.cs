using System;
using System.Collections.Generic;

namespace GameHost.Simulation.TabEcs.LLAPI
{
	/// <summary>
	///     A board contains user-defined columns and rows as an ID format.
	/// </summary>
	public struct UIntRowCollectionBase : IRowCollection<uint>
    {
        private readonly Queue<uint> unusedRows;
        private uint[] usedRows;


        public int Count { get; private set; }

        public uint MaxId { get; private set; }

        public Span<uint> UsedRows => new(usedRows, 0, Count);

        public UIntRowCollectionBase(int capacity)
        {
            unusedRows = new Queue<uint>(capacity);
            MaxId = 1;

            usedRows = Array.Empty<uint>();
            Count = 0;
        }

        public bool TrySetUnusedRow(uint position)
        {
            if (position > MaxId)
                return false;

            unusedRows.Enqueue(position);

            var usedIndex = Array.IndexOf(usedRows, position);
            if (usedIndex < 0)
                throw new InvalidOperationException("");

            // swapback
            Count--;
            for (var i = usedIndex; i <= Count; i++) usedRows[i] = usedRows[i + 1];

            return true;
        }

        public void CreateRowBulk(Span<uint> rows)
        {
            var id = MaxId;
            var length = rows.Length;
            while (unusedRows.Count > 0 && length-- > 0)
            {
                // the reason we do not use TryDequeue is to keep the original ID intact,
                // in case we don't have any recycled ids (since calling this method will reset the variable)
                id = unusedRows.Dequeue();

                usedRows[Count++] = id;
                rows[length] = id;
            }

            if (
                length <= 0) // If it's 0 that mean we've used all unused rows, or if it's less than 0, this mean we created enough rows.
                return;

            MaxId += (uint) length; // adding one is necessary here, since ids start from 1
            if (MaxId >= usedRows.Length) Array.Resize(ref usedRows, (int) (MaxId + 1) * 2);

            length--; // necessary for the FOR operation to execute correctly
            for (; id < MaxId; id++)
            {
                rows[length--] = id;
                usedRows[Count++] = id;
            }
        }

        public uint CreateRow()
        {
            if (unusedRows.TryDequeue(out var id))
            {
                usedRows[Count++] = id;
                return id;
            }

            if (MaxId >= usedRows.Length) Array.Resize(ref usedRows, (int) (MaxId + 1) * 2);

            usedRows[Count++] = MaxId;
            return MaxId++;
        }

        public ref T GetColumn<T>(uint row, ref T[] array)
        {
            if (MaxId >= array.Length)
                Array.Resize(ref array, (int) ((MaxId + 1) * 2));
            return ref array[row];
        }

        public void Clear()
        {
            unusedRows.Clear();
            MaxId = 1;

            usedRows.AsSpan().Clear();
            Count = 0;
        }
    }
}