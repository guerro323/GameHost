namespace GameHost.Simulation.TabEcs
{
	public readonly struct GameEntity
	{
		public readonly uint Id;

		public GameEntity(uint id)
		{
			Id = id;
		}
	}
}