using System;
using System.Collections.Generic;
using GameHost.Core.Ecs;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.Resource.Systems
{
	public class KeepAliveCollectionSystem : AppSystem
	{
		private readonly Dictionary<Type, List<KeepAliveResourceSystemBase>> systemsPerResource;

		public KeepAliveCollectionSystem(WorldCollection collection) : base(collection)
		{
			systemsPerResource = new Dictionary<Type, List<KeepAliveResourceSystemBase>>();
		}

		public void Add(Type resourceType, KeepAliveResourceSystemBase system)
		{
			systemsPerResource.TryAdd(resourceType, new List<KeepAliveResourceSystemBase>());
			if (!systemsPerResource[resourceType].Contains(system))
				systemsPerResource[resourceType].Add(system);
		}

		public IReadOnlyList<KeepAliveResourceSystemBase> GetSystems(Type type)
		{
			if (systemsPerResource.TryGetValue(type, out var list))
				return list;
			return Array.Empty<KeepAliveResourceSystemBase>();
		}
	}

	public abstract class KeepAliveResourceSystemBase : AppSystem
	{
		private KeepAliveCollectionSystem collection;

		public abstract Type ResourceType { get; }

		protected KeepAliveResourceSystemBase(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref this.collection);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			collection.Add(ResourceType, this);
		}

		protected internal abstract void KeepAlive(Span<bool> keep, Span<GameEntity> resources);
	}
}