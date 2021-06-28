namespace GameHost.Utility
{
	public interface ISystemOrder
	{
		void Inject<TParentOrder, TSystem>(TParentOrder parent, TSystem system, OrderGroup<TSystem> groupMap);
	}
}