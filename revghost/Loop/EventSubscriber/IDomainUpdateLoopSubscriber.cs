using System;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;

namespace revghost.Loop.EventSubscriber;

/// <summary>
/// Main loop of a domain
/// </summary>
public interface IDomainUpdateLoopSubscriber : IEventSubscriber
{
    Entity Subscribe(Action<WorldTime> callback, ProcessOrder process = null);
}