using DefaultEcs;
using GameHost.V3.Threading.Systems;

namespace GameHost.V3.Threading.Components
{
	/// <summary>
	/// Read-only component, attributed from <see cref="AddListenerToCollectionSystem"/>
	/// </summary>
	public struct ListenerCollectionTarget
	{
		public readonly Entity Entity;

		public ListenerCollectionTarget(Entity entity)
		{
			Entity = entity;
		}
	}

	/// <summary>
	/// Component to update an <see cref="ListenerCollectionTarget"/>
	/// </summary>
	public struct PushToListenerCollection
	{
		public readonly Entity Entity;

		public PushToListenerCollection(Entity entity)
		{
			Entity = entity;
		}
	}
}