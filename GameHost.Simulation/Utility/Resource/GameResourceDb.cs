using System;
using System.Collections.Generic;
using System.Diagnostics;
using BidirectionalMap;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Injection;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.Resource.Components;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource
{
	public class GameResourceDb<TResourceDescription> : AppObject
		where TResourceDescription : struct, IGameResourceDescription, IEquatable<TResourceDescription>
	{
		private GameWorld gameWorldRef;

		public GameWorld GameWorld => gameWorldRef;

		private BiMap<GameEntity, TResourceDescription> GetResourceMap() => stateEntity.Get<BiMap<GameEntity, TResourceDescription>>();

		private Entity stateEntity;

		public Entity StateEntity
		{
			get => stateEntity;
			set
			{
				if (stateEntity.IsAlive)
					stateEntity.Dispose();

				stateEntity = value;
				
				if (!stateEntity.Has<BiMap<GameEntity, TResourceDescription>>())
					stateEntity.Set(new BiMap<GameEntity, TResourceDescription>());
			}
		}

		public GameResourceDb(Context context) : base(context)
		{
			StateEntity = new ContextBindingStrategy(context, true).Resolve<World>()
			                                                       .CreateEntity();

			if (context != null)
			{
				DependencyResolver.Add(() => ref gameWorldRef);
				DependencyResolver.OnComplete(_ => { });
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
				entityResourceMap.Add(entity = GameWorld.CreateEntity(), resourceDesc);
			else
				entity = entityResourceMap.Reverse[resourceDesc];

			GameWorld.AddComponent(entity, resourceDesc);
			GameWorld.AddComponent(entity, new IsResourceEntity());
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