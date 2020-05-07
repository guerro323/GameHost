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
    public class GameInputThreadingHost : ThreadingHost<GameInputThreadingHost>
    {
        public WorldCollection worldCollection;

        public int Frame { get; private set; }

        private ConcurrentDictionary<string, float> keyPresses = new ConcurrentDictionary<string, float>();

        public GameInputThreadingHost(Context parentCtx)
        {
            worldCollection = new WorldCollection(parentCtx, new World());
            var types = new List<Type>(128);
            AppSystemResolver.ResolveFor<GameInputThreadingHost>(types);
            foreach (var type in types)
                worldCollection.GetOrCreate(type);
        }

        protected override void OnThreadStart()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (!CancellationToken.IsCancellationRequested)
            {
                Frame++;
                stopwatch.Restart();

                OnBeforeThreadSynchronization();

                using (SynchronizeThread())
                {
                    OnThreadSynchronized();
                }
                
                var elapsed = (float)stopwatch.ElapsedTicks / TimeSpan.TicksPerMillisecond;
                GamePerformance.Set("input", stopwatch.Elapsed);
            }
        }

        protected virtual void OnBeforeThreadSynchronization()
        {
        }

        protected virtual void OnThreadSynchronized()
        {
            worldCollection.DoInitializePass();
            worldCollection.DoUpdatePass();
        }
    }

    public class GameInputThreadingClient : ThreadingClient<GameInputThreadingHost>
    {
        public void SetBackend<TBackend>()
            where TBackend : InputBackendBase
        {
            using (SynchronizeThread())
            {
                var backend = (InputBackendBase) Listener.worldCollection.GetOrCreate(typeof(TBackend));
                if (backend == null)
                    throw new Exception("Should not happen");

                Listener.worldCollection.GetOrCreate<InputBackendManager>()
                        .Backend = backend;
            }
        }
    }
}
