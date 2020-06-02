using System;
using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Threading;
using GameHost.UI.Noesis;
using Noesis;
using OpenToolkit.Mathematics;

namespace GameHost.Graphics.FrameStatistic
{
    public class FrameStatistic : LoadableUserControl<FrameStatistic.Context>
    {
        public class Context
        {
            public string RenderName  { get; set; }
            public ApplicationWorker   Worker { get; set; }
        }

        public class FrameListener : IFrameListener
        {
            private SimpleFrameListener super  = new SimpleFrameListener();
            private List<WorkerFrame>   frames = new List<WorkerFrame>();

            public bool Add(WorkerFrame frame)
            {
                return super.Add(frame);
            }

            public int    LastCollectionIndex;
            public double Delta;
            public double SD;
            public double Workload;

            public TimeSpan TargetFramerate = TimeSpan.FromSeconds(1f / 1000);
			
            public List<WorkerFrame> DequeueAll()
            {
                frames.Clear();
                super.DequeueAll(frames);

                foreach (var frame in frames)
                {
                    if (frame.CollectionIndex != LastCollectionIndex)
                    {
                        Delta               = 0;
                        Workload            = 0;
                        LastCollectionIndex = frame.CollectionIndex;
                    }

                    Delta    = Math.Max(frame.Delta.TotalSeconds, Delta);
                    Workload = Math.Max(frame.Delta.TotalSeconds / TargetFramerate.TotalSeconds, Workload);
                }

                SD = MathHelper.Lerp((float) SD, (float) Delta, (float) Delta * 25);

                return frames;
            }
        }
        
        protected override Context provideDataContext()
        {
            SetBinding(ContentProperty, ".");
            if (Content is Context ctx)
                return ctx;
            return new Context();
        }

        private FrameListener listener;
        private Label fpsLabel;
        private Label loadLabel;

        private void ac()
        {
            if (!IsEnabled)
                return;

            listener.DequeueAll();

            fpsLabel.Content = (int)(1 / listener.Delta);
            loadLabel.Content = listener.Workload.ToString("000%");
            
            Dispatcher.BeginInvoke(ac);
        }
        
        public override void OnLoad()
        {
            if (!GenContext.Worker.FrameListener.TryAdd(listener = new FrameListener()))
            {
                throw new InvalidOperationException();
            }

            listener.TargetFramerate = GenContext.Worker.TargetFrameRate;
            
            fpsLabel = new Label {Content = "100FPS"};
            loadLabel = new Label {Content = "100%"};
            
            fpsLabel.FontFamily = new FontFamily("Courier New");
            loadLabel.FontFamily = new FontFamily("Courier New");
            
            var appLabel         = new Label
            {
                Content = "App1",
                Margin = new Thickness(5, 0, 0, 0)
            };
            
            Dispatcher.BeginInvoke(ac);

            appLabel.SetBinding(ContentProperty, "RenderName");
            fpsLabel.SetBinding(ContentProperty, "Worker.Performance");

            var border = new Border
            {
                Background = new SolidColorBrush(new Color {ScB = 1, ScA = 0.25f}),
                CornerRadius = new CornerRadius(6)
            };
            border.Child = appLabel;

            Content = new Grid 
            {
                ColumnDefinitions = {GridDef.Column("100*"), GridDef.Column("3*"), GridDef.Column("10*"), GridDef.Column("15*")},
                Children =
                {
                    border, 
                    loadLabel,
                    fpsLabel,
                },
                Margin = new Thickness(0, 1, 0, 1)
            };

            Grid.SetColumn(appLabel, 0);
            Grid.SetColumn(loadLabel, 2);
            Grid.SetColumn(fpsLabel, 3);
        }

        public override void OnUnload()
        {
                
        }

        public override void Dispose()
        {
                
        }
    }
}
