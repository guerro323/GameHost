using System;

namespace revghost.Ecs;

public class OrderElement
{
    internal bool _stateCalculated;

    public void Clear()
    {
        _stateCalculated = false;
        Index = 0;
        Position = OrderPosition.AtMiddle;
    }

    public int Index { get; set; }

    // position is used for duplicate keys
    public OrderPosition Position { get; set; }
}

public readonly struct OrderElementName : IEquatable<OrderElementName>
{
    public readonly string Value;

    public OrderElementName(string name)
    {
        Value = name;
    }

    public bool Equals(OrderElementName other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object obj)
    {
        return obj is OrderElementName other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (Value != null ? Value.GetHashCode() : 0);
    }
}

public struct OrderElementFinalIndex
{
    public int Value;
}