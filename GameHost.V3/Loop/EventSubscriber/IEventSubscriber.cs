using System;
using DefaultEcs;
using GameHost.V3.Ecs;

namespace GameHost.V3.Loop.EventSubscriber
{
    public interface IEventSubscriber
    {
        Entity Subscribe(Action<Entity> callback, ProcessOrder process = null);
    }
}