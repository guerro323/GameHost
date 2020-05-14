using System;
using System.Reflection;
using DryIoc;
using GameHost.Core.Graphics;
using GameHost.Core.Threading;
using GameHost.Injection;
using OpenToolkit.Windowing.Common;

namespace GameHost.Applications
{
    public class GameRenderThreadingHost : GameThreadedHostApplicationBase<GameRenderThreadingHost>
    {
        private readonly IGameWindow window;

        protected override void OnInit()
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

            while (!window.IsVisible) { }

            window.MakeCurrent();
            window.VSync = VSyncMode.On;

            AddInstance(Context.Container.Resolve<Instance>());
        }

        protected override void OnQuit()
        {
        }

        public GameRenderThreadingHost(IGameWindow window, Context context) : base(context)
        {
            this.window = window;
        }

        protected override void OnUpdate(ref int fixedUpdateCount, TimeSpan elapsedTime)
        {
            if (!window.Exists || window.IsExiting)
            {
                QuitApplication = true;
                return;
            }

            base.OnUpdate(ref fixedUpdateCount, elapsedTime);
        }

        protected override void OnInstanceAdded<TInstance>(in TInstance instance)
        {
            base.OnInstanceAdded(in instance);
            var worldCollection = MappedWorldCollection[instance];
            worldCollection.Ctx.Bind<IGraphicTool, OpenGl4GraphicTool>(new OpenGl4GraphicTool(window));
        }
    }

    public class GameRenderThreadingClient : ThreadingClient<GameRenderThreadingHost>
    {
        public void InjectAssembly(Assembly assembly) => Listener.InjectAssembly(assembly);
    }
}
