using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.Inputs
{
    [RestrictToApplication(typeof(GameInputThreadingHost))]
    public class InputInitializationSystem : AppSystem
    {
        public InputInitializationSystem(WorldCollection collection) : base(collection)
        {
        }
    }
}
