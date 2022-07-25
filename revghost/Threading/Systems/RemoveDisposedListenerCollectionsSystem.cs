using System;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.Loop.EventSubscriber;
using revghost.Threading.V2;
using revghost.Utility;

namespace revghost.Threading.Systems;

public class RemoveDisposedListenerCollectionsSystem : AppSystem
{
    private World _world;
    private IDomainUpdateLoopSubscriber _updateLoop;

    public RemoveDisposedListenerCollectionsSystem(Scope scope) : base(scope)
    {
        Dependencies.Add(() => ref _world);
        Dependencies.Add(() => ref _updateLoop);
    }

    private EntitySet _collectionSet;

    protected override void OnInit()
    {
        Disposables.AddRange(new IDisposable[]
        {
            _collectionSet = _world.GetEntities()
                .With((in ListenerCollectionBase c) => c.IsDisposed)
                .AsSet(),

            _updateLoop.Subscribe(OnUpdate)
        });
    }

    private void OnUpdate(WorldTime obj) => _collectionSet.DisposeAllEntities();
}