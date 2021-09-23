using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Threading.V2;
using GameHost.V3.Utility;

namespace GameHost.V3.Threading.Systems
{
    public class RemoveDisposedListenerCollectionsSystem : AppSystem
    {
        private World _world;
        private IDomainUpdateLoopSubscriber _updateLoop;

        public RemoveDisposedListenerCollectionsSystem(Scope scope) : base(scope)
        {
            Dependencies.AddRef(() => ref _world);
            Dependencies.AddRef(() => ref _updateLoop);
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
}