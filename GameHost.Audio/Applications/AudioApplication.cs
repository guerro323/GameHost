using GameHost.Injection;
using GameHost.Threading.Apps;
using GameHost.Worlds;

namespace GameHost.Audio.Applications
{
	public class AudioApplication : CommonApplicationThreadListener
	{
		public AudioApplication(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
		{
		}
	}
}