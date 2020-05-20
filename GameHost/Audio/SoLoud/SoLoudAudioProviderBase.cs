using System;
using System.Runtime.CompilerServices;
using DefaultEcs;
using GameHost.Core.Audio.Loader;
using GameHost.Core.Ecs;

namespace SoLoud
{
    public class SoLoudAudioProviderBase : AudioProviderBase
    {
        public SoLoudAudioProviderBase(WorldCollection collection) : base(collection)
        {
        }

        public override unsafe Entity LoadAudioFromData(ReadOnlySpan<byte> data)
        {
            // TODO: Check if data resource already exist and return it instead of recreating it at every call.

            var wav = new Wav();
            fixed (byte* dataPtr = &data.GetPinnableReference())
            {
                wav.loadMem((IntPtr)dataPtr, (uint)data.Length, aCopy: 1);
            }

            var resource = World.Mgr.CreateEntity();
            resource.Set(wav);

            return resource;
        }
    }
}
