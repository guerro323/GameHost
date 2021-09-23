using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;

namespace GameHost.V3.Loop.EventSubscriber
{
    public interface IDomainUpdateLoopSubscriber : IEventSubscriber
    {
        Entity Subscribe(Action<WorldTime> callback, ProcessOrder process = null);
    }
}