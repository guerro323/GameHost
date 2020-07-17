using GameHost.Applications;
using GameHost.Core.IO;

namespace GameHost.Simulation.Features.ShareWorldState
{
	/// <summary>
	/// Feature to share world state to receivers.
	/// </summary>
	/// <remarks>
	/// Internally the feature will use the <see cref="Transport"/> variable to send the data.
	/// It shouldn't be used for sending data from a server to client in a multiplayer game.
	/// But it should be used to send data locally.
	/// </remarks>
	public class ShareWorldStateFeature : IFeature
	{
		public TransportDriver Transport;

		public ShareWorldStateFeature(TransportDriver transport)
		{
			this.Transport = transport;
		}
	}
}