using System;
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
                Inputs = new ReadOnlyCollection<CInput>(inputs);
            }

            public override void Serialize(ref DataBufferWriter buffer)
            {
                var count = Inputs.Count;
                buffer.WriteInt(count);
                for (var i = 0; i < count; i++)
                    buffer.WriteStaticString(Inputs[i].Target);
            }

            public override void Deserialize(ref DataBufferReader buffer)
            {
                var count = buffer.ReadValue<int>();
                var array = new CInput[count];
                for (var i = 0; i < count; i++)
                    array[i] = new CInput(buffer.ReadString());

                Inputs = new ReadOnlyCollection<CInput>(array);
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
                    foreach (var input in layout.Inputs)
                        if (Backend.GetInputControl(input.Target) is { } buttonControl)
                        {
                            action.DownCount += buttonControl.wasPressedThisFrame ? 1u : 0;
                            action.UpCount   += buttonControl.wasReleasedThisFrame ? 1u : 0;
                        }

                    entity.Set(action);
                }
            }
        }

        public void Serialize(ref DataBufferWriter buffer)
        {
            buffer.WriteValue(DownCount);
            buffer.WriteValue(UpCount);
        }

        public void Deserialize(ref DataBufferReader buffer)
        {
            DownCount += buffer.ReadValue<uint>();
            UpCount   += buffer.ReadValue<uint>();
        }
    }
}