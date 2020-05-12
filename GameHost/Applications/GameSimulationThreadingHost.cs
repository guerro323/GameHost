using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DefaultEcs;
using DryIoc;
using GameHost.Core.Ecs;
using GameHost.Core.Game;
using GameHost.Core.Threading;
using GameHost.Entities;
using GameHost.Injection;

namespace GameHost.Applications
{
    public class GameSimulationThreadingHost : GameThreadedHostApplicationBase<GameSimulationThreadingHost>
    {
        public GameSimulationThreadingHost(Context context, TimeSpan? frequency = null) : base(context, frequency)
        {
        }

        protected override void OnInit()
        {

        }

        protected override void OnQuit()
        {

        }
    }

    public class GameSimulationThreadingClient : ThreadingClient<GameSimulationThreadingHost>
    {
        public void InjectAssembly(Assembly assembly)
        {
            Listener.InjectAssembly(assembly);
        }

        public void AddInstance(Instance instance)
        {
            Listener.AddInstance(instance);
        }
    }
}
