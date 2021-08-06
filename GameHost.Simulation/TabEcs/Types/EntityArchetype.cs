using System;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct EntityArchetype : IEquatable<EntityArchetype>
	{
		public readonly uint Id;

		public EntityArchetype(uint id)
		{
			Id = id;
		}

		public bool Equals(EntityArchetype other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is EntityArchetype other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int)Id;
		}
	}
}