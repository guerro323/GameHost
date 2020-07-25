using DefaultEcs;
using GameHost.Worlds;

namespace GameHost.Applications
{
	/// <summary>
	/// This application is only used in the context of a <see cref="GlobalWorld"/>.
	/// </summary>
	public class ExecutiveEntryApplication : IApplication
	{
		public Entity          AssignedEntity { get; set; }
		public GlobalWorld     Global         { get; }
		public ApplicationData Data           { get; }

		public ExecutiveEntryApplication(GlobalWorld source)
		{
			Global = source;
			Data   = new ApplicationData(source.Context, source.World);
		}
	}
}