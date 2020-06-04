using System;
using System.Diagnostics;
using Noesis;
using EventArgs = Noesis.EventArgs;

namespace GameHost.UI.Noesis
{
    public abstract class LoadableUserControl<TDataContext> : UserControl, ILoadableInterface
        where TDataContext : class
    {
        public TDataContext GenContext => (TDataContext)DataContext;

        protected virtual TDataContext provideDataContext() => Activator.CreateInstance<TDataContext>();

        protected virtual bool EnableFrameUpdate() => false;

        private TimeSpan delta;
        private Stopwatch deltaSw;
        protected TimeSpan Delta
        {
            get
            {
                if (!EnableFrameUpdate())
                    throw new InvalidOperationException("!EnableFrameUpdate");

                return delta;
            }
        }

        public LoadableUserControl()
        {
            Initialized += load;
            Unloaded    += unload;
        }

        private void update()
        {            
            if (deltaSw == null)
            {
                deltaSw = new Stopwatch();
                deltaSw.Start();
            }
            else
            {
                deltaSw.Stop();
                delta = deltaSw.Elapsed;
                deltaSw.Restart();
            }

            try
            {
                OnUpdate();
                Dispatcher.BeginInvoke(update);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void load(object sender, EventArgs args)
        {
            DataContext = provideDataContext();
            OnLoad();

            if (EnableFrameUpdate())
            {
                Dispatcher.BeginInvoke(update);
            }
        }

        private void unload(object sender, RoutedEventArgs args)
        {
            OnUnload();
            Dispose();
            Initialized -= load;
            Unloaded    -= unload;
        }

        public abstract void OnLoad();

        public abstract void OnUnload();

        public abstract void Dispose();
        
        public virtual void OnUpdate() {}

        public T FindName<T>(string name)
        {
            return (T)FindName(name);
        }
    }

    public static class NoesisExtension
    {
        public static T FindName<T>(this FrameworkElement element, string name)
            where T : UIElement
        {
            return (T)element.FindName(name);
        }

        public static T FindName<T>(this FrameworkTemplate element, string name)
            where T : UIElement
        {
            return (T)element.FindName(name);
        }
    }

    public static class GridDef
    {
        public static ColumnDefinition Column(string txt) => new ColumnDefinition {Width = GridLength.Parse(txt)};
        public static RowDefinition    Row(string    txt) => new RowDefinition {Height   = GridLength.Parse(txt)};
    }
}
