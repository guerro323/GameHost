using System;
using GameHost.Simulation.TabEcs;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Simulation.Features.ShareWorldState
{
	public interface IShareComponentSerializer
	{
		bool CanSerialize(GameWorld world, Span<GameEntityHandle>           entities, ComponentBoardBase board);
		void SerializeBoard(ref DataBufferWriter buffer, GameWorld world,   Span<GameEntityHandle>       entities, ComponentBoardBase board);
	}
}