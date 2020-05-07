using GameHost.Core.Ecs;
using GameHost.Core.IO;

namespace GameHost.Core.Inputs
{
    public class InputInitializationSystem : AppSystem
    {
        public IStorage Storage { get; set; }

        protected override void OnInit()
        {
        }
    }
}
