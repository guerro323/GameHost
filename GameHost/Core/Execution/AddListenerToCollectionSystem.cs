using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Threading;

namespace GameHost.Core.Execution
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class AddListenerToCollectionSystem : AppSystem
	{
		public AddListenerToCollectionSystem(WorldCollection collection) : base(collection)
		{
		}

		private EntitySet listenerSet;

		protected override void OnInit()
		{
			listenerSet = World.Mgr.GetEntities()
			                   .With<IListener>()
			                   .With((in PushToListenerCollection value) => value.Entity.IsAlive)
			                   .AsSet();
		}

		protected override void OnUpdate()
		{
			foreach (var entity in listenerSet.GetEntities())
			{
				var listener   = entity.Get<IListener>();
				var collection = entity.Get<PushToListenerCollection>().Entity.Get<ListenerCollectionBase>();

				if (entity.Has<ListenerCollectionTarget>())
				{
					var currentEntity = entity.Get<ListenerCollectionTarget>().Entity;
					if (currentEntity.IsAlive)
					{
						var previous = currentEntity.Get<ListenerCollectionBase>();
						previous.RemoveListener(listener);
					}
					// in case there is an exception in the next line, be sure that this brace will not get re-invoked.
					entity.Remove<ListenerCollectionTarget>();
				}

				var keys = Array.Empty<IListenerKey>();
				if (entity.Has<IListenerKey[]>())
					keys = entity.Get<IListenerKey[]>();

				collection.AddListener(listener, keys);
				entity.Set(new ListenerCollectionTarget(entity.Get<PushToListenerCollection>().Entity));
				entity.Remove<PushToListenerCollection>();
			}
		}
	}
}