#nullable enable
using System;
using System.Collections.Generic;
using Collections.Pooled;
using DefaultEcs;
using revghost.Injection.Dependencies;
using revghost.Shared;
using revghost.Utility;

namespace revghost.Ecs;

public class OrderGroup : IDisposable
{
    public readonly bool IsCustomWorld;
    public readonly World World;

    public Span<Entity> Entities => _entities.Span;

    // the goal was to originally use an EntitySet, but it use a bit more memory, and entities may not be in order of creation (because of pooling)
    private PooledList<Entity> _entities = new();

    private IDisposable _entityDisposedMessage;

    private bool _isDirty;

    public OrderGroup(World? customWorld = null)
    {
        IsCustomWorld = customWorld != null;
        World = customWorld ?? new World();

        _entityDisposedMessage = World.SubscribeEntityDisposed((in Entity entity) =>
        {
            if (_entities.Remove(entity))
                _isDirty = true;
        });
    }

    public Entity Add(ProcessOrder? processOrder)
    {
        var entity = World.CreateEntity();
        entity.Set(processOrder ?? (_ => { }));
        entity.Set(new OrderElement());

        _entities.Add(entity);
        _isDirty = true;

        return entity;
    }

    private HashSet<Entity> _isMarked = new();
    private HashSet<Entity> _circularPrevention = new();

    public bool Build(bool forceBuild = false)
    {
        if (!forceBuild && !_isDirty)
        {
            return false;
        }

        _isDirty = false;
        _isMarked.Clear();

        foreach (var entity in _entities)
            entity.Get<OrderElement>().Clear();

        foreach (var entity in _entities)
            Calculate(entity);

        using (DisposableArray<Entity>.Rent(_entities.Count, out var sortedArray))
        {
            var span = sortedArray.AsSpan(0, _entities.Count);
            var crawl = 0;
            
            _circularPrevention.Clear();
            foreach (var entity in _entities)
            {
                if (!Crawl(entity, ref crawl, span))
                {
                    return false;
                }
            }

            if (_entities.Count != crawl)
            {
                HostLogger.Output.Error("Unmatched crawling, perhaps there was a circular dependency?", "OrderGroup");
                foreach (var entity in _entities)
                {
                    if (_isMarked.Contains(entity)) continue;
                    
                    HostLogger.Output.Error($"{entity} was not sorted");
                }

                return false;
            }
            
            for (var i = 0; i < span.Length; i++)
            {
                _entities[i] = span[i];
            }
        }

        return true;
    }

    public void Dispose()
    {
        if (!IsCustomWorld)
        {
            HostLogger.Output.Info("Disposed OrderGroup non-custom world");
            World.Dispose();
        }

        _entityDisposedMessage.Dispose();
    }

    private void Calculate(Entity entity)
    {
        var builder = new OrderBuilder(entity, entity.Get<OrderElement>(), this);
        entity.Get<ProcessOrder>()(builder);
    }
    
    // very simple algorithm:
    // 1. Marked entities are skipped
    // 2. We check if the entity was already being crawled, and if it does it's a circular dep error
    // 3. Crawl into dependencies, and early return if there was a circular dependency.
    // 4. Mark the entity and add it to the sort span
    private bool Crawl(Entity entity, ref int crawl, Span<Entity> sort)
    {
        var element = entity.Get<OrderElement>();
        if (_isMarked.Contains(entity))
            return true;
        
        if (_circularPrevention.Contains(entity))
        {
            HostLogger.Output.Error($"{entity} was in a circular dependency");
            return false;
        }
        
        _circularPrevention.Add(entity);
        
        foreach (var dep in element.Dependencies)
        {
            if (!Crawl(dep, ref crawl, sort))
            {
                HostLogger.Output.Error($"Couldn't crawl {entity}", "OrderGroup");
                return false;
            }
        }

        _isMarked.Add(entity);
        _circularPrevention.Remove(entity);
        
        sort[crawl++] = entity;
        return true;
    }
}