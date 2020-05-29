using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.Command;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class QueuedComponentOperation<T> : ComponentOperationBase<T>
        where T : unmanaged
    {
        private Queue<(Entity entity, T component)> queued;

        protected override void OnUpdate(ref EntityRecord record, in RevolutionEntity revolutionEntity, in T component)
        {
            queued.Enqueue((CurrentEntity, component));
        }

        protected override void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity)
        {
        }

        public override void OnPlayback()
        {
            foreach (ref readonly var queuedComponent in World.World.Get<QueuedComponent<T>>())
            {
                queuedComponent.Clear();
            }

            while (queued.TryDequeue(out var tuple))
            {
                var (entity, component) = tuple;

                QueuedComponent<T> queuedComponent = null;
                if (!entity.Has<QueuedComponent<T>>())
                    entity.Set(queuedComponent = new QueuedComponent<T>());
                else
                    queuedComponent = entity.Get<QueuedComponent<T>>();
                
                queuedComponent.Add(component);
            }
        }
        
    }
    
    public class QueuedComponent<T> : List<T>
    {}
}
