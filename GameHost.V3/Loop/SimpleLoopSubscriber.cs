using System;
using Collections.Pooled;
using DefaultEcs;
using GameHost.V3.Ecs;
using GameHost.V3.Utility;

namespace GameHost.V3.Loop
{
    public abstract class SimpleLoopSubscriber<TDelegate> : IDisposable
        where TDelegate : Delegate
    {
        protected Entity NonGenericEntity;
        protected OrderGroup OrderGroup;

        public SimpleLoopSubscriber(World world)
        {
            OrderGroup = new OrderGroup(world);
            NonGenericEntity = OrderGroup.World.CreateEntity();
        }

        private readonly PooledList<TDelegate> _callbacks = new();

        protected abstract void OnInvoked(Span<TDelegate> delegates);

        public void Invoke()
        {
            if (OrderGroup.Build())
            {
                _callbacks.ClearReference();
                foreach (var ent in OrderGroup.Entities)
                {
                    _callbacks.Add(ent.Get<TDelegate>());
                }
            }

            OnInvoked(_callbacks.Span);
        }

        public void Dispose()
        {
            NonGenericEntity.Dispose();
            OrderGroup.Dispose();
            
            _callbacks.Dispose();
        }
    }
}