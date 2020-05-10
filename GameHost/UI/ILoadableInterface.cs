using System;

namespace GameHost.UI
{
    public interface ILoadableInterface : IDisposable
    {
        void OnLoad();
        void OnUnload();
    }
}
