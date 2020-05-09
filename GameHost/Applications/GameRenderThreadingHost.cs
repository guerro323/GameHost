using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Graphics;
using GameHost.Core.Threading;
using GameHost.Injection;
using OpenToolkit.Windowing.Common;
using OpenToolkit.Windowing.Desktop;
using OpenToolkit.Windowing.GraphicsLibraryFramework;

namespace GameHost.Applications
{
    public class GameRenderThreadingHost : ThreadingHost<GameRenderThreadingHost>
    {
        private readonly IGameWindow window;

        // there should be only one worldCollection when rendering
        private readonly WorldCollection worldCollection;

        private List<Type> systemTypes       = new List<Type>();
        private List<Type> queuedSystemTypes = new List<Type>();

        public GameRenderThreadingHost(IGameWindow window, Context context)
        {
            this.window          = window;
            this.worldCollection = new WorldCollection(context, new World());

            AppSystemResolver.ResolveFor<GameRenderThreadingHost>(queuedSystemTypes);
        }

        protected override void OnThreadStart()
        {
            Noesis.Log.SetLogCallback((level, channel, message) =>
            {
                if (channel == "")
                {
                    // [TRACE] [DEBUG] [INFO] [WARNING] [ERROR]
                    string[] prefixes = new string[] {"T", "D", "I", "W", "E"};
                    string   prefix   = (int)level < prefixes.Length ? prefixes[(int)level] : " ";
                    Console.WriteLine("[NOESIS/" + prefix + "] " + message);
                }
            });

            unsafe
            {
                while (!window.IsVisible) { }
            }

            window.MakeCurrent();
            window.VSync = VSyncMode.On;

            // todo: we shouldn't explicitly do new() here, but parent contexts aren't used normal Resolve<>()... 
            worldCollection.Ctx.Bind<IGraphicTool, OpenGl4GraphicTool>(new OpenGl4GraphicTool(window));
            worldCollection.Ctx.Bind<IScheduler, Scheduler>(GetScheduler());

            while (!CancellationToken.IsCancellationRequested && window.Exists && !window.IsExiting)
            {
                GetScheduler().Run();
                
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

            worldCollection.Mgr.Dispose();
            worldCollection.Ctx.Container.Dispose();
        }

        internal void InjectAssembly(Assembly assembly)
        {
            using (SynchronizeThread())
            {
                AppSystemResolver.ResolveFor<GameRenderThreadingHost>(assembly, queuedSystemTypes);
            }
        }
    }

    public class GameRenderThreadingClient : ThreadingClient<GameRenderThreadingHost>
    {
        public void InjectAssembly(Assembly assembly) => Listener.InjectAssembly(assembly);
    }
}
