using System;
using System.Collections.Generic;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource
{
	public class GameResourceDb<TResourceDescription, TKey> : IDisposable
		where TResourceDescription : IGameResourceDescription
		where TKey : IEquatable<TKey>
	{
		public struct Key__ : IComponentData
		{
			public TKey Data;
		}

		private readonly GameWorld gameWorld;

		private readonly Dictionary<GameEntity, TKey> entityToKey;
		private readonly Dictionary<TKey, GameEntity> keyToEntity;

		public GameResourceDb(GameWorld gameWorld, int capacity = 0)
		{
			this.gameWorld = gameWorld;
			
			entityToKey = new Dictionary<GameEntity, TKey>(capacity);
			keyToEntity = new Dictionary<TKey, GameEntity>(capacity);
		}

		public GameResource<TResourceDescription> GetOrCreate(TKey key)
		{
			if (!keyToEntity.TryGetValue(key, out var entity))
			{
				keyToEntity[key] = gameWorld.CreateEntity();
			}

			gameWorld.AddComponent(entity, new Key__ {Data = key});
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