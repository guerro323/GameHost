using System;
using Collections.Pooled;

namespace GameHost.Simulation.TabEcs
{
	public class BufferComponentBoard : ComponentBoardBase
	{
		private (PooledList<byte>[] buffers, byte h) column;

		public BufferComponentBoard(int size, int capacity) : base(size, capacity)
		{
			column.buffers = new PooledList<byte>[0];
		}

		public override void CreateRowBulk(Span<uint> rows)
		{
			base.CreateRowBulk(rows);
			if (board.MaxId * Size >= column.buffers.Length)
			{
				Array.Resize(ref column.buffers, (int) ((board.MaxId + 1) * Size * 2));
			}

			foreach (ref readonly var row in rows)
				column.buffers[row] = new PooledList<byte>();
		}

		public override uint CreateRow()
		{
			var row = base.CreateRow();
			if (board.MaxId * Size >= column.buffers.Length)
			{
				Array.Resize(ref column.buffers, (int) ((board.MaxId + 1) * Size * 2));
			}

			column.buffers[row] = new PooledList<byte>();

			return row;
		}

		public override bool DeleteRow(uint row)
		{
			column.buffers[row].Dispose();
			column.buffers[row] = null;
			
			return base.DeleteRow(row);
		}

		public Span<PooledList<byte>> AsSpan()
		{
			return column.buffers;
		}
	}
}