using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private ItemsControl graphControl;
        private ObservableCollection<GGPerformance> performances;

        protected override bool EnableFrameUpdate()
        {
            return true;
        }

        public override void OnUpdate()
        {
            var frames = listener.DequeueAll();

            const bool debugAllFrames = false;
            if (debugAllFrames)
            {
                foreach (var frame in frames)
                {
                    performances.Add(new GGPerformance {Performance = (float)(frame.Delta.TotalSeconds / listener.TargetFramerate.TotalSeconds)});
                }
            }
            else if (frames.Count > 0)
            {
                performances.Add(new GGPerformance {Performance = (float) listener.Workload});
            }

            while (performances.Count > 100)
                performances.RemoveAt(0);

            fpsLabel.Content  = (int)(1 / listener.Delta);
            loadLabel.Content = listener.Workload.ToString("000%");
        }

        public override void OnLoad()
        {
            PPAAMode = PPAAMode.Disabled;
            
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
                Margin = new Thickness(5, 0, 0, 0),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 1,
                    Color = Colors.Black,
                    ShadowDepth = 2
                }
            };

            appLabel.SetBinding(ContentProperty, "RenderName");
            fpsLabel.SetBinding(ContentProperty, "Worker.Performance");

            var border = new Border
            {
                Background = new SolidColorBrush(new Color {ScB = 1, ScA = 0.25f}),
                CornerRadius = new CornerRadius(6)
            };
            border.Child = appLabel;

            performances = new ObservableCollection<GGPerformance>();
            for (var i = 0; i != 100; i++)
                performances.Add(new GGPerformance {Performance = i * 0.02f});

            var graphControlTemplateRectangle = new Rectangle
            {
                Width = 3,
                Fill  = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            graphControl = new ItemsControl
            {
                ItemsSource = performances,
                ItemsPanel = new ItemsPanelTemplate
                {
                  VisualTree  = new StackPanel
                  {
                      Orientation = Orientation.Horizontal,
                      HorizontalAlignment = HorizontalAlignment.Right,
                      VerticalAlignment = VerticalAlignment.Bottom
                  }
                },
                HorizontalContentAlignment = HorizontalAlignment.Right,
                VerticalContentAlignment   = VerticalAlignment.Center,
                ItemTemplate = new DataTemplate
                {
                    VisualTree = new Grid
                    {
                        Children =
                        {
                            graphControlTemplateRectangle
                        },
                        MaxHeight = 10
                    }
                },
                HorizontalAlignment = HorizontalAlignment.Right
            };

            graphControlTemplateRectangle.SetBinding(HeightProperty, "ScaledPerformance");
            
            Content = new Grid 
            {
                ColumnDefinitions = {GridDef.Column("100*"), GridDef.Column("3*"), GridDef.Column("10*"), GridDef.Column("15*")},
                Children =
                {
                    graphControl,
                    border, 
                    loadLabel,
                    fpsLabel,
                },
                Margin = new Thickness(0, 1, 0, 1)
            };

            Grid.SetColumn(appLabel, 0);
            Grid.SetColumn(loadLabel, 2);
            Grid.SetColumn(fpsLabel, 3);

            Height = 40;
        }

        public override void OnUnload()
        {
                
        }

        public override void Dispose()
        {
                
        }

        public class GGPerformance : NotifyPropertyChangedBase
        {
            public float ScaledPerformance
            {
                get => Math.Clamp(Performance, 0, 100);
            }

            private float performance;
            public float Performance
            {
                get => performance;
                set
                {
                    performance = value;
                    OnPropertyChanged(nameof(ScaledPerformance));
                    OnPropertyChanged(nameof(Performance));
                }
            }
        }
    }
}
