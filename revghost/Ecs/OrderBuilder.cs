using System;
using DefaultEcs;
using revghost.Utility;

namespace revghost.Ecs;

public delegate void ProcessOrder(OrderBuilder builder);

public struct OrderBuilder
{
    public readonly OrderGroup Group;
    public readonly Entity View;
    public readonly OrderElement Element;

    public OrderBuilder(Entity view, OrderElement element, OrderGroup group)
    {
        View = view;
        Element = element;
        Group = group;
    }

    public OrderBuilder After(Entity other)
    {
        Element.Dependencies.Add(other);
        return this;
    }

    public OrderBuilder Before(Entity other)
    {
        other.Get<OrderElement>().Dependencies.Add(View);
        return this;
    }

    public OrderBuilder After(Type type)
    {
        if (Group._entityTypeMultiMap.TryGetEntities(type, out var span))
        {
            foreach (var entity in span)
                After(entity);
        }
        else
        {
            HostLogger.Output.Warn($"No entities found with type '{type}'", nameof(OrderBuilder), "order-after");
        }

        return this;
    }

    public OrderBuilder Before(Type type)
    {
        if (Group._entityTypeMultiMap.TryGetEntities(type, out var span))
        {
            foreach (var entity in span)
                Before(entity);
        }
        else
        {
            HostLogger.Output.Warn($"No entities found with type '{type}'", nameof(OrderBuilder), "order-before");
        }

        return this;
    }
}