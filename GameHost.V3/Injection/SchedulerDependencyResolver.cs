using System;
using Collections.Pooled;
using GameHost.V3.Threading;

namespace GameHost.V3.Injection
{
    public class SchedulerDependencyResolver : IDependencyResolver, IDisposable
    {
        private static readonly Func<SchedulerDependencyResolver, bool> updateMethod = resolver =>
        {
            // Early stop if we've disposed
            if (resolver._isDisposed)
                return true;

            var isFinished = true;
            for (var i = 0; i < resolver._collections.Count; i++)
            {
                var collection = resolver._collections[i];
                bool wantToContinue;
                do
                {
                    collection.TryResolve(out wantToContinue);
                } while (wantToContinue);

                if (collection.Dependencies.IsEmpty)
                    // swapback
                    resolver._collections.RemoveAt(i--);
                else
                    isFinished = false;
            }

            return isFinished;
        };

        private readonly PooledList<IDependencyCollection> _collections = new();
        private readonly IScheduler _scheduler;

        private bool _isDisposed;

        public SchedulerDependencyResolver(IScheduler scheduler)
        {
            _scheduler = scheduler;
        }

        public void Queue(IDependencyCollection collection)
        {
            _collections.Add(collection);
            _scheduler.Add(updateMethod, this, SchedulingParametersWithArgs.AsOnceWithArgs);
        }

        public void Dequeue(IDependencyCollection collection)
        {
            _collections.Remove(collection);
        }

        public void Dispose()
        {
            _collections?.Dispose();

            _isDisposed = true;
        }
    }
}