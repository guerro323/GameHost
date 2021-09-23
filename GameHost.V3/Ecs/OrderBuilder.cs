using System;
using DefaultEcs;

namespace GameHost.V3.Ecs
{
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

        public OrderBuilder Position(OrderPosition position)
        {
            Element.Position = position;
            return this;
        }

        public OrderBuilder After(Entity other)
        {
            Group.Calculate(other);

            Element.Index = Math.Max(Element.Index, other.Get<OrderElement>().Index + 1);
            return this;
        }

        public OrderBuilder Before(Entity other)
        {
            Group.Calculate(other);

            Element.Index = Math.Min(Element.Index, other.Get<OrderElement>().Index - 1);
            return this;
        }
    }

    public enum OrderPosition
    {
        AtBeginning,
        AtMiddle,
        AtEnd
    }
}