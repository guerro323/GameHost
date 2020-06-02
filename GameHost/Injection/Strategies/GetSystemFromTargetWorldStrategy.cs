using System;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;

namespace GameHost.Injection
{
    public class DependencyFunctionStrategy : IDependencyStrategy
    {
        private readonly Func<object> getObjectFunc;

        public DependencyFunctionStrategy(Func<object> getObject)
        {
            this.getObjectFunc = getObject;
        }

        public object ResolveNow(Type type)
        {
            var result = getObjectFunc();
            if (result.GetType() == type)
                return result;
            return null;
        }

        public Func<object> GetResolver(Type type)
        {
            return () => ResolveNow(type);
        }
    }

    public class GetSystemFromTargetWorldStrategy : IDependencyStrategy
    {
        private readonly Func<WorldCollection> getWorldFunc;

        public GetSystemFromTargetWorldStrategy(Func<WorldCollection> getWorld)
        {
            this.getWorldFunc = getWorld;
        }

        public object ResolveNow(Type type)
        {
            var world = getWorldFunc();
            return world != null
                ? new ResolveSystemStrategy(world).ResolveNow(type)
                : null;
        }

        public Func<object> GetResolver(Type type)
        {
            return () => ResolveNow(type);
        }
    }

    public class ThreadSystemWithInstanceStrategy<TApplication> : IDependencyStrategy
        where TApplication : ApplicationHostBase, IApplicationGetWorldFromInstance
    {
        private readonly Context context;

        private Instance targetInstance;

        public ThreadSystemWithInstanceStrategy(Context context)
        {
            this.context = context;
        }

        public ThreadSystemWithInstanceStrategy(Instance givenInstance)
        {
            this.targetInstance = givenInstance;
        }

        public object ResolveNow(Type type)
        {
            targetInstance ??= new ContextBindingStrategy(context, true).Resolve<Instance>();

            if (ThreadingHost.TryGetListener(out TApplication application))
            {
                using var threadLocker = ThreadingHost.Synchronize<TApplication>();

                if (application.TryGetWorldFromInstance(targetInstance, out var worldCollection))
                {
                    return worldCollection.GetOrCreate(type);
                }
            }

            return null;
        }

        public Func<object> GetResolver(Type type)
        {
            return () => ResolveNow(type);
        }
    }

    public class ThreadWorldWithInstanceStrategy<TApplication> : IDependencyStrategy
        where TApplication : ApplicationHostBase, IApplicationGetWorldFromInstance
    {
        private readonly Context context;

        private Instance targetInstance;

        public ThreadWorldWithInstanceStrategy(Context context)
        {
            this.context = context;
        }

        public ThreadWorldWithInstanceStrategy(Instance givenInstance)
        {
            this.targetInstance = givenInstance;
        }

        public object ResolveNow(Type type)
        {
            targetInstance ??= new ContextBindingStrategy(context, true).Resolve<Instance>();

            if (ThreadingHost.TryGetListener(out TApplication application))
            {
                using var threadLocker = ThreadingHost.Synchronize<TApplication>();

                if (application.TryGetWorldFromInstance(targetInstance, out var worldCollection))
                {
                    return worldCollection;
                }
            }

            return null;
        }
        
        public Func<object> GetResolver(Type type)
        {
            return () => GetResolver(type);
        }
    }
}
