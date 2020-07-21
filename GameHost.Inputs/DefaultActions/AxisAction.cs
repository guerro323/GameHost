using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using GameHost.Core.Ecs;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;
using GameHost.Inputs.Systems;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.DefaultActions
{
    public struct AxisAction : IInputAction
    {
        public class Layout : InputLayoutBase
        {
            public CInput[] Negative;
            public CInput[] Positive;

            // empty for Activator.CreateInstance<>();
            public Layout(string id) : base(id)
            {
                Inputs = new ReadOnlyCollection<CInput>(Array.Empty<CInput>());
            }
            
            public Layout(string id, IEnumerable<CInput> negative, IEnumerable<CInput> positive) : base(id)
            {
                Negative = negative.ToArray();
                Positive = positive.ToArray();
                Inputs   = new ReadOnlyCollection<CInput>(Negative.Concat(Positive).ToArray());
            }

            public override void Serialize(ref DataBufferWriter buffer)
            {
                void write(CInput[] array, ref DataBufferWriter writer)
                {
                    writer.WriteInt(array.Length);
                    foreach (var input in array)
                        writer.WriteStaticString(input.Target);
                }

                write(Negative, ref buffer);
                write(Positive, ref buffer);
            }

            public override void Deserialize(ref DataBufferReader buffer)
            {
                void read(ref CInput[] array, ref DataBufferReader reader)
                {
                    var count = reader.ReadValue<int>();
                    array = new CInput[count];
                    for (var i = 0; i != count; i++)
                        array[i] = new CInput(reader.ReadString());
                }

                read(ref Negative, ref buffer);
                read(ref Positive, ref buffer);

                Inputs = new ReadOnlyCollection<CInput>(Negative.Concat(Positive).ToArray());
            }
        }

        public float Value;

        public class InputActionSystem : InputActionSystemBase<AxisAction, Layout>
        {
            public InputActionSystem(WorldCollection collection) : base(collection)
            {
            }

            public override void OnBeginFrame()
            {
                // dont set data to default
            }
        }

        public void Serialize(ref DataBufferWriter buffer)
        {
            buffer.WriteValue(Value);
        }

        public void Deserialize(ref DataBufferReader buffer)
        {
            Value = buffer.ReadValue<float>();
        }
    }
}