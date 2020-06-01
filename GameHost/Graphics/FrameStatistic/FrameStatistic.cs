using System;
using GameHost.Applications;
using GameHost.UI.Noesis;
using Noesis;

namespace GameHost.Graphics.FrameStatistic
{
    public class FrameStatistic : LoadableUserControl<FrameStatistic.Context>
    {
        public class Context
        {
            public string RenderName  { get; set; }
            public ApplicationWorker   Worker { get; set; }
        }

        protected override Context provideDataContext()
        {
            SetBinding(ContentProperty, ".");
            if (Content is Context ctx)
                return ctx;
            return new Context();
        }

        public override void OnLoad()
        {
            var performanceLabel = new Label {Content = "100%"};
            var appLabel         = new Label {Content = "App1"};
            appLabel.Background = new SolidColorBrush(Colors.Blue);

            Content = new Grid 
            {
                ColumnDefinitions = {GridDef.Column("100*"), GridDef.Column("3*"), GridDef.Column("10*")},
                Children =
                {
                    appLabel, 
                    performanceLabel
                },
                Margin = new Thickness(0, 1, 0, 1)
            };

            Grid.SetColumn(appLabel, 0);
            Grid.SetColumn(performanceLabel, 2);
        }

        public override void OnUnload()
        {
                
        }

        public override void Dispose()
        {
                
        }
    }
}
