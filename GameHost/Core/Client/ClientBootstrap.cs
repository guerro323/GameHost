using DefaultEcs;

namespace GameHost.Core.Client
{
	public class ClientBootstrap
	{
		public string ExecutablePath { get; set; }
		public string LaunchArgs     { get; set; }
	}

	public readonly struct LaunchClient
	{
		public readonly Entity entity;
		
		public LaunchClient(Entity entity)
		{
			this.entity = entity;
		}
	}
}