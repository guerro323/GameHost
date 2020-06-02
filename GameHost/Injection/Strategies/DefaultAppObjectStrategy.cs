using System;
using System.Reflection;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Modding;
using GameHost.Core.Modding.Systems;
using Microsoft.Extensions.Logging;
using NetFabric.Hyperlinq;

namespace GameHost.Injection
{
    public class DefaultAppObjectStrategy : IDependencyStrategy
    {
        private readonly WorldCollection collection;
        private readonly object          source;

        public DefaultAppObjectStrategy(object source, WorldCollection collection)
        {
            this.source     = source;
            this.collection = collection;
        }

        private object resolving;

        public object ResolveNow(Type type)
        {
            if (resolving != null)
            {
                if (resolving is AppObject appObject
                    && appObject.DependencyResolver.Dependencies.Count == 0)
                {
                    return resolving;
                }

                return null;
            }

            if (type.GetInterfaces().Contains((typeof(IWorldSystem))))
                return new ResolveSystemStrategy(collection).ResolveNow(type);

            if (typeof(AppObject).IsAssignableFrom(type)
                && !typeof(AppSystem).IsAssignableFrom(type))
            {
                resolving = Activator.CreateInstance(type, new object[] {collection.Ctx});
                return null;
            }

            if (type == typeof(Assembly))
                return source.GetType().Assembly;

            if (typeof(CModule).IsAssignableFrom(type))
                return collection.GetOrCreate(world => new ModuleManager(world)).GetModule(source.GetType().Assembly).Result;

            if (typeof(ILogger).IsAssignableFrom(type))
            {
                var factory = new ContextBindingStrategy(collection.Ctx, true).Resolve<ILoggerFactory>();
                if (factory != null)
                {
                    return factory.CreateLogger(source.GetType().FullName);
                }
            }

            if (typeof(ApplicationClientBase).IsAssignableFrom(type))
            {
                var instance = (ApplicationClientBase)Activator.CreateInstance(type);
                instance.Connect();
                if (source is AppSystem app)
                    app.AddDisposable(instance);
                return instance;
            }

            return new ContextBindingStrategy(collection.Ctx, true).Resolve(type);
        }

        public Func<object> GetResolver(Type type)
        {
            var clone = new DefaultAppObjectStrategy(source, collection);
            return () => clone.ResolveNow(type);
        }
    }
}
