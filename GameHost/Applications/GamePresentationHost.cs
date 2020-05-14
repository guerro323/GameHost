using System;
using System.Collections.Generic;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.Injection;

namespace GameHost.Applications
{
    /// <summary>
    /// The presentation application is the syncing point between SIMULATION, RENDER, AUDIO applications.
    /// </summary>
    public class GamePresentationHost : GameThreadedHostApplicationBase<GamePresentationHost>
    {
        private class InstanceData
        {
            public bool isReady;
            public WorldCollection renderWorld;
            public WorldCollection simulationWorld;
        }
        
        private Dictionary<Instance, InstanceData> instanceData = new Dictionary<Instance, InstanceData>();
        
        private GameRenderThreadingClient     renderClient;
        private GameSimulationThreadingClient simulationClient;

        public GamePresentationHost(Context context, TimeSpan? frequency = null) : base(context, frequency)
        {
        }

        protected override void OnInit()
        {
            renderClient.Connect();
            simulationClient.Connect();
        }

        protected override void OnQuit()
        {

        }

        protected override void OnFixedUpdate(int step, TimeSpan delta, TimeSpan elapsedTime)
        {
            using (Worker.StartFrame())
            {
                foreach (var kvp in MappedWorldCollection)
                {
                    if (!instanceData[kvp.Key].isReady)
                    {
                        if (renderClient.Listener.MappedWorldCollection.TryGetValue(kvp.Key, out instanceData[kvp.Key].renderWorld)
                            && simulationClient.Listener.MappedWorldCollection.TryGetValue(kvp.Key, out instanceData[kvp.Key].simulationWorld))
                        {
                            instanceData[kvp.Key].isReady = true;
                        }

                        continue;
                    }

                    var world = kvp.Value;
                    world.DoInitializePass();
                    var timeSystem = world.GetOrCreate(collection => new TimeSystem {WorldCollection = collection});
                    timeSystem.Update(new WorldTime {Delta = delta, Total = elapsedTime});
                    world.DoUpdatePass();
                }
            }
        }

        protected override void OnInstanceAdded<TInstance>(in TInstance instance)
        {
            base.OnInstanceAdded(in instance);
            instanceData[instance] = new InstanceData {isReady = false};
        }
    }
}
