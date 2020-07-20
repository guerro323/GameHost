using System;

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

			var prevLength = column.componentTypes.Length;
			GetColumn(row, ref column.componentTypes) = componentTypes.ToArray();
			if (prevLength != column.componentTypes.Length)
				Array.Fill(column.componentTypes, Array.Empty<uint>(), prevLength, column.componentTypes.Length - prevLength);
			
			return row;
		}
	}
}