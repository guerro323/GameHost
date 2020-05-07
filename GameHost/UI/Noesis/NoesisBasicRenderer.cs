using System;
using System.Threading;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using Noesis;
using NoesisApp;
using OpenToolkit.Windowing.GraphicsLibraryFramework;

namespace GameHost.UI.Noesis
{
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class NoesisSystem : AppSystem
    {
        protected override void OnInit()
        {
            base.OnInit();
            GUI.Init("", "");

            Application.SetThemeProviders();
            GUI.LoadApplicationResources("Theme/NoesisTheme.DarkBlue.xaml");

            GUI.SetXamlProvider(new LocalXamlProvider(Environment.CurrentDirectory));
            GUI.SetTextureProvider(new LocalTextureProvider(Environment.CurrentDirectory));
            GUI.SetFontProvider(new EmbeddedFontProvider(null, null));
        }
    }

    public class NoesisBasicRenderer : IDisposable
    {
        public View     View     { get; private set; }
        public Renderer Renderer { get; private set; }

        public virtual void Dispose()
        {
            Renderer.Shutdown();
        }

        public void ParseXaml(string xaml)
        {
            LoadXamlObject((FrameworkElement)GUI.ParseXaml(xaml));
        }

        public void LoadXamlObject(FrameworkElement xamlObject)
        {
            View = GUI.CreateView(xamlObject);
            View.SetFlags(RenderFlags.PPAA | RenderFlags.LCD);

            Console.WriteLine("Loaded Object on " + Thread.CurrentThread.Name);

            Renderer = View.Renderer;
            Renderer.Init(new RenderDeviceGL());
        }

        public void SetSize(int width, int height)
        {
            View.SetSize(width, height);
        }

        public virtual void Update(double time)
        {
            View.Update(time);
        }

        public void PrepareRender()
        {
            Renderer.UpdateRenderTree();
            Renderer.RenderOffscreen();
        }

        public void Render()
        {
            Renderer.Render();
        }
    }
}
