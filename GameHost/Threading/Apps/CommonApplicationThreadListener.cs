using System;
using System.Threading;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Utility;
using GameHost.Worlds;

namespace GameHost.Threading.Apps
{
	public class CommonApplicationThreadListener : IListener, IApplication, IScheduler
	{
		public virtual bool                   UniqueToOneUpdater => true;
		public         ListenerCollectionBase LastUpdater        { get; protected set; }
		public         ListenerCollectionBase CurrentUpdater     { get; protected set; }

		public IScheduler    Scheduler     { get; protected set; }
		public TaskScheduler TaskScheduler { get; protected set; }

		protected TaskCompletionSource disposalStartTask = new();
		protected TaskCompletionSource disposalEndTask   = new();
		
		public CommonApplicationThreadListener(GlobalWorld source, Context overrideContext)
		{
			Scheduler = new Scheduler();
			Global    = source;
			Data      = new ApplicationData(overrideContext ?? new Context(source.Context));
			Data.Context.BindExisting<IScheduler>(Scheduler);
			Data.Context.BindExisting<IApplication>(this);
			Data.Context.BindExisting<TaskScheduler>(TaskScheduler = new SameThreadTaskScheduler());
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

		public bool IsDisposed => disposalEndTask.Task.IsCompleted;

		public virtual bool QueueDisposal()
		{
			if (IsDisposed || disposalStartTask.Task.IsCompleted)
				return false;

			disposalStartTask.SetResult();
			Global.Scheduler.Schedule(task =>
			{
				if (task.Task.IsCompleted)
					return;

				try
				{
					Dispose();
				}
				finally
				{
					task.SetResult();
				}
			}, disposalEndTask, SchedulingParametersWithArgs.AsOnceWithArgs);
			return true;
		}

		// This is done so SimulationApplication can call the scheduler from here
		protected bool TryExecuteScheduler()
		{
			if (TaskScheduler is SameThreadTaskScheduler sameThreadTaskScheduler)
			{
				sameThreadTaskScheduler.Execute();
				return true;
			}

			return false;
		}

		protected virtual ListenerUpdate OnUpdate()
		{
			if (IsDisposed || disposalStartTask.Task.IsCompleted)
				return default;
			
			using (CurrentUpdater.SynchronizeThread())
			{
				Scheduler.Run();
				TryExecuteScheduler();

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

		public void Add<T>(T task) where T : ISchedulerTask
		{
			Scheduler.Add(task);
		}

		public void Schedule(Action action, in SchedulingParameters parameters)
		{
			Scheduler.Schedule(action, parameters);
		}

		public void Schedule<T>(Action<T> action, T args, in SchedulingParametersWithArgs parameters)
		{
			Scheduler.Schedule(action, args, parameters);
		}

		public Entity          AssignedEntity { get; set; }
		public GlobalWorld     Global         { get; }
		public ApplicationData Data           { get; }

		public virtual void Dispose()
		{
			if (!IsMainThread)
				throw new InvalidOperationException("Dispose can only be called by the Main Thread");
			
			Scheduler?.Run();
			Scheduler?.Dispose();
			
			Data?.Context?.Dispose();
			Data?.Collection?.Dispose();
		}
		
		[ThreadStatic]
		// ReSharper disable ThreadStaticFieldHasInitializer
		// We disable that warning since it's intended.
		public static readonly bool IsMainThread = true;
		// ReSharper restore ThreadStaticFieldHasInitializer
	}
}