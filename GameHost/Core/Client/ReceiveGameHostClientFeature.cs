using GameHost.Applications;

namespace GameHost.Core.Client
{
	public class ReceiveGameHostClientFeature : IFeature
	{
		public int ServerPort { get; }

		public ReceiveGameHostClientFeature(int serverPort)
		{
			ServerPort = serverPort;
		}
	}
}