using System;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;

namespace GameHost.Injection
{
    public class GetSystemFromTargetWorldStrategy : IDependencyStrategy
    {
        private readonly Func<WorldCollection> getWorldFunc;

        public GetSystemFromTargetWorldStrategy(Func<WorldCollection> getWorld)
        {
            this.getWorldFunc = getWorld;
        }

        public object Resolve(Type type)
        {
            var world = getWorldFunc();
            return world != null
                ? new ResolveSystemStrategy(world).Resolve(type)
                : null;
        }
    }

    public class GetSystemFromTargetWorldStrategy<TApplication> : GetSystemFromTargetWorldStrategy
        where TApplication : GameThreadedHostApplicationBase<TApplication>
    {
        public GetSystemFromTargetWorldStrategy() : base(() =>
        {
            if (ThreadingHost.TypeToThread.TryGetValue(typeof(TApplication), out var threadHost))
            {
                var application = (TApplication)threadHost.Host;
                return null; // ?
            }

            return null;
        })
        {
        }
    }
}
