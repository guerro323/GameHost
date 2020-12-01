using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.Features
{
	public class GetFeature<T> : List<(Entity entity, T feature)>
		where T : IFeature
	{
		private readonly Func<IFeature, bool> isFeatureValid;

		public event Action<Entity, T> OnFeatureAdded;
		public event Action<Entity, T> OnFeatureRemoved;

		public GetFeature(WorldCollection worldCollection, Func<IFeature, bool> isFeatureValid)
		{
			this.isFeatureValid = isFeatureValid;

			using (var set = worldCollection.Mgr.GetEntities()
			                                .With<IFeature>()
			                                .AsSet())
			{
				foreach (ref readonly var entity in set.GetEntities())
					TryAddFeature(entity, entity.Get<IFeature>());
			}

			worldCollection.Mgr.SubscribeComponentAdded((in   Entity entity, in IFeature value) => TryAddFeature(entity, value));
			worldCollection.Mgr.SubscribeComponentRemoved((in Entity entity, in IFeature value) => RemoveFeature(entity, value));
		}

		private bool TryAddFeature(Entity entity, IFeature feature)
		{
			if (!isFeatureValid(feature))
				return false;

			Add((entity, (T) feature));
			OnFeatureAdded?.Invoke(entity, (T) feature);
			return true;
		}

		private void RemoveFeature(Entity entity, IFeature feature)
		{
			if (feature is T asT)
			{
				Remove((entity, asT));
				OnFeatureRemoved?.Invoke(entity, asT);
			}
		}
	}
}