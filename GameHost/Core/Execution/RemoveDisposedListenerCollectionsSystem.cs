using Collections.Pooled;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Threading;

namespace GameHost.Core.Execution
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RemoveDisposedListenerCollectionsSystem : AppSystem
	{
		private PooledList<Entity> toRemove = new();

		public RemoveDisposedListenerCollectionsSystem(WorldCollection collection) : base(collection)
		{
			AddDisposable(toRemove);
		}

		private EntitySet collectionSet;

		protected override void OnInit()
		{
			collectionSet = World.Mgr.GetEntities()
			                     .With<ListenerCollectionBase>()
			                     .AsSet();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			toRemove.Clear();
			foreach (var entity in collectionSet.GetEntities())
			{
				if (false == entity.Get<ListenerCollectionBase>().IsDisposed)
					continue;

				toRemove.Add(entity);
			}

			foreach (var entity in toRemove)
				entity.Dispose();
		}
	}
}