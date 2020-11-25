using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BidirectionalMap;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Injection;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.Resource.Components;
using GameHost.Simulation.Utility.Resource.Interfaces;
using GameHost.Utility;

namespace GameHost.Simulation.Utility.Resource
{
	public class GameResourceDb<TResourceDescription> : AppObject
		where TResourceDescription : struct, IGameResourceDescription, IEquatable<TResourceDescription>
	{
		public class Defaults : BiMap<GameEntity, TResourceDescription>
		{
		}

		private GameWorld gameWorldRef;

		public GameWorld GameWorld => gameWorldRef;

		private BiMap<GameEntity, TResourceDescription> GetResourceMap() => stateEntity.Get<Defaults>();

		private Entity stateEntity;

		public Entity StateEntity
		{
			get => stateEntity;
			set
			{
				if (stateEntity.IsAlive)
					stateEntity.Dispose();

				stateEntity = value;

				if (!stateEntity.Has<Defaults>())
					stateEntity.Set(new Defaults());
			}
		}

		public GameResourceDb(Context context) : base(context)
		{
			DependencyResolver.Add<DefaultEntity<Defaults>>();
			DependencyResolver.OnComplete(deps =>
			{
				StateEntity = deps.OfType<DefaultEntity<Defaults>>()
				                  .First()
				                  .Entity;
			});

			if (context != null)
			{
				DependencyResolver.Add(() => ref gameWorldRef);
			}
		}

		public GameResourceDb(GameWorld gameWorld) : this((Context) null)
		{
			gameWorldRef = gameWorld;
		}

		public GameResource<TResourceDescription> GetOrCreate(TResourceDescription resourceDesc)
		{
			if (DependencyResolver != null)
				Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");

			var entityResourceMap = GetResourceMap();

			GameEntity entity;
			if (!entityResourceMap.Reverse.ContainsKey(resourceDesc))
				entityResourceMap.Add(entity = GameWorld.Safe(GameWorld.CreateEntity()), resourceDesc);
			else
				entity = entityResourceMap.Reverse[resourceDesc];

			GameWorld.AddComponent(entity.Handle, resourceDesc);
			GameWorld.AddComponent(entity.Handle, new IsResourceEntity());
			return new GameResource<TResourceDescription>(entity);
		}

		public bool TryGet(TResourceDescription key, out GameResource<TResourceDescription> resource)
		{
			var entityResourceMap = GetResourceMap();
			if (entityResourceMap.Reverse.ContainsKey(key))
			{
				resource = new GameResource<TResourceDescription>(entityResourceMap.Reverse[key]);
				return true;
			}

			resource = default;
			return false;
		}
	}
}