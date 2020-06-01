using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Entities;
using RevolutionSnapshot.Core;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class CloneableComponentOperation<T> : ComponentOperationBase<T>
        where T : ICloneable<T>
    {
        protected override void OnUpdate(ref EntityRecord record, in RevolutionEntity revolutionEntity, in T component)
        {
            record.Set(component.Clone());
        }

        protected override void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity)
        {
            record.Remove<T>();
        }
    }

    public class CopyableComponentOperation<T> : ComponentOperationBase<T>
        where T : ICopyable<T>, new()
    {
        private ConcurrentQueue<Entity>    queued  = new ConcurrentQueue<Entity>();
        private Dictionary<Entity, Copied> copyMap = new Dictionary<Entity, Copied>();

        private class Copied
        {
            public T Value = new T();
        }

        protected override void OnUpdate(ref EntityRecord record, in RevolutionEntity revolutionEntity, in T component)
        {
            if (!copyMap.TryGetValue(CurrentEntity, out Copied copied))
                copyMap[CurrentEntity] = copied = new Copied();

            component.CopyTo(ref copied.Value);
            queued.Enqueue(CurrentEntity);
        }

        protected override void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity)
        {
            copyMap.Remove(CurrentEntity);
            record.Remove<T>();
        }

        public override void OnPlayback()
        {
            while (queued.TryDequeue(out var entity))
            {
                if (!copyMap.TryGetValue(entity, out var copy))
                    continue;

                if (!entity.Has<T>())
                    entity.Set(new T());
                entity.Get<T>() = copy.Value;
            }
        }
    }

    public interface ICopyable<T>
    {
        void CopyTo(ref T other);
    }
}
