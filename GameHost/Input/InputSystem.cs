using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using DefaultEcs;
using DryIoc;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Entities;

namespace GameHost.Input
{
    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public class InputSystem : AppSystem
    {
        private GameInputThreadingClient client;

        public InputSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref client);
        }
    }

    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public abstract class InputBackendBase : AppSystem
    {
        protected InputBackendManager backendMgr;

        protected InputBackendBase(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref backendMgr);
        }

        public override bool CanUpdate() => base.CanUpdate() && backendMgr.Backend == this;

        protected internal abstract void OnDisable();
        protected internal abstract void OnEnable();
    }

    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public class InputBackendManager : AppSystem
    {
        private InputBackendBase backend;

        public InputBackendBase Backend
        {
            get => backend;
            set
            {
                backend?.OnDisable();
                backend = value;
                backend?.OnEnable();
            }
        }

        public InputBackendManager(WorldCollection collection) : base(collection)
        {
        }
    }

    public class ReceiveFromInputThreadSystem : AppSystem
    {
        private GameInputThreadingClient client;

        public ReceiveFromInputThreadSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref client);
        }
        
        protected override void OnUpdate()
        {
            using (client.SynchronizeThread())
            {
                client.Listener.WorldCollection.Ctx.SignalApp(new OnInputSynchronizeData());
            }
        }
    }
}
