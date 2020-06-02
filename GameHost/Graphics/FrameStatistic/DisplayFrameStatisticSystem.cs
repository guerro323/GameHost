using System;
using System.Collections.Generic;
using System.Linq;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
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
        private void addContext<T>(ref List<FrameStatistic.Context> contexts)
            where T : GameThreadedHostApplicationBase<T>
        {
            if (ThreadingHost.TryGetListener(out T host))
            {
                using var synchronizer = host.SynchronizeThread();
                contexts.Add(new FrameStatistic.Context {RenderName = typeof(T).Name, Worker = host.Worker});
            }
        }

        public override void OnLoad()
        {
            Grid parentGrid;
            
            var contexts = new List<FrameStatistic.Context>();
            addContext<GameSimulationThreadingHost>(ref contexts);
            addContext<GameInputThreadingHost>(ref contexts);
            addContext<GameRenderThreadingHost>(ref contexts);
            addContext<GameAudioThreadingHost>(ref contexts);

            var root = new Grid
            {
                ColumnDefinitions = {GridDef.Column("100*"), GridDef.Column("50*"), GridDef.Column("2*")},
                RowDefinitions    = {GridDef.Row("100*"), GridDef.Row("2*")},
                Children =
                {
                    (parentGrid = new Grid
                    {
                        Children =
                        {
                            new Viewbox
                            {
                                VerticalAlignment   = VerticalAlignment.Bottom,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Child = new ItemsControl
                                {
                                    ItemsSource = contexts,
                                    ItemTemplate        = new DataTemplate {VisualTree = new FrameStatistic()},
                                    Width               = 500,
                                    VerticalAlignment   = VerticalAlignment.Top,
                                    HorizontalAlignment = HorizontalAlignment.Right
                                }
                            }
                        }
                    })
                }
            };

            Grid.SetColumn(parentGrid, 1);
            Grid.SetRow(parentGrid, 0);

            Content = root;
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
