using System;
using System.Threading;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Event;

namespace GameHost.Input
{
    public struct OnInputSynchronizeData : IAppEvent
    {}
    
    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public abstract class InputProviderSystemBase<T> : AppSystem, IReceiveAppEvent<OnInputSynchronizeData>
        where T : IInputProvider
    {
        private InputProviderEndBarrier barrier;
        protected bool DataHasBeenReadThisFrame { get; private set; }

        protected override void OnInit()
        {
            base.OnInit();
        }

        protected EntityRecord GetEntityRecord(Entity entity)
        {
            if (entity.World == World.Mgr)
                throw new InvalidOperationException("Create another EntityCommandRecorder for entity inside of this thread world.");

            return barrier.Recorder.Record(entity);
        }

        protected abstract void OnSync();
        
        void IReceiveAppEvent<OnInputSynchronizeData>.OnEvent(OnInputSynchronizeData t)
        {
            if (World == null)
                return;
            OnSync();
        }
    }

    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public class InputProviderEndBarrier : AppSystem
    {
        public readonly EntityCommandRecorder Recorder;

        private GameInputThreadingHost host;

        protected override void OnInit()
        {
            base.OnInit();
        }
    }
}
