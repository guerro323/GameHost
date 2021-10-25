using System;

namespace revghost.Threading.V2;

public delegate void OnApplicationAttached(ListenerCollectionBase updater);

public delegate ListenerUpdate OnApplicationUpdate();

public delegate void OnApplicationDetached(ListenerCollectionBase updater);
	
public interface IListener
{
	void OnAttachedToUpdater(ListenerCollectionBase updater);
	void OnRemovedFromUpdater(ListenerCollectionBase updater);
	ListenerUpdate OnUpdate(ListenerCollectionBase updater);

	bool IsDisposed { get; }
		
	/// <remarks>
	/// A listener cannot be disposed immediately in some situations:
	/// - Threaded listener
	///
	/// A disposal can only be called once, the bootstrap will ultimately call it again when it's being shutdown
	/// </remarks>
	/// <returns>Whether or not this request has already been queued</returns>
	bool QueueDisposal();
}

public struct ListenerUpdate
{
	public TimeSpan TimeToSleep;
}