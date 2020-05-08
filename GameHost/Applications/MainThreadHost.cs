using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Modding;
using GameHost.Core.Modding.Components;
using GameHost.Core.Threading;
using GameHost.Injection;

namespace GameHost.Applications
{
    // mostly used for knowing the main thread object...
    // but.........
    // what can we do with it?......
    public class MainThreadHost : ThreadingHost<MainThreadHost>
    {
        private Dictionary<string, CModule> loadedModules = new Dictionary<string, CModule>();
        private WorldCollection worldCollection;

        public event Action<CModule> OnModuleAdded;

        public MainThreadHost(Context context)
        {
            worldCollection = new WorldCollection(context, new World());
            worldCollection.Ctx.Bind<AssemblyLoadContext, ModuleAssemblyLoadContext>(new ModuleAssemblyLoadContext());
            
            var systemList = new List<Type>(32);
            AppSystemResolver.ResolveFor<MainThreadHost>(systemList);

            foreach (var system in systemList)
                worldCollection.GetOrCreate(system);
        }

        public WorldCollection WorldCollection => worldCollection;

        protected override void OnThreadStart()
        {
            throw new Exception("Should not be called.");
        }


        public void Update()
        {
            GetScheduler().Run();
            
            using (SynchronizeThread())
            {
                worldCollection.DoInitializePass();
                worldCollection.DoUpdatePass();
            }
        }
    }

    public class MainThreadClient : ThreadingClient<MainThreadHost>
    {
    }
}
