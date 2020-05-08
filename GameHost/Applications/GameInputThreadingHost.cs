using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Game;
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
            AddInstance(Instance.CreateInstance<Instance>("InputApplication", Context));
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
            using (SynchronizeThread())
            {
                var backend = (InputBackendBase) Listener.WorldCollection.GetOrCreate(typeof(TBackend));
                if (backend == null)
                    throw new Exception("Should not happen");

                Listener.WorldCollection.GetOrCreate<InputBackendManager>()
                        .Backend = backend;
            }
        }
    }
}
