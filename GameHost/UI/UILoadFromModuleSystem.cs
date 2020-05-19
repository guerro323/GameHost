using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modding;
using GameHost.IO;

namespace GameHost.UI
{
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public abstract class UILoadFromModuleSystem<T> : AppSystem
        where T : ILoadableInterface
    {
        public abstract string FileName { get; }

        protected IStorage Storage;

        public UILoadFromModuleSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add<CModule>();
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            foreach (var dep in dependencies)
            {
                if (dep is CModule cmod)
                {
                    Storage = new StorageCollection {cmod.Storage.Value, cmod.DllStorage};
                }
            }
        }
    }
}
