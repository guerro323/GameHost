using System;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct GameEntity : IEquatable<GameEntity>
	{
		public readonly uint Id;

		public GameEntity(uint id)
		{
			Id = id;
		}

		public bool Equals(GameEntity other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is GameEntity other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}
	}
}