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
        [DependencyStrategy]
        public IGraphicTool Gt { get; set; }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Gt.Clear(null);
        }
    }

    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class EndRenderSystem : AppSystem
    {
        [DependencyStrategy]
        public IGraphicTool Gt { get; set; }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Gt.SwapBuffers();
        }
    }
}
