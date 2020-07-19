using DefaultEcs;

namespace GameHost.Core.Ecs
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