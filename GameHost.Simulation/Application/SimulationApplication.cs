using System;
using System.Diagnostics;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Injection;
using GameHost.Simulation.TabEcs;
using GameHost.Threading;
using GameHost.Threading.Apps;
using GameHost.Worlds;
using GameHost.Worlds.Components;
using StormiumTeam.GameBase.Utility.Misc.EntitySystem;

namespace GameHost.Simulation.Application
{
	public class SimulationApplication : CommonApplicationThreadListener
	{
		private TimeApp timeApp;
		private FixedTimeStep fts;

		private TimeSpan targetFrequency;
		
		private ApplicationWorker worker;

		public void SetTargetFrameRate(TimeSpan span)
		{
			Schedule(() => { fts.TargetFrameTimeMs = (int) span.TotalMilliseconds; }, default);
		}

		private ThreadBatchRunner batchRunner;
		private GameWorld         gameWorld;

		public SimulationApplication(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
		{
			// register game world since it's kinda important for the simu app, ahah
			Data.Context.BindExisting(gameWorld = new GameWorld());
			Data.Context.BindExisting<IBatchRunner>(batchRunner = new ThreadBatchRunner(0.5f)); // we only use 50% of the cores

			targetFrequency = TimeSpan.FromSeconds(0.02); // 100 fps
			timeApp = new TimeApp(Data.Context);
			fts     = new FixedTimeStep {TargetFrameTimeMs = (int) targetFrequency.TotalMilliseconds};

			worker = new ApplicationWorker("Simulation");
		}

		private Stopwatch sleepTime = new Stopwatch();
		protected override ListenerUpdate OnUpdate()
		{
			gameWorld.SwitchStructuralThread();
			
			sleepTime.Stop();
		
			var delta       = worker.Delta + sleepTime.Elapsed;
			var updateCount = fts.GetUpdateCount(delta.TotalSeconds);
			updateCount = Math.Min(updateCount, 3);
			
			var elapsed           = worker.Elapsed;

			using (worker.StartMonitoring(targetFrequency))
			{
				timeApp.Update(elapsed, delta);
				using (CurrentUpdater.SynchronizeThread())
				{
					Scheduler.Run();
					TryExecuteScheduler();

					try
					{
						batchRunner.StartPerformanceCriticalSection();
						for (var tickAge = updateCount - 1; tickAge >= 0; --tickAge)
						{
							timeApp.Update(elapsed - TimeSpan.FromSeconds(fts.accumulatedTime) - targetFrequency * tickAge, targetFrequency);
							Data.Loop();
						}
					}
					finally
					{
						batchRunner.StopPerformanceCriticalSection();
					}
				}
			}

			/*if (updateCount > 0)
				Console.WriteLine($"{updateCount} DeltaMs : {worker.Delta.TotalMilliseconds:F3}ms");*/

			var timeToSleep = TimeSpan.FromTicks(Math.Max(targetFrequency.Ticks - worker.Delta.Ticks, 0));

			sleepTime.Restart();
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