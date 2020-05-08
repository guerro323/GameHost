using GameHost.Core.Threading;

namespace GameHost.Applications
{
    public class GameAudioThreadingHost : GameThreadedHostApplicationBase<GameAudioThreadingHost>
    {
        protected override void OnInit()
        {
            AddInstance(Instance.CreateInstance<Instance>("AudioApplication", Context));
        }
    }

    public class GameAudioThreadingClient : ThreadingClient<GameAudioThreadingHost>
    {
        
    }
}
