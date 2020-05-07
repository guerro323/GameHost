using System;
using System.Collections.Generic;
using System.Reflection;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using OpenToolkit.Windowing.Common;

namespace GameHost.Applications
{
    public class GameRenderThreadingHost : ThreadingHost<GameRenderThreadingHost>
    {
        private readonly INativeWindow window;
        
        // there should be only one worldCollection when rendering
        private readonly WorldCollection worldCollection;

        private List<Type> systemTypes = new List<Type>();
        private List<Type> queuedSystemTypes = new List<Type>();
        
        public GameRenderThreadingHost(INativeWindow window, Context context)
        {
            this.window = window;
            this.worldCollection = new WorldCollection(context, new World());
            
            AppSystemResolver.ResolveFor<GameRenderThreadingHost>(queuedSystemTypes);
        }
        
        protected override void OnThreadStart()
        {
            window.MakeCurrent();
            while (true)
            {
                if (queuedSystemTypes.Count > 0)
                {
                    // When creating new systems, we need to be sure the thread is safe
                    using (SynchronizeThread())
                    {
                        foreach (var type in queuedSystemTypes)
                            worldCollection.GetOrCreate(type);
                    }

                    systemTypes.AddRange(queuedSystemTypes);
                    queuedSystemTypes.Clear();
                }

                using (SynchronizeThread())
                {
                    worldCollection.DoInitializePass();
                    worldCollection.DoUpdatePass();
                }
            }
        }
    }

    public class GameRenderThreadingClient : ThreadingClient<GameRenderThreadingHost>
    {
        public void InjectAssembly(Assembly assembly)
        {
            
        }
    }
}
