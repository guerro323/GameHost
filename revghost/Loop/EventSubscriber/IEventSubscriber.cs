using System;
using DefaultEcs;
using revghost.Ecs;

namespace revghost.Loop.EventSubscriber;

public interface IEventSubscriber
{
    Entity Subscribe(Action<Entity> callback, ProcessOrder process = null);
}