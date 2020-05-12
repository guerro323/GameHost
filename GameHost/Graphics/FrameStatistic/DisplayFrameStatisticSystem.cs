using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.UI.Noesis;
using Noesis;
using OpenToolkit.Windowing.Common;

namespace GameHost.Graphics.FrameStatistic
{
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class DisplayFrameStatisticSystem : AppSystem
    {
        private IGameWindow window;

        public DisplayFrameStatisticSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref window);
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            var gui = new NoesisOpenTkRenderer(window);
            gui.LoadXamlObject(new FrameStatisticControl());

            var view = World.Mgr.CreateEntity();
            view.Set(gui);
        }
    }

    public class FrameStatisticControl : LoadableUserControl<FrameStatisticControl.Context>
    {
        public override void OnLoad()
        {
            Content = new Button {Content = "hello world"};
        }

        public override void OnUnload()
        {
            
        }

        public override void Dispose()
        {
            
        }

        public class Context
        {
            
        }
    }
}
