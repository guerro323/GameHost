using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Audio
{
    public struct PlayFlatAudioComponent
    {
        
    }
    
    [RestrictToApplication(typeof(GameAudioThreadingHost))]
    public class PlayFlatAudioSystem : AppSystem
    {
        public PlayFlatAudioSystem(WorldCollection collection) : base(collection)
        {
        }
    }
}
