using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Revolution;
using package.stormiumteam.networking.runtime.lowlevel;

namespace GameHost.HostSerialization
{
    public class HostCreateSnapshotSystem : AppSystem
    {
        private EntitySet toReplicateSet;

        private DataBufferWriter buffer;

        public HostCreateSnapshotSystem(WorldCollection collection) : base(collection)
        {
            toReplicateSet = World.Mgr.GetEntities()
                                  .With<SetAsHostEntity>()
                                  .AsSet();
            buffer = new DataBufferWriter(128);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Serialize();
        }

        private void Serialize()
        {
            buffer.Length = 0;

            var entities = toReplicateSet.GetEntities();
        }

        public override void Dispose()
        {
            buffer.Dispose();
            
            base.Dispose();
        }
    }
}
