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

        protected override void OnInit()
        {
            client = new GameInputThreadingClient();
        }
    }
    
    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public abstract class InputBackendBase : AppSystem
    {
        protected InputBackendManager backendMgr;

        protected override void OnInit()
        {
            base.OnInit();
            backendMgr = World.GetOrCreate<InputBackendManager>();
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
    }
    
    public class ReceiveFromInputThreadSystem : AppSystem
    {
        private GameInputThreadingClient client;

        protected override void OnInit()
        {
            client = new GameInputThreadingClient();
        }

        protected override void OnUpdate()
        {
            using (client.SynchronizeThread())
            {
                client.Listener.worldCollection.Ctx.SignalApp(new OnInputSynchronizeData());
            }
        }
    }
}
