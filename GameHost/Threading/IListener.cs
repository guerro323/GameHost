using System;

namespace GameHost.Threading
{
	public delegate void OnApplicationAttached(ListenerCollectionBase updater);

	public delegate ListenerUpdate OnApplicationUpdate();

	public delegate void OnApplicationDetached(ListenerCollectionBase updater);
	
	public interface IListener
	{
		void OnAttachedToUpdater(ListenerCollectionBase updater);
		void OnRemovedFromUpdater(ListenerCollectionBase updater);
		ListenerUpdate OnUpdate(ListenerCollectionBase updater);
	}

	public struct ListenerUpdate
	{
		public TimeSpan TimeToSleep;
	}
}