using System;
using DefaultEcs;

namespace GameHost.Utility
{
	public class DefaultEntity<T>
	{
		public readonly Entity Entity;

		public DefaultEntity(Entity entity)
		{
			if (!entity.Has<T>())
				throw new InvalidOperationException($"{entity} does not implement {typeof(T)}");

			Entity = entity;
		}

		public static DefaultEntity<T> Create(World world, T val)
		{
			var ent = world.CreateEntity();
			ent.Set(val);
			return new DefaultEntity<T>(ent);
		}
	}
}