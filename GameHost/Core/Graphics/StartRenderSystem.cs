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
        protected override void OnUpdate()
        {
            base.OnUpdate();
            
            GL.ClearColor(0.0f, 0.2f, 0.0f, 0.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }
    }

    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class EndRenderSystem : AppSystem
    {
        [DependencyStrategy]
        public IGameWindow Window { get; set; }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            Window.SwapBuffers();
        }
    }
}
