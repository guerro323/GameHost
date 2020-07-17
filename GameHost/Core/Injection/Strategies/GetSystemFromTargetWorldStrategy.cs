using System;
using GameHost.Core.Ecs;

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
}
