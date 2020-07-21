using System;
using System.Diagnostics;
using System.Text.Json;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Inputs.Components;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Systems
{
    public class InputDatabase : AppSystem
    {
        public InputDatabase(WorldCollection collection) : base(collection)
        {
        }

        public void Register(JsonDocument document)
        {
            Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");

            throw new NotImplementedException("Method not implemented: Register(JsonDocument)");
        }

        public Entity RegisterSingle<TAction>(params InputLayoutBase[] layouts)
            where TAction : IInputAction, new()
        {
            Debug.Assert(DependencyResolver.Dependencies.Count == 0, "DependencyResolver.Dependencies.Count == 0");

            var ac = World.Mgr.CreateEntity();
            ac.Set(new InputActionLayouts(layouts));
            ac.Set(new InputActionType(typeof(TAction)));
            ac.Set(default(TAction));
            ac.Set(new PushInputLayoutChange());

            return ac;
        }
    }
}