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

        public object Resolve(Type type)
        {
            if (type.GetInterfaces().Contains((typeof(IWorldSystem))))
                return new ResolveSystemStrategy(collection).Resolve(type);

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
    }
}
