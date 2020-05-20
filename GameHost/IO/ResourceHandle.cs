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

        public Entity Entity => handleEntity;
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
