using System;
using DefaultEcs;

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
}