using System.Collections.ObjectModel;
using GameHost.Core.Ecs;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;
using GameHost.Inputs.Systems;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.DefaultActions
{
    public struct PressAction : IInputAction
    {
        public class Layout : InputLayoutBase
        {
            public Layout(string id, params CInput[] inputs) : base(id)
            {
                Inputs = inputs;
            }
        }

        public uint DownCount, UpCount;

        public bool HasBeenPressed => DownCount > 0;

        public class InputActionSystem : InputActionSystemBase<PressAction, Layout>
        {
            public InputActionSystem(WorldCollection collection) : base(collection)
            {
            }

            public override void OnInputUpdate()
            {
                var currentLayout = World.Mgr.Get<InputCurrentLayout>()[0];
                foreach (var entity in InputQuery.GetEntities())
                {
                    var layouts = GetLayouts(entity);
                    if (!layouts.TryGetOrDefault(currentLayout.Id, out var layout))
                        return;

                    PressAction action = default;

                    foreach (var input in layout.Inputs.Span)
                        if (Backend.GetInputControl(input.Target) is { } buttonControl)
                        {
                            action.DownCount += buttonControl.wasPressedThisFrame ? 1u : 0;
                            action.UpCount   += buttonControl.wasReleasedThisFrame ? 1u : 0;
                        }

                    entity.Set(action);
                }
            }
        }
    }
}