using System.Threading;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.Graphics
{
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class StartRenderSystem : AppSystem
    {
        private IGraphicTool graphicTool;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            graphicTool.Clear(null);
        }

        public StartRenderSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref graphicTool);
        }
    }

    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class EndRenderSystem : AppSystem
    {
        private IGraphicTool graphicTool;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            graphicTool.SwapBuffers();
            Thread.Sleep(1); // report correct CPU usage
        }

        public EndRenderSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref graphicTool);
        }
    }
}
