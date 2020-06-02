using System;
using System.Reflection;
using DryIoc;
using GameHost.Core.Graphics;
using GameHost.Core.Threading;
using GameHost.Injection;
using Microsoft.Extensions.Logging;
using OpenToolkit.Windowing.Common;
using ZLogger;

namespace GameHost.Applications
{
    public class GameRenderThreadingHost : GameThreadedHostApplicationBase<GameRenderThreadingHost>
    {
        private readonly IGameWindow window;
        private readonly ILogger logger;
        private readonly ILogger noesisLogger;

        protected override void OnInit()
        {
            Noesis.Log.SetLogCallback((level, channel, message) =>
            {
                if (channel == "")
                {
                    // [TRACE] [DEBUG] [INFO] [WARNING] [ERROR]
                    string[] prefixes = new string[] {"T", "D", "I", "W", "E"};
                    string   prefix   = (int)level < prefixes.Length ? prefixes[(int)level] : " ";
                    noesisLogger.ZLog((LogLevel)level, message);
                }
            });

            while (!window.IsVisible) { }

            window.MakeCurrent();
            window.VSync = VSyncMode.Off;

            AddInstance(Context.Container.Resolve<Instance>());
        }

        protected override void OnQuit()
        {
        }

        public GameRenderThreadingHost(IGameWindow window, Context context, TimeSpan frequency) : base(context, frequency)
        {
            this.window = window;

            var factory = context.Container.Resolve<ILoggerFactory>();
            this.logger = factory.CreateLogger("GameRenderApp");
            this.noesisLogger = factory.CreateLogger("Noesis");
        }

        protected override void OnUpdate(ref int fixedUpdateCount, TimeSpan elapsedTime)
        {
            if (!window.Exists || window.IsExiting)
            {
                QuitApplication = true;
                return;
            }

            fixedUpdateCount = Math.Min(fixedUpdateCount, 1);
            base.OnUpdate(ref fixedUpdateCount, elapsedTime);
        }

        protected override void OnFixedUpdate(int step, TimeSpan delta, TimeSpan elapsedTime)
        {
            // replaced delta 'frequency' by PreviousWorkDelta
            base.OnFixedUpdate(step, PreviousWorkDelta, elapsedTime);
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
