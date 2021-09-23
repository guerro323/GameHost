using System;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.V3.Injection;
using GameHost.V3.Threading;
using GameHost.V3.Threading.V2;

namespace GameHost.V3.Domains
{
    // straight import from GameHost.V2 CommonApplicationThreadListener
    public abstract class CommonDomainThreadListener : IUpdateableDomain
    {
        [ThreadStatic]
        // ReSharper disable ThreadStaticFieldHasInitializer
        // We disable that warning since it's intended. (this variable will only be set to true on the main thread)
        public static readonly bool IsMainThread = true;
        // ReSharper restore ThreadStaticFieldHasInitializer

        protected readonly TaskCompletionSource _disposalEndTask = new();
        protected readonly TaskCompletionSource _disposalStartTask = new();

        protected readonly Scope DomainScope;
        protected readonly HostRunnerScope HostScope;

        protected CommonDomainThreadListener(Scope scope, Entity domainEntity)
        {
            if (!scope.Context.TryGet(out HostScope))
                throw new InvalidOperationException("expected to have an host scope");

            Scheduler = new ConcurrentScheduler();

            DomainEntity = domainEntity;

            DomainScope = new __DomainScope(scope);
            DomainScope.Context.Register<IScheduler>(Scheduler);
            DomainScope.Context.Register<IDomain>(this);
            DomainScope.Context.Register(TaskScheduler = new ConstrainedTaskScheduler());
        }

        public virtual bool UniqueToOneUpdater => true;
        public ListenerCollectionBase LastUpdater { get; protected set; }
        public ListenerCollectionBase CurrentUpdater { get; protected set; }

        public IRunnableScheduler Scheduler { get; protected set; }
        public TaskScheduler TaskScheduler { get; protected set; }

        public virtual void OnAttachedToUpdater(ListenerCollectionBase updater)
        {
            if (LastUpdater != null && UniqueToOneUpdater)
                throw new Exception();
            LastUpdater = updater;
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

        public bool IsDisposed => _disposalEndTask.Task.IsCompleted;

        public virtual bool QueueDisposal()
        {
            if (IsDisposed || _disposalStartTask.Task.IsCompleted)
                return false;

            _disposalStartTask.SetResult();
            HostScope.Scheduler.Add(task =>
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
            }, _disposalEndTask, true);
            return true;
        }

        public Entity DomainEntity { get; }

        // This is done so SimulationApplication can call the scheduler from here
        protected bool TryExecuteScheduler()
        {
            if (TaskScheduler is ConstrainedTaskScheduler sameThreadTaskScheduler)
            {
                sameThreadTaskScheduler.Execute();
                return true;
            }

            return false;
        }

        protected abstract void DomainUpdate();

        protected virtual ListenerUpdate OnUpdate()
        {
            if (IsDisposed || _disposalStartTask.Task.IsCompleted)
                return default;

            using (CurrentUpdater.SynchronizeThread())
            {
                Scheduler.Run();
                TryExecuteScheduler();

                DomainUpdate();
            }

            return new ListenerUpdate
            {
                TimeToSleep = TimeSpan.FromSeconds(0.01)
            };
        }

        public virtual void Dispose()
        {
            if (!IsMainThread)
                throw new InvalidOperationException("Dispose can only be called by the Main Thread");

            if (Scheduler is IRunnableScheduler runnableScheduler)
                runnableScheduler.Run();

            Scheduler?.Dispose();
        }

        private class __DomainScope : Scope
        {
            public __DomainScope(Scope parent) : base(new ChildScopeContext(parent.Context))
            {
            }
        }
    }
}