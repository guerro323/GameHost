using GameHost.Applications;
using GameHost.Core.IO;

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