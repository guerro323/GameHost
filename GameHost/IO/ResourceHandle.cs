using DefaultEcs;
using GameHost.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using DefaultEcs.Resource;
using GameHost.Core.Ecs;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public abstract class Resource
    {
        protected Resource()
        {
        }
    }

    public struct IsResourceLoaded<T>
        where T : Resource
    {
    }

    public struct ResourceHandle<T>
        where T : Resource
    {
        private readonly Entity handleEntity;

        public bool IsLoaded => handleEntity.Has<IsResourceLoaded<T>>();

        public ResourceHandle(Entity handleEntity)
        {
            this.handleEntity = handleEntity;
        }

        public T Result => handleEntity.Get<T>();
    }

    public struct LoadResourceViaFile
    {
        public IStorage Storage;
        public string   Path;
    }

    public struct AskLoadResource<T>
        where T : Resource
    {
    }
}
