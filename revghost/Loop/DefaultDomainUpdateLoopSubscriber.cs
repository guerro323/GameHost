using System;
using Collections.Pooled;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;
using revghost.Loop.EventSubscriber;
using revghost.Utility;

namespace revghost.Loop;

public class DefaultDomainUpdateLoopSubscriber : IDomainUpdateLoopSubscriber, IDisposable
{
    private readonly OrderGroup _orderGroup;

    private Entity _callbackEntity;

    public DefaultDomainUpdateLoopSubscriber(World world)
    {
        _orderGroup = new OrderGroup();
        _callbackEntity = world.CreateEntity();
    }

    public void Dispose()
    {
        _orderGroup.Dispose();
        _callbackEntity.Dispose();
    }

    public Entity Subscribe(Action<Entity> callback, ProcessOrder process)
    {
        return Subscribe((WorldTime _) => { callback(_callbackEntity); }, process);
    }

    public Entity Subscribe(Action<WorldTime> callback, ProcessOrder process)
    {
        var entity = _orderGroup.Add(process);
        entity.Set(callback);

        return entity;
    }

    private readonly PooledList<Action<WorldTime>> _callbacks = new(ClearMode.Always);

    public void Invoke(TimeSpan total, TimeSpan delta)
    {
        if (_orderGroup.Build())
        {
            // We do that because .Clear() is bugged (it doesn't zero elements)
            _callbacks.ClearReference();
            foreach (var ent in _orderGroup.Entities)
            {
                _callbacks.Add(ent.Get<Action<WorldTime>>());
            }
        }

        var worldTime = new WorldTime {Total = total, Delta = delta};
        foreach (var action in _callbacks.Span)
            action(worldTime);
    }
}