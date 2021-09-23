using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Threading.Components;
using GameHost.V3.Threading.V2;
using GameHost.V3.Utility;

namespace GameHost.V3.Threading.Systems
{
    public class AddListenerToCollectionSystem : AppSystem
    {
        private World _world;
        private IDomainUpdateLoopSubscriber _updateLoop;

        public AddListenerToCollectionSystem(Scope scope) : base(scope)
        {
            Dependencies.AddRef(() => ref _world);
            Dependencies.AddRef(() => ref _updateLoop);
        }

        private EntitySet _listenerSet;

        protected override void OnInit()
        {
            Disposables.AddRange(new IDisposable[]
            {
                _listenerSet = _world.GetEntities()
                    .With<IListener>()
                    .With((in PushToListenerCollection value) => value.Entity.IsAlive)
                    .AsSet(),

                _updateLoop.Subscribe(OnUpdate)
            });
        }

        private void OnUpdate(WorldTime worldTime)
        {
            foreach (var entity in _listenerSet.GetEntities())
            {
                var listener   = entity.Get<IListener>();
                var collection = entity.Get<PushToListenerCollection>().Entity.Get<ListenerCollectionBase>();

                if (entity.Has<ListenerCollectionTarget>())
                {
                    var currentEntity = entity.Get<ListenerCollectionTarget>().Entity;
                    if (currentEntity.IsAlive)
                    {
                        var previous = currentEntity.Get<ListenerCollectionBase>();
                        previous.RemoveListener(listener);
                    }
                    // in case there is an exception in the next line, be sure that this brace will not get re-invoked.
                    entity.Remove<ListenerCollectionTarget>();
                }

                var keys = Array.Empty<IListenerKey>();
                if (entity.Has<IListenerKey[]>())
                    keys = entity.Get<IListenerKey[]>();

                collection.AddListener(listener, keys);
                entity.Set(new ListenerCollectionTarget(entity.Get<PushToListenerCollection>().Entity));
                entity.Remove<PushToListenerCollection>();
            }
        }
    }
}