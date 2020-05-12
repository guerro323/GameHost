using Noesis;

namespace GameHost.UI.Noesis
{
    public abstract class LoadableUserControl : UserControl, ILoadableInterface
    {
        public LoadableUserControl()
        {
            Initialized += load;
            Unloaded    += unload;
        }

        private void load(object sender, EventArgs args)
        {
            OnLoad();
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

        public T FindName<T>(string name)
        {
            return (T) FindName(name);
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
}
