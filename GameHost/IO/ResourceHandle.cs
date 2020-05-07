using DefaultEcs;
using GameHost.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using GameHost.Core.Ecs;

namespace GameHost.IO
{
    public abstract class Resource
    {
        public abstract bool IsCompleted { get; }

        protected abstract T GetMetadata<T>();
    }

    public struct ResourceHandle<T>
        where T : Resource
    {
        public readonly Entity handleEntity;

        public ResourceHandle(Entity entity)
        {
            handleEntity = entity;
            if (!entity.Has<T>())
                throw new Exception($"{entity.ToString()} is not initialized for resource management.");
        }
    }

    public struct TextResourceSchematic : IEntitySchematic
    {
        public void Apply(Entity entity)
        {

        }

        public void Init(WorldCollection collection)
        {

        }
    }
}
