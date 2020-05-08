using OpenToolkit.Windowing.Common;

namespace GameHost.Core.Graphics
{
    using System.Numerics;
    
    public interface IGraphicTool
    {
        void Clear(Vector4? vectorColor);
        void SwapBuffers();
    }
}

namespace GameHost.Core.Graphics
{
    using OpenToolkit.Graphics.OpenGL4;
    using OpenToolkit.Mathematics;
    
    public class OpenGl4GraphicTool : IGraphicTool
    {
        private readonly IGameWindow window;
        
        private Color4 color = new Color4(0, 0, 0, 0);

        public OpenGl4GraphicTool(IGameWindow window)
        {
            this.window = window;
        }

        public void Clear(System.Numerics.Vector4? vectorColor)
        {
            if (vectorColor is { } vec)
                this.color = new Color4(vec.X, vec.Y, vec.Z, vec.W);
            
            GL.ClearColor(this.color);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void SwapBuffers()
        {
            window.SwapBuffers();
        }
    }
}
