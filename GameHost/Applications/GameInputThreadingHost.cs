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
            var updateSw = new Stopwatch();
            var totalSw = new Stopwatch();
            
            totalSw.Start();

            var frequency = TimeSpan.FromSeconds(1f / 500);
            var fts = new FixedTimeStep {TargetFrameTimeMs = frequency.Milliseconds};
            while (!CancellationToken.IsCancellationRequested)
            {
                Frame++;

                var spanDt = updateSw.Elapsed;
                updateSw.Restart();

                // Make sure no one can mess with thread safety here
                var updateCount = fts.GetUpdateCount(spanDt.TotalSeconds);
                updateCount = Math.Min(updateCount, 1); // max one update per frame (it's not a simulation application, so we don't care)

                for (var i = 0; i < updateCount; i++)
                {
                    Frame++;
                    OnBeforeThreadSynchronization();
                    using (SynchronizeThread())
                    {
                        OnThreadSynchronized();
                    }
                }

                if (updateCount > 0)
                    GamePerformance.SetElapsedDelta("input", spanDt);

                var wait = frequency - spanDt;
                if (wait > TimeSpan.Zero)
                    CancellationToken.WaitHandle.WaitOne(wait);
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
