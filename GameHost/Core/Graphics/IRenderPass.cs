using System;
using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Event;
using GameHost.Injection;
using JetBrains.Annotations;

namespace GameHost.Core.Graphics
{
    public enum EPassType
    {
        Pre     = -1,
        Default = 0,
        Post    = 1
    }

    [InjectSystemToWorld]
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public abstract class RenderPassBase : AppObject, IInitSystem, IWorldSystem
    {
        WorldCollection IWorldSystem.WorldCollection => World;

        void IInitSystem.OnInit()
        {
            OnInit();
        }

        public WorldCollection World { get; }

        public abstract EPassType Type { get; }

        protected virtual void OnInit() { }
        public abstract   void Execute();

        protected RenderPassBase(WorldCollection worldCollection) : base(worldCollection.Ctx)
        {
            this.World = worldCollection;
        }
    }

    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    [UpdateAfter(typeof(StartRenderSystem)), UpdateBefore(typeof(EndRenderSystem))]
    public class RenderPassManager : AppSystem, IReceiveAppEvent<OnWorldSystemAdded>
    {
        private Dictionary<EPassType, OrderedList<RenderPassBase>> renderPassByType;

        protected override void OnInit()
        {
            base.OnInit();
            
            renderPassByType = new Dictionary<EPassType, OrderedList<RenderPassBase>>(3);
            foreach (var elem in Enum.GetValues(typeof(EPassType)))
                renderPassByType[(EPassType)elem] = new OrderedList<RenderPassBase>();

            foreach (var system in World.SystemList)
            {
                tryAddSystemAndForget(system);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            void updatePass(EPassType type)
            {
                foreach (var element in renderPassByType[type].Elements)
                    element.Execute();
            }

            updatePass(EPassType.Pre);
            updatePass(EPassType.Default);
            updatePass(EPassType.Post);
        }

        void IReceiveAppEvent<OnWorldSystemAdded>.OnEvent(OnWorldSystemAdded t)
        {
            if (t.System is RenderPassBase tr)
                renderPassByType[tr.Type].Add(tr, OrderedList.GetAfter(tr.GetType()), OrderedList.GetBefore(tr.GetType()));
        }

        private void tryAddSystemAndForget(object system)
        {
            if (system is RenderPassBase tr)
            {
                renderPassByType[tr.Type].Add(tr, OrderedList.GetAfter(tr.GetType()), OrderedList.GetBefore(tr.GetType()));
            }
        }

        public RenderPassManager(WorldCollection collection) : base(collection)
        {
        }
    }
}
