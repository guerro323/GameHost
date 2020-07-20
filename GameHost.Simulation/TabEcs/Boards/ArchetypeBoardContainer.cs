using System;
using System.Runtime.InteropServices;

namespace GameHost.Simulation.TabEcs
{
	/// <summary>
	/// A container that contains archetype of components.
	/// </summary>
	/// <remarks>
	/// For fast access, it first require the sum of all components IDs (a bit like a hash), and then compare with existing archetypes.
	/// If an archetype match the sum, it will check for the length and then, an operation to check if they have the same components.
	/// </remarks>
	public class ArchetypeBoardContainer : BoardContainer
	{
		private (uint[] sum, uint[][] componentTypes, byte h) column;

		public ArchetypeBoardContainer(int capacity) : base(capacity)
		{
			column.componentTypes = new uint[0][];
			column.sum = new uint[0];
		}

		public override void CreateRowBulk(Span<uint> rows)
		{
			base.CreateRowBulk(rows);
			if (board.MaxId >= column.componentTypes.Length)
				OnResize();
		}

		public override uint CreateRow()
		{
			var row = base.CreateRow();
			if (board.MaxId >= column.componentTypes.Length)
				OnResize();

			return row;
		}
		
		protected virtual void OnResize()
		{
			var previousLength = column.componentTypes.Length;
			Array.Resize(ref column.componentTypes, (int) ((board.MaxId + 1) * 2));
			for (var i = previousLength; i < column.componentTypes.Length; i++)
			{
				column.componentTypes[i] = Array.Empty<uint>();
			}
		}

		public uint GetOrCreateRow(Span<uint> componentTypes, bool isOrdered)
		{
			if (!isOrdered)
				throw new NotImplementedException("Only ordered components is supported for now");
			
			uint sum = 0;
			for (var i = 0; i < componentTypes.Length; i++)
			{
				sum += componentTypes[i];
			}

			for (var i = 0; i != column.componentTypes.Length; i++)
			{
				if (column.sum[i] != sum)
					continue;

				var existing = column.componentTypes[i];
				if (existing.Length != componentTypes.Length)
					continue;

				var length = existing.Length;
				for (var x = 0; x != length; x++)
					if (existing[x] != componentTypes[x])
						goto c;

				return (uint) i;

				c:
				continue;
			}

			return CreateArchetype(componentTypes, sum);
		}

		public uint CreateArchetype(Span<uint> componentTypes, uint sum = 0)
		{
			if (sum == 0)
			{
				for (var i = 0; i < componentTypes.Length; i++)
				{
					sum += componentTypes[i];
				}
			}

			var row = CreateRow();
			GetColumn(row, ref column.sum)            = sum;
			GetColumn(row, ref column.componentTypes) = componentTypes.ToArray();

			return row;
		}

		public Span<uint> GetComponentTypes(uint row) => column.componentTypes[row];
		
		public Span<EntityArchetype> Registered => MemoryMarshal.Cast<uint, EntityArchetype>(board.UsedRows);
	}
}