using GameHost.Core.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Event
{
    public struct OnWorldSystemAdded : IAppEvent
    {
        public WorldCollection Source;
        public object          System;
    }
}
