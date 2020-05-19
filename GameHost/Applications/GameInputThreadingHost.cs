using System;
using System.Reflection;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Input;

namespace GameHost.Applications
{
    // This should be completely redone to accept other backends other than openTk
    public class GameInputThreadingHost : GameThreadedHostApplicationBase<GameInputThreadingHost>
    {
        public WorldCollection WorldCollection { get; private set; }

        protected override void OnInit()
        {
            AddInstance(new ContextBindingStrategy(Context, true).Resolve<Instance>());
        }

        protected override void OnQuit()
        {
            
        }

        public GameInputThreadingHost(Context context) : base(context, TimeSpan.FromSeconds(1f / 1000))
        {
        }

        protected override void OnInstanceAdded<TInstance>(in TInstance instance)
        {
            base.OnInstanceAdded(in instance);
            WorldCollection = MappedWorldCollection[instance];
        }
    }

    public class GameInputThreadingClient : ThreadingClient<GameInputThreadingHost>
    {
        public void SetBackend<TBackend>()
            where TBackend : InputBackendBase
        {
            Listener.GetScheduler().Add(() =>
            {
                var backend = (InputBackendBase) Listener.WorldCollection.GetOrCreate(typeof(TBackend));
                if (backend == null)
                    throw new Exception("Should not happen");

                Listener.WorldCollection.GetOrCreate(world => new InputBackendManager(world))
                        .Backend = backend;   
            });
        }
        
        public void InjectAssembly(Assembly assembly) => Listener.InjectAssembly(assembly);
    }
}
