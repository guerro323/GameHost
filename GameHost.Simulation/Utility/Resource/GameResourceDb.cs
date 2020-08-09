using System;
using System.Collections.Generic;
using System.Diagnostics;
using GameHost.Core.Ecs;
using GameHost.Injection;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.Resource.Components;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource
{
	public class GameResourceDb<TResourceDescription, TKey> : AppObject
		where TResourceDescription : IGameResourceDescription
		where TKey : struct, IEquatable<TKey>, IGameResourceKeyDescription
	{
		private GameWorld gameWorldRef;
		
		public GameWorld GameWorld => gameWorldRef;

		private readonly Dictionary<GameEntity, TKey> entityToKey;
		private readonly Dictionary<TKey, GameEntity> keyToEntity;

		public GameResourceDb(Context context) : base(context)
		{
			entityToKey = new Dictionary<GameEntity, TKey>(0);
			keyToEntity = new Dictionary<TKey, GameEntity>(0);
			
			if (context != null)
			{
				DependencyResolver.Add(() => ref gameWorldRef);
				DependencyResolver.OnComplete(objs => { });
			}
		}

		public GameResourceDb(GameWorld gameWorld) : this((Context) null)
		{
			gameWorldRef = gameWorld;
		}

		public GameResource<TResourceDescription> GetOrCreate(TKey key)
		{
			if (DependencyResolver != null)
				Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");

			if (!keyToEntity.TryGetValue(key, out var entity))
			{
				keyToEntity[key] = entity = GameWorld.CreateEntity();
			}

			GameWorld.AddComponent(entity, new GameResourceKey<TKey> {Value = key});
			GameWorld.AddComponent(entity, new IsResourceEntity());
			return new GameResource<TResourceDescription>(entity);
		}

		public bool TryGet(TKey key, out GameResource<TResourceDescription> resource)
		{
			var result = keyToEntity.TryGetValue(key, out var entity);
			resource = new GameResource<TResourceDescription>(entity);
			return result;
		}

		public TKey GetKey(in GameResource<TResourceDescription> resource)
		{
			return entityToKey[resource.Entity];
		}

		public void Dispose()
		{
		}
	}
}