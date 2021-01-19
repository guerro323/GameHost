using System;

namespace GameHost.Simulation.TabEcs.Interfaces
{
	public interface IEntityComponent
	{
	}

	public interface IComponentData : IEntityComponent
	{
	}

	public interface IComponentBuffer : IEntityComponent
	{

	}

	public interface IMetadataSubComponentOf
	{
		ComponentType ProvideComponentParent(GameWorld gameWorld);
	}

	public interface IMetadataCustomComponentName
	{
		string ProvideName(GameWorld gameWorld);
	}

	public interface IMetadataCustomComponentBoard
	{
		ComponentBoardBase ProvideComponentBoard(GameWorld gameWorld);
	}
}