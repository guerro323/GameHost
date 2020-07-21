using System.Collections.ObjectModel;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;
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
                buffer.WriteInt(Inputs.Count);
                foreach (var input in Inputs)
                    buffer.WriteStaticString(input.Target);
            }

            public override void Deserialize(ref DataBufferReader buffer)
            {
                var count = buffer.ReadValue<int>();
                var array = new CInput[count];
                for (var i = 0; i != count; i++)
                    array[i] = new CInput(buffer.ReadString());

                Inputs = new ReadOnlyCollection<CInput>(array);
            }
        }

        public uint DownCount, UpCount;

        public bool HasBeenPressed => DownCount > 0;
    }
}