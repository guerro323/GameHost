using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Injection;
using GameHost.Simulation;
using GameHost.Threading;
using GameHost.Threading.Apps;
using GameHost.Worlds;
using GameHost.Worlds.Components;

namespace GameHost.Audio.Applications
{
	public class AudioApplication : CommonApplicationThreadListener
	{
		private TimeApp timeApp;
		private FixedTimeStep fts;

		private TimeSpan targetFrequency;
		
		private ApplicationWorker worker;

		public void SetTargetFrameRate(TimeSpan span)
		{
			Schedule(() => { fts.TargetFrameTimeMs = (int) span.TotalMilliseconds; }, default);
		}

		public AudioApplication(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
		{
			targetFrequency = TimeSpan.FromSeconds(1f / 1000f);
			timeApp = new TimeApp(Data.Context);
			fts     = new FixedTimeStep {TargetFrameTimeMs = (int) targetFrequency.TotalMilliseconds};

			worker = new ApplicationWorker("Audio");
		}

		protected override ListenerUpdate OnUpdate()
		{
			var delta       = worker.Delta;
			var updateCount = fts.GetUpdateCount(delta.TotalSeconds);
			
			var elapsed           = worker.Elapsed;
			using (worker.StartMonitoring(targetFrequency))
			{
				timeApp.Update(elapsed, delta);
				using (CurrentUpdater.SynchronizeThread())
				{
					Scheduler.Run();
					
					while (updateCount-- > 0)
					{
						timeApp.Update(elapsed - updateCount * delta, delta);
						Data.Loop();
					}
				}
			}

			var timeToSleep = TimeSpan.FromTicks(Math.Max(targetFrequency.Ticks - worker.Delta.Ticks, 0));
			if (timeToSleep.Ticks > 0)
				worker.Delta += timeToSleep;

			return new ListenerUpdate
			{
				TimeToSleep = timeToSleep
			};
		}

		private class TimeApp : AppObject
		{
			private World world;
			
			public TimeApp(Context context) : base(context)
			{
				DependencyResolver.Add(() => ref world);
				DependencyResolver.OnComplete(deps =>
				{
					worldTimeEntity = world.CreateEntity();
					managedWorldTime = new ManagedWorldTime();
				});
			}
			
			private Entity            worldTimeEntity;
			private ManagedWorldTime managedWorldTime;

			public void Update(TimeSpan total, TimeSpan delta)
			{
				if (DependencyResolver.Dependencies.Count > 0)
					return;
				
				if (!worldTimeEntity.Has<WorldTime>())
				{
					Context.BindExisting<IManagedWorldTime>(managedWorldTime);
				}
				
				managedWorldTime.Total = total;
				managedWorldTime.Delta = delta;
				worldTimeEntity.Set(managedWorldTime.ToStruct());
			}
		}
	}
}