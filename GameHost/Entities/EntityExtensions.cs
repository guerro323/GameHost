using DefaultEcs;

namespace GameHost.Entities
{
    public static class EntityExtensions
    {
        public static bool TryGet<T>(this Entity entity, out T val)
        {
            if (!entity.Has<T>())
            {
                val = default;
                return false;
            }

            val = entity.Get<T>();
            return true;
        }
    }
}
