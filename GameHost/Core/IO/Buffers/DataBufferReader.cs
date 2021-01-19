using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameHost.Native.Char;

namespace RevolutionSnapshot.Core.Buffers
{
    public ref partial struct DataBufferReader
    {
        public int CurrReadIndex;
        public int Length => Span.Length;
        
        public unsafe DataBufferReader(IntPtr dataPtr, int length) : this((byte*) dataPtr, length)
        {
        }

        public unsafe DataBufferReader(byte* dataPtr, int length)
        {
            if (dataPtr == null)
                throw new InvalidOperationException("dataPtr is null");
            
            CurrReadIndex = 0;
            Span   = new Span<byte>(dataPtr, length);
        }

        public DataBufferReader(DataBufferReader reader, int start, int end)
        {
            Span   = reader.Span.Slice(start, end - start);
            CurrReadIndex = 0;
        }

        public DataBufferReader(DataBufferWriter writer)
        {
            Span          = writer.Span;
            CurrReadIndex = 0;
        }

        private Span<byte> Span;

        public DataBufferReader(Span<byte> data)
        {
            Span          = data;
            CurrReadIndex = 0;
        }

        public int GetReadIndexAndSetNew(DataBufferMarker marker, int size)
        {
            if (size < 0)
                throw new InvalidOperationException();
            
            var readIndex = !marker.Valid ? CurrReadIndex : marker.Index;
            if (readIndex >= Length)
            {
                throw new IndexOutOfRangeException($"p1 r={readIndex} >= l={Length}");
            }

            CurrReadIndex = readIndex + size;
            if (CurrReadIndex > Length)
            {
                throw new IndexOutOfRangeException($"p2 {CurrReadIndex} ({readIndex} + {size}) > {Length}");
            }

            return readIndex;
        }

        public unsafe void ReadUnsafe(byte* data, int index, int size)
        {
            Span.Slice(index, size).CopyTo(new Span<byte>(data, size)); 
        }

        public Span<T> ReadSpan<T>(int size, DataBufferMarker marker = default)
            where T : struct
        {
            size *= Unsafe.SizeOf<T>();
            var readIndex = GetReadIndexAndSetNew(marker, size);
            // Set it for later usage
            CurrReadIndex = readIndex + size;

            return MemoryMarshal.Cast<byte, T>(Span.Slice(readIndex, size));
        }

        public void ReadDataSafe<T>(Span<T> span, DataBufferMarker marker = default)
            where T : struct
        {
            ReadSpan<T>(span.Length).CopyTo(span);
        }

        public T ReadValue<T>(DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return ReadSpan<T>(1, marker)[0];
        }

        public DataBufferMarker CreateMarker(int index)
        {
            return new DataBufferMarker(index);
        }

        public string ReadString(DataBufferMarker marker = default)
        {
            var length = ReadValue<int>();
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            
            return new string(ReadSpan<char>(length));
        }

        public TCharBuffer ReadBuffer<TCharBuffer>(DataBufferMarker marker = default(DataBufferMarker))
            where TCharBuffer : struct, ICharBuffer
        {
            var length = ReadValue<int>();
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));

            var buffer = new TCharBuffer {Length = length};
            ReadDataSafe(buffer.Span.Slice(0, length));

            return buffer;
        }
    }
}