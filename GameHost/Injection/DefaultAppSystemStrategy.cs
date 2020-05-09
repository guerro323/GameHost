using System;
using System.Reflection;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Modding;
using GameHost.Core.Modding.Systems;

namespace GameHost.Injection
{
    public class DefaultAppSystemStrategy : IDependencyStrategy
    {
        private readonly WorldCollection collection;
        private readonly object source;

        public DefaultAppSystemStrategy(object source, WorldCollection collection)
        {
            this.source     = source;
            this.collection = collection;
        }

        public object Resolve(Type type)
        {
            if (type.IsSubclassOf(typeof(IWorldSystem)))
            {
                // todo: check correct application
                return collection.GetOrCreate(type);
            }

            if (type == typeof(Assembly))
                return source.GetType().Assembly;

            if (type.IsSubclassOf(typeof(CModule)))
                return collection.GetOrCreate<ModuleManager>().GetModule(source.GetType().Assembly);

            if (type.IsSubclassOf(typeof(ApplicationClientBase)))
            {
                var instance = (ApplicationClientBase) Activator.CreateInstance(type);
                instance.Connect();
                if (source is AppSystem app)
                    app.AddDisposable(instance);
                return instance;
            }

            return new ContextBindingStrategy(collection.Ctx, true).Resolve(type);
        }
    }
}
