using System;
using System.Threading;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Input.Default;

namespace GameHost.Input
{
    public struct OnInputSynchronizeData : IAppEvent
    {
    }
    
    public abstract class InputProviderSystemBase<TSelf, TAction> : AppSystem
        where TAction : IInputAction
        where TSelf : InputProviderSystemBase<TSelf, TAction>
    {
        private InputBackendManager              inputBackendMgr;
        private TSelf selfOnInputThread;

        private readonly bool     isInputThread;
        
        // ReSharper disable StaticMemberInGenericType
        public object InputSynchronizationBarrier
        {
            get
            {
                if (selfOnInputThread != null)
                    return selfOnInputThread.Synchronization;
                return Synchronization;
            }
        }
        // ReSharper restore StaticMemberInGenericType

        protected InputBackendBase Backend => inputBackendMgr.Backend;

        protected EntitySet InputSet { get; private set; }

        private EntitySet inputThreadEntitySet;
        private EntitySet receiveThreadEntitySet;

        protected InputProviderSystemBase(WorldCollection collection) : base(collection)
        {
            inputThreadEntitySet = World.Mgr.GetEntities()
                                        .With<InputActionLayouts>()
                                        .With<TAction>()
                                        .With<ThreadInputToCompute>()
                                        .AsSet();
            receiveThreadEntitySet = World.Mgr.GetEntities()
                                          .With<InputActionLayouts>()
                                          .With<TAction>()
                                          .With<InputThreadTarget>()
                                          .AsSet();

            // not elegant to know on which application we are...
            isInputThread = ThreadingHost.TypeToThread.TryGetValue(typeof(GameInputThreadingHost), out var threadHost)
                            && threadHost.Thread == Thread.CurrentThread;

            if (isInputThread)
            {
                DependencyResolver.Add(() => ref inputBackendMgr);
            }
            else
            {
                DependencyResolver.Add(() => ref selfOnInputThread, new ThreadSystemWithInstanceStrategy<GameInputThreadingHost>(Context));   
            }
        }

        public override bool CanUpdate()
        {
            if (isInputThread && inputBackendMgr?.Backend == null)
                return false;
            return base.CanUpdate();
        }

        protected override void OnUpdate()
        {
            if (isInputThread)
            {
                InputSet = inputThreadEntitySet;
                OnInputThreadUpdate();
            }

            InputSet = receiveThreadEntitySet;
            OnReceiverUpdate();
            InputSet = null;
        }

        protected abstract void OnInputThreadUpdate();
        protected abstract void OnReceiverUpdate();
    }
}
