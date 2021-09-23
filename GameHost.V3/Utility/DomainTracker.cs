using System;
using DefaultEcs;
using GameHost.V3.Domains;
using GameHost.V3.Threading;
using GameHost.V3.Threading.V2;

namespace GameHost.V3.Utility
{
    public class DomainTracker<T> : IDisposable
        where T : IDomain
    {
        private bool isDisposed;
        private IDisposable msgOnNewListener;

        public DomainTracker(Action<T> onListener, HostRunnerScope scope, bool automaticSchedule = true)
        {
            var original = onListener;
            onListener = listener =>
            {
                if (automaticSchedule && listener is IScheduler scheduler)
                    scheduler.Add(v => original(v), listener);
                else
                    original(listener);
            };

            msgOnNewListener = scope.World.SubscribeComponentAdded((in Entity entity, in IListener value) =>
            {
                if (!(value is T asT))
                    return;

                scope.Scheduler.Add(v =>
                {
                    if (isDisposed)
                        return;

                    onListener(v);
                }, asT, default);
            });

            foreach (var listener in scope.World.GetAll<IListener>())
            {
                if (!(listener is T asT))
                {
                    continue;
                }

                onListener(asT);
            }
        }

        public void Dispose()
        {
            isDisposed = true;

            msgOnNewListener.Dispose();
        }
    }
    
    public static class DomainTracker
    {
        public static IDisposable TrackDomain<T>(this HostRunnerScope scope, Action<T> onListener, bool automaticSchedule = true)
            where T : IDomain
        {
            return new DomainTracker<T>(onListener, scope, automaticSchedule);
        }
    }
}