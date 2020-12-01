using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.Features.Systems
{
	public class AppSystemWithFeature<T> : AppSystem
		where T : IFeature
	{
		private readonly Func<IFeature, bool> isFeatureValid;

		protected GetFeature<T> Features;

		public AppSystemWithFeature(Func<IFeature, bool> isFeatureValid, WorldCollection collection) : base(collection)
		{
			this.isFeatureValid = isFeatureValid;

			DependencyResolver.Add(() => ref Features, new FeatureDependencyStrategy<T>(collection, isFeatureValid));
		}

		public AppSystemWithFeature(WorldCollection collection) : this(f => f is T, collection)
		{
		}

		public override bool CanUpdate()
		{
			return base.CanUpdate() && Features.Count > 0;
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			Features.OnFeatureAdded   += OnFeatureAdded;
			Features.OnFeatureRemoved += OnFeatureRemoved;
			
			foreach (var (entity, feature) in Features)
				OnFeatureAdded(entity, feature);
		}

		protected virtual void OnFeatureAdded(Entity entity, T obj)
		{
		}

		protected virtual void OnFeatureRemoved(Entity entity, T obj)
		{
		}
	}
}