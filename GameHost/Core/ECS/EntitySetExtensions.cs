﻿using System;
using DefaultEcs;

namespace GameHost.Core.Ecs
{
	public static class EntitySetExtensions
	{
		/// <summary>
		/// Destroy all entities of a given <see cref="EntitySet"/>
		/// </summary>
		/// <param name="set">The selected entity set</param>
		public static void DisposeAllEntities(this EntitySet set)
		{
			if (set.Count == 0)
				return;

			Span<Entity> entities = stackalloc Entity[set.Count];
			set.GetEntities().CopyTo(entities);

			foreach (ref readonly var entity in entities)
				entity.Dispose();
		}

		/// <summary>
		/// Remove a component of a given <see cref="EntitySet"/>
		/// </summary>
		/// <param name="set">The selected entity set</param>
		/// <typeparam name="T">The component to remove</typeparam>
		public static void Remove<T>(this EntitySet set)
		{
			if (set.Count == 0)
				return;

			// no ref access since we do structural change
			Span<Entity> entities = stackalloc Entity[set.Count];
			set.GetEntities().CopyTo(entities);

			foreach (ref var entity in entities)
				entity.Remove<T>();
		}


		/// <summary>
		/// Remove a component of a given <see cref="EntitySet"/>
		/// </summary>
		/// <param name="set">The selected entity set</param>
		/// <typeparam name="T">The component to remove</typeparam>
		public static void Set<T>(this EntitySet set, in T data = default)
		{
			if (set.Count == 0)
				return;

			// no ref access since we do structural change
			Span<Entity> entities = stackalloc Entity[set.Count];
			set.GetEntities().CopyTo(entities);

			foreach (ref var entity in entities)
				entity.Set(data);
		}
	}
}