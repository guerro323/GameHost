using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
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

        private List<Type> systemTypes = new List<Type>();
        private List<Type> queuedSystemTypes = new List<Type>();

        public MainThreadHost(Context context)
        {
            worldCollection = new WorldCollection(context, new World());
        }

        public WorldCollection WorldCollection => worldCollection;

        // that's quite ugly, if only we could use OnThreadStart instead...
        public override void ListenOnThread(Thread wantedThread)
        {
            base.ListenOnThread(wantedThread);
            worldCollection.Ctx.Bind<AssemblyLoadContext, ModuleAssemblyLoadContext>(new ModuleAssemblyLoadContext());
            worldCollection.Ctx.Bind<IScheduler, Scheduler>(GetScheduler());

            wantedThread.Name = "MainThread";
            
            AppSystemResolver.ResolveFor<MainThreadHost>(queuedSystemTypes);
        }

        protected override void OnThreadStart()
        {
            throw new InvalidOperationException();
        }

        public void Update()
        {
            if (GetThread() == null)
                throw new NullReferenceException("MainThreadHost was disposed.");
                
            GetScheduler().Run();
            
            using (SynchronizeThread())
            {
                foreach (var system in queuedSystemTypes)
                {
                    worldCollection.GetOrCreate(system);
                }
                systemTypes.AddRange(queuedSystemTypes);
                queuedSystemTypes.Clear();
                
                worldCollection.DoInitializePass();
                worldCollection.DoUpdatePass();
            }
        }
    }

    public class MainThreadClient : ThreadingClient<MainThreadHost>
    {
    }
}
