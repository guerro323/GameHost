using System;
using GameHost.Applications;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Worlds;

namespace GameHost.Threading.Apps
{
	public class CommonApplicationThreadListener : IListener, IApplication, IScheduler
	{
		public virtual bool                   UniqueToOneUpdater => true;
		public         ListenerCollectionBase LastUpdater        { get; protected set; }
		public         ListenerCollectionBase CurrentUpdater     { get; protected set; }

		public IScheduler Scheduler { get; protected set; }

		public CommonApplicationThreadListener(GlobalWorld source, Context overrideContext)
		{
			Scheduler = new Scheduler();
			Global    = source;
			Data      = new ApplicationData(overrideContext ?? new Context(source.Context));
			Data.Context.BindExisting<IScheduler>(Scheduler);
		}

		public virtual void OnAttachedToUpdater(ListenerCollectionBase updater)
		{
			if (LastUpdater != null && UniqueToOneUpdater)
				throw new Exception();
			LastUpdater    = updater;
			CurrentUpdater = updater;
		}

		public virtual void OnRemovedFromUpdater(ListenerCollectionBase updater)
		{
			if (updater == CurrentUpdater)
				CurrentUpdater = null;

			if (updater != LastUpdater && UniqueToOneUpdater)
				throw new Exception();

			LastUpdater = null;
		}
		
		

		ListenerUpdate IListener.OnUpdate(ListenerCollectionBase updater)
		{
			CurrentUpdater = updater;
			return OnUpdate();
		}

		protected virtual ListenerUpdate OnUpdate()
		{
			using (CurrentUpdater.SynchronizeThread())
			{
				Scheduler.Run();
				Data.Loop();
			}

			return new ListenerUpdate
			{
				TimeToSleep = TimeSpan.FromSeconds(0.01)
			};
		}

		public void Run()
		{
			Scheduler.Run();
		}

		public void Schedule(Action action, in SchedulingParameters parameters)
		{
			Scheduler.Schedule(action, parameters);
		}

		public void Schedule<T>(Action<T> action, T args, in SchedulingParametersWithArgs parameters)
		{
			Scheduler.Schedule(action, args, parameters);
		}

		public GlobalWorld Global { get; }
		public ApplicationData Data { get; }
	}
}