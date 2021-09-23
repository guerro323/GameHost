using System;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Types;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource
{
    public readonly struct GameResource<T> : IEquatable<GameResource<T>>
        where T : IGameResourceDescription
    {
        public static bool operator ==(GameResource<T> left, GameResource<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GameResource<T> left, GameResource<T> right)
        {
            return !left.Equals(right);
        }

        public readonly GameEntity Entity;

        public GameEntityHandle Handle => Entity.Handle;

        public GameResource(GameEntity target)
        {
            Entity = target;
        }

        public bool Equals(GameResource<T> other)
        {
            return Entity.Equals(other.Entity);
        }

        public override bool Equals(object obj)
        {
            return obj is GameResource<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Entity.GetHashCode();
        }
    }
}