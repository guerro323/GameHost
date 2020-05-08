using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading;
using DefaultEcs;
using DryIoc;
using GameHost.Applications;
using GameHost.Core.IO;
using GameHost.Injection;
using GameHost.IO;
using OpenToolkit.Windowing.Desktop;

namespace GameHost.Core.Game
{
    public abstract class GameBootstrapBase : IDisposable
    {    
        public Context Context { get; }

        private MainThreadHost mainHost;

        public GameBootstrapBase(Context context)
        {
            Context = context;
            
            context.Bind<IStorage, LocalStorage>(new LocalStorage(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/" + GetGameInformation().NameAsFolder));
            Debug.Assert(context.Container.Resolve<IStorage>() != null, "context.Container.Resolve<IStorage>() != null");
            Debug.Assert(context.Container.Resolve<IStorage>() is LocalStorage, "context.Container.Resolve<IStorage>() is LocalStorage");
        
            mainHost = new MainThreadHost(context);
            mainHost.ListenOnThread(Thread.CurrentThread);
        }

        public void Run()
        {
            RunGame();
        }

        protected abstract void RunGame();
        public abstract    bool IsRunning { get; }

        public virtual void Dispose()
        {
            mainHost.Dispose();
        }

        public abstract GameInformation GetGameInformation();
    }

    public struct GameInformation
    {
        public string Name;
        public string NameAsFolder;
    }


    /// <summary>
    /// Window component link
    /// </summary>
    public struct GameExternalWindow
    {
        public string  Name;
        public Vector2 Size;
        public Vector2 Position;
    }
}
