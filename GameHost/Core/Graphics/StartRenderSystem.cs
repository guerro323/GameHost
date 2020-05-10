using System;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Injection;
using OpenToolkit.Graphics.ES11;
using OpenToolkit.Windowing.Common;

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
        }

        public EndRenderSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref graphicTool);
        }
    }
}
