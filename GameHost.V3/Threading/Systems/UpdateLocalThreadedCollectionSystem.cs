﻿using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Threading.V2;
using GameHost.V3.Utility;

namespace GameHost.V3.Threading.Systems
{
	public class UpdateLocalThreadedCollectionSystem : AppSystem
	{
		private World _world;
		private IDomainUpdateLoopSubscriber _updateLoop;

		public UpdateLocalThreadedCollectionSystem(Scope scope) : base(scope)
		{
			Dependencies.AddRef(() => ref _world);
			Dependencies.AddRef(() => ref _updateLoop);
		}

		private EntitySet _collectionSet;
		
		protected override void OnInit()
		{
			Disposables.AddRange(new IDisposable[]
			{
				_collectionSet = _world.GetEntities()
					.With<ListenerCollectionBase>()
					.AsSet(),

				// ReSharper disable HeapView.BoxingAllocation
				_updateLoop.Subscribe(OnUpdate, builder =>
				{
					// todo: update right after AddListenerToCollectionSystem
				})
				// ReSharper restore HeapView.BoxingAllocation
			});

			_world.Set<TimeToSleep>();
		}

		private void OnUpdate(WorldTime worldTime)
		{
			var sleep = TimeSpan.MaxValue;
			foreach (var entity in _collectionSet.GetEntities())
			{
				var collection = entity.Get<ListenerCollectionBase>();
				if (!collection.CanCallUpdateFromCurrentContext())
					return;

				sleep = new TimeSpan(Math.Min(collection.Update().Ticks, sleep.Ticks));
			}

			if (sleep == TimeSpan.MaxValue)
				sleep = TimeSpan.FromSeconds(0.1);

			_world.Set(new TimeToSleep {Span = sleep});
		}
	}

	public struct TimeToSleep
	{
		public TimeSpan Span;
	}
}