using System;
using DefaultEcs;
using GameHost.Core.Bindables;

namespace GameHost.Core.Ecs
{
    public class ComponentBindable<TValue> : Bindable<TValue>
    {
        public World WorldTarget { get; private set; }

        private IDisposable disposable;

        public ComponentBindable(World world)
        {
            WorldTarget = world;
        }

        public void UseEntitySet(EntitySet set)
        {

        }

        public void BindTo<TComponent>()
        {
            disposable = WorldTarget.SubscribeComponentChanged<TComponent>(OnUpdate);
        }

        public override void Dispose()
        {
            disposable.Dispose();
        }

        private void OnUpdate<TComponent>(in Entity entity, in TComponent oldValue, in TComponent newValue)
        {
        }
    }
}
