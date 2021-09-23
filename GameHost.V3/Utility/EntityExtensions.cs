using DefaultEcs;

namespace GameHost.V3.Utility
{
    public static class EntityExtensions
    {
        public static bool TryGet<T>(this Entity entity, out T component)
        {
            if (entity.Has<T>())
            {
                component = entity.Get<T>();
                return true;
            }

            component = default;
            return false;
        }
    }
}