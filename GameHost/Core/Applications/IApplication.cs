using DefaultEcs;
using GameHost.Worlds;

namespace GameHost.Applications
{
	/// <summary>
	/// An application is where an <see cref="ApplicationData"/> live, it can be executed locally, multi-threaded; on another application, or even online.
	/// </summary>
	public interface IApplication
	{
		public Entity          AssignedEntity { get; set; }
		public GlobalWorld     Global         { get; }
		public ApplicationData Data           { get; }
	}

	public readonly struct ApplicationName
	{
		public readonly string Value;

		public ApplicationName(string value) => Value = value;
	}
}