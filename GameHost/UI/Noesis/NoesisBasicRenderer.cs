using System;
using Noesis;
using NoesisApp;

namespace GameHost.UI.Noesis
{
    public class NoesisBasicRenderer : IDisposable
    {
        static NoesisBasicRenderer()
        {
            GUI.Init("", "");

            Application.SetThemeProviders();
            GUI.LoadApplicationResources("Theme/NoesisTheme.DarkBlue.xaml");

            GUI.SetXamlProvider(new LocalXamlProvider(Environment.CurrentDirectory));
            GUI.SetTextureProvider(new LocalTextureProvider(Environment.CurrentDirectory));
            GUI.SetFontProvider(new EmbeddedFontProvider(null, null));
        }

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
