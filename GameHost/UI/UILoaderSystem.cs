using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using Noesis;

namespace GameHost.UI
{
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class UILoaderSystem<T> : AppSystem
        where T : ILoadableInterface
    {
        public UILoaderSystem(WorldCollection collection) : base(collection)
        {
        }
    }
}
