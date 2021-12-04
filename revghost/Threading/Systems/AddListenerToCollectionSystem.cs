using System;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.Loop.EventSubscriber;
using revghost.Shared.Collections;
using revghost.Threading.Components;
using revghost.Threading.V2;
using revghost.Utility;

namespace revghost.Threading.Systems;

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
        using var entities = new ValueList<Entity>(_listenerSet.GetEntities());
        
        foreach (var entity in entities)
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