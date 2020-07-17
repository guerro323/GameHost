using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Threading;

namespace GameHost.Core.Execution
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	[UpdateAfter(typeof(AddListenerToCollectionSystem))]
	public class UpdateLocalThreadedCollectionSystem : AppSystem
	{
		public UpdateLocalThreadedCollectionSystem(WorldCollection collection) : base(collection)
		{
		}

		private EntitySet collectionSet;

		protected override void OnInit()
		{
			collectionSet = World.Mgr.GetEntities()
			                     .With<ListenerCollectionBase>()
			                     .AsSet();

			if (World.Mgr.Get<TimeToSleep>().Length == 0)
				World.Mgr.CreateEntity().Set(new TimeToSleep());
		}

		protected override void OnUpdate()
		{
			var sleep = TimeSpan.MaxValue;
			foreach (var entity in collectionSet.GetEntities())
			{
				var collection = entity.Get<ListenerCollectionBase>();
				if (!collection.CanCallUpdateFromCurrentContext())
					return;
					
				sleep = new TimeSpan(Math.Min(collection.Update().Ticks, sleep.Ticks));
			}

			if (sleep == TimeSpan.MaxValue)
				sleep = TimeSpan.FromSeconds(0.1);

			World.Mgr.Get<TimeToSleep>()[0].Span = sleep;
		}
	}

	public struct TimeToSleep
	{
		public TimeSpan Span;
	}
}