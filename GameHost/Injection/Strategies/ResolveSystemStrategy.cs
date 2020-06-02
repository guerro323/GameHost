using System;
using GameHost.Core.Ecs;

namespace GameHost.Injection
{
    public struct ResolveSystemStrategy : IDependencyStrategy
    {
        private readonly WorldCollection collection;

        public ResolveSystemStrategy(WorldCollection targetWorldCollection)
        {
            this.collection = targetWorldCollection;
        }

        public object ResolveNow(Type type)
        {
            // todo: make it more 'functional'
            if (collection.TryGet(type, out var obj))
            {
                // todo: we should have an interface called IHasDependencies for getting the DependencyResolver field.
                if (obj is AppObject ao && ao.DependencyResolver.Dependencies.Count > 0)
                {
                    return null;
                }

                return obj;
            }

            // todo: maybe we should have an option to create the system if it does not exist?
            return null;
        }

        public Func<object> GetResolver(Type type)
        {
            var cc = collection;
            return () => new ResolveSystemStrategy(cc).ResolveNow(type);
        }
    }
}
