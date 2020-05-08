using System;
using GameHost.Core.Threading;
using GameHost.Injection;

namespace GameHost.Applications
{
    public class GameAudioThreadingHost : GameThreadedHostApplicationBase<GameAudioThreadingHost>
    {
        protected override void OnInit()
        {
            AddInstance(Instance.CreateInstance<Instance>("AudioApplication", Context));
        }

        protected override void OnQuit()
        {
            
        }

        public GameAudioThreadingHost(Context context, TimeSpan? frequency = null) : base(context, frequency)
        {
        }
    }

    public class GameAudioThreadingClient : ThreadingClient<GameAudioThreadingHost>
    {
        
    }
}
