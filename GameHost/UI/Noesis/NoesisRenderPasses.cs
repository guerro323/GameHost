using System;
using GameHost.Core.Ecs;
using GameHost.Core.Graphics;
using GameHost.Entities;
using GameHost.Injection;
using OpenToolkit.Graphics.OpenGL4;
using OpenToolkit.Windowing.Common;

namespace GameHost.UI.Noesis
{
    public abstract class NoesisRenderPassBase : RenderPassBase
    {
        public Span<NoesisOpenTkRenderer> Components => World.Mgr.Get<NoesisOpenTkRenderer>();

        protected NoesisRenderPassBase(WorldCollection worldCollection) : base(worldCollection)
        {
        }
    }
    
    public class NoesisRenderPrePass : NoesisRenderPassBase
    {
        [DependencyStrategy]
        public IManagedWorldTime WorldTime { get; set; }

        public override EPassType Type => EPassType.Pre;

        public override void Execute()
        {
            foreach (var renderer in Components)
            {
                renderer.Update(WorldTime.Total.TotalSeconds);
                renderer.PrepareRender();
            }
        }

        public NoesisRenderPrePass(WorldCollection worldCollection) : base(worldCollection)
        {
        }
    }

    public class NoesisRenderDefaultPass : NoesisRenderPassBase
    {
        [DependencyStrategy]
        public IGameWindow Window { get; set; }

        public override EPassType Type => EPassType.Default;

        public override void Execute()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ClearDepth(1.0f);
            GL.DepthMask(true);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.StencilTest);
            GL.Disable(EnableCap.Blend);
            GL.Disable(EnableCap.ScissorTest);

            GL.UseProgram(0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Window.Size.X, Window.Size.Y);
            GL.ColorMask(true, true, true, true);
        }

        public NoesisRenderDefaultPass(WorldCollection worldCollection) : base(worldCollection)
        {
        }
    }

    public class NoesisRenderPostPass : NoesisRenderPassBase
    {
        [DependencyStrategy]
        public IGameWindow Window { get; set; }
        
        public override EPassType Type => EPassType.Post;

        public override void Execute()
        {
            foreach (var renderer in Components)
            {
                renderer.SetSize(Window.Size.X, Window.Size.Y);
                renderer.Render();
            }
        }

        public NoesisRenderPostPass(WorldCollection worldCollection) : base(worldCollection)
        {
        }
    }
}
