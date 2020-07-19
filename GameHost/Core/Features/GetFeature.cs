using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.Features
{
	public class GetFeature<T> : List<T>
		where T : IFeature
	{
		private readonly Func<IFeature, bool> isFeatureValid;

		public event Action<T> OnFeatureAdded;
		public event Action<T> OnFeatureRemoved;

		public GetFeature(WorldCollection worldCollection, Func<IFeature, bool> isFeatureValid)
		{
			this.isFeatureValid = isFeatureValid;

			foreach (var feature in worldCollection.Mgr.Get<IFeature>())
				TryAddFeature(feature);

			worldCollection.Mgr.SubscribeComponentAdded((in   Entity entity, in IFeature value) => TryAddFeature(value));
			worldCollection.Mgr.SubscribeComponentRemoved((in Entity entity, in IFeature value) => RemoveFeature(value));
		}

		private bool TryAddFeature(IFeature feature)
		{
			if (!isFeatureValid(feature))
				return false;

			Add((T) feature);
			OnFeatureAdded?.Invoke((T) feature);
			return true;
		}

		private void RemoveFeature(IFeature feature)
		{
			if (feature is T asT)
			{
				Remove(asT);
				OnFeatureRemoved?.Invoke(asT);
			}
		}
	}
}