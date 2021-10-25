#nullable enable
using System;
using Collections.Pooled;
using DefaultEcs;
using revghost.Shared;
using revghost.Utility;

namespace revghost.Ecs;

public class OrderGroup : IDisposable
{
    public readonly bool IsCustomWorld;
    public readonly World World;

    public Span<Entity> Entities => _entities.Span;

    private EntityMap<OrderElementName> _elementNameMap;

    // the goal was to originally use an EntitySet, but it use a bit more memory, and entities may not be in order of creation (because of pooling)
    private PooledList<Entity> _entities = new();

    private IDisposable _entityDisposedMessage;

    private bool _isDirty;

    public OrderGroup(World customWorld = null)
    {
        IsCustomWorld = customWorld != null;
        World = customWorld ?? new World();

        _entityDisposedMessage = World.SubscribeEntityDisposed((in Entity entity) =>
        {
            if (_entities.Remove(entity))
                _isDirty = true;
        });

        // If it's a custom world, then we add predicates for OrderGroup (since there could be multiple ones
        if (IsCustomWorld)
        {
            _elementNameMap = World.GetEntities()
                .With((in OrderGroup c) => c == this)
                .With<OrderElement>()
                .AsMap<OrderElementName>();
        }
        else
        {
            _elementNameMap = World.GetEntities()
                .With<OrderElement>()
                .AsMap<OrderElementName>();
        }
    }

    public Entity Add(ProcessOrder? processOrder)
    {
        var entity = World.CreateEntity();
        entity.Set(processOrder ?? (_ => { }));
        entity.Set(new OrderElement());
        entity.Set(new OrderElementFinalIndex());

        _entities.Add(entity);
        _isDirty = true;

        return entity;
    }

    public bool Build(bool forceBuild = false)
    {
        if (!forceBuild && !_isDirty)
        {
            return false;
        }

        _isDirty = false;

        foreach (var entity in _entities)
            entity.Get<OrderElement>().Clear();

        foreach (var entity in _entities)
            Calculate(entity);

        using (DisposableArray<FinalOrder>.Rent(_entities.Count, out var finalArray))
        {
            var span = finalArray.AsSpan(0, _entities.Count);
            for (var i = 0; i < _entities.Count; i++)
            {
                var element = _entities[i].Get<OrderElement>();
                span[i] = new FinalOrder(element.Index, element.Position, _entities[i]);
            }

            span.Sort();
                
            for (var i = 0; i < span.Length; i++)
            {
                span[i].Entity.Get<OrderElementFinalIndex>().Value = i;
                _entities[i] = span[i].Entity;
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (IsCustomWorld)
            World.Dispose();

        _elementNameMap.Dispose();

        _entityDisposedMessage.Dispose();
    }

    public void Calculate(Entity entity)
    {
        if (entity.Get<OrderElement>()._stateCalculated)
            return;

        var builder = new OrderBuilder(entity, entity.Get<OrderElement>(), this);
        entity.Get<ProcessOrder>()(builder);
    }

    private struct FinalOrder : IComparable<FinalOrder>
    {
        public int Index;
        public OrderPosition Position;
        public Entity Entity;

        public FinalOrder(int index, OrderPosition position, Entity entity)
        {
            Index = index;
            Position = position;
            Entity = entity;
        }

        public int CompareTo(FinalOrder other)
        {
            var indexComparison = Index.CompareTo(other.Index);
            if (indexComparison != 0) return indexComparison;
            return ((int) Position).CompareTo((int) other.Position);
        }
    }
}