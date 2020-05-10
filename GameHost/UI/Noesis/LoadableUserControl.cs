using Noesis;

namespace GameHost.UI.Noesis
{
    public abstract class LoadableUserControl : UserControl, ILoadableInterface
    {
        public abstract void OnLoad();

        public abstract void OnUnload();

        public abstract void Dispose();
    }
}
