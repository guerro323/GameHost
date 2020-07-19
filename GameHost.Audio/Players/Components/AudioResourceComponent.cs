using DefaultEcs;

namespace GameHost.Audio.Players
{
	public readonly struct AudioResourceComponent
	{
		public readonly Entity Source;

		public AudioResourceComponent(Entity source)
		{
			Source = source;
		}
	}
}