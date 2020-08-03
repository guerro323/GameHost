using System;
using System.Diagnostics;
using System.Text.Json;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Inputs.Components;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;

namespace GameHost.Inputs.Systems
{
    public class InputDatabase : AppSystem
    {
        private int maxId = 1;

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
            ac.Set(new InputEntityId(maxId++));
            ac.Set(new InputActionLayouts(layouts));
            ac.Set(new InputActionType(typeof(TAction)));
            ac.Set(default(TAction));
            ac.Set(new PushInputLayoutChange());

            return ac;
        }
    }
}