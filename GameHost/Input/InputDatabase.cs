using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Input.Default;

namespace GameHost.Input
{
    public class InputDatabase : AppSystem
    {
        private struct QueuedInput
        {
            public InputActionLayouts Layouts;
            public Entity             Source;

            public Action<Entity> SetComponents;
        }

        [RestrictToApplication(typeof(GameInputThreadingHost))]
        private class RestrictedHost : AppSystem
        {
            public ConcurrentQueue<QueuedInput> queued;

            public RestrictedHost(WorldCollection collection) : base(collection)
            {
                queued = new ConcurrentQueue<QueuedInput>();
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
                while (queued.TryDequeue(out var queuedInput))
                {
                    var clientEntity = World.Mgr.CreateEntity();
                    clientEntity.Set(queuedInput.Layouts);
                    clientEntity.Set(new ThreadInputToCompute {Source = queuedInput.Source});

                    queuedInput.SetComponents(clientEntity);
                    Console.WriteLine(clientEntity + ", yes.");
                }
            }
        }

        private RestrictedHost restrictedHost;
        private IScheduler     scheduler;

        public InputDatabase(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref restrictedHost, new ThreadSystemWithInstanceStrategy<GameInputThreadingHost>(Context));
            DependencyResolver.Add(() => ref scheduler);
        }

        public void Register(JsonDocument document)
        {
            Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");
        }

        public Entity RegisterSingle<TAction>(params InputLayoutBase[] layouts)
            where TAction : IInputAction, new()
        {
            Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");

            var ac = World.Mgr.CreateEntity();
            ac.Set(new InputActionLayouts(layouts));
            ac.Set(default(TAction));

            restrictedHost.queued.Enqueue(new QueuedInput
            {
                Layouts = new InputActionLayouts(ac.Get<InputActionLayouts>()),
                Source  = ac,
                SetComponents = e =>
                {
                    e.Set(new TAction());
                    scheduler.Add(() => ac.Set(new InputThreadTarget {Target = e}));
                }
            });

            return ac;
        }
    }
}
