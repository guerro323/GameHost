using System;
using DefaultEcs;
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

    public struct ResourceHandle<T> : IEquatable<ResourceHandle<T>>
        where T : Resource
    {
        private readonly Entity handleEntity;

        public bool IsLoaded => handleEntity != default && handleEntity.Has<IsResourceLoaded<T>>();

        public ResourceHandle(Entity handleEntity)
        {
            this.handleEntity = handleEntity;
        }

        public T Result => handleEntity.Get<T>();

        public Entity Entity => handleEntity;

        public bool Equals(ResourceHandle<T> other)
        {
            return handleEntity.Equals(other.handleEntity);
        }

        public override bool Equals(object obj)
        {
            return obj is ResourceHandle<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return handleEntity.GetHashCode();
        }

        public static bool operator ==(ResourceHandle<T> left, ResourceHandle<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ResourceHandle<T> left, ResourceHandle<T> right)
        {
            return !left.Equals(right);
        }
    }

    public struct LoadResourceViaStorage
    {
        public IStorage Storage;
        public string   Path;
    }

    public struct LoadResourceViaFile
    {
        public IFile File;
    }

    public struct AskLoadResource<T>
        where T : Resource
    {
    }
}
