using System;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct GameEntity : IEquatable<GameEntity>
	{
		public static bool operator ==(GameEntity left, GameEntity right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameEntity left, GameEntity right)
		{
			return !left.Equals(right);
		}

		// Special syntax, return the first non-default entity.
		public static GameEntity operator |(GameEntity left, GameEntity right)
		{
			if (left != default)
				return left;
			return right;
		}

		// Special syntax, return the last non-default entity.
		public static GameEntity operator&(GameEntity left, GameEntity right)
		{
			if (right != default)
				return right;
			return left;
		}

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

		public override string ToString()
		{
			return $"(GameEntity Row={Id})";
		}
	}
}