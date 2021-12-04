using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RevolutionSnapshot.Core.Buffers
{
    public struct DataBufferMarker
    {
        public bool Valid;
        public int  Index;

        public DataBufferMarker(int index)
        {
            Index = index;
            Valid = true;
        }

        public DataBufferMarker GetOffset(int offset)
        {
            return new DataBufferMarker(Index + offset);
        }
    }

    public unsafe partial struct DataBufferWriter : IDisposable
    {
        internal struct DataBuffer
        {
            public byte* memory;
            public int length;
            public int capacity;
        }

        private DataBuffer* m_Data;

        public bool IsCreated => m_Data != null;

        public ref int Length => ref m_Data->length;

        public int Capacity
        {
            get => m_Data->capacity;
            set
            {
                if (m_Data->capacity == value)
                    return;
                if (m_Data->capacity > value)
                    throw new InvalidOperationException("New capacity is shorter than current one");

                if (m_Data->capacity < m_Data->length)
                    throw new InvalidOperationException("length bigger than capacity");

                var newBuffer = (byte*) NativeMemory.Alloc((uint) value);
                Unsafe.CopyBlock(newBuffer, m_Data->memory, (uint) m_Data->capacity);

                NativeMemory.Free(m_Data->memory);

                m_Data->memory = newBuffer;
                m_Data->capacity = value;
            }
        }

        public Span<byte> Span => new(m_Data->memory, m_Data->length);
        public Span<byte> CapacitySpan => new(m_Data->memory, m_Data->capacity);

        // Provide a constructor with zero parameters so that it can initialize the data safely
        public DataBufferWriter() : this(0)
        {
        }

        public DataBufferWriter(int capacity)
        {
            m_Data = (DataBuffer*) NativeMemory.Alloc((nuint) sizeof(DataBuffer));

            m_Data->length = 0;
            m_Data->capacity = capacity;
            m_Data->memory = (byte*) NativeMemory.Alloc((nuint) capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteDataSafe(Span<byte> data, DataBufferMarker marker)
        {
            ref var buffer = ref Unsafe.AsRef<DataBuffer>(m_Data);

            var dataLength = buffer.length;
            var writeIndex = marker.Valid ? marker.Index : dataLength;

            var predictedLength = writeIndex + data.Length;

            if (buffer.capacity <= predictedLength)
            {
                Capacity = predictedLength * 2;
            }

            buffer.length = Math.Max(predictedLength, dataLength);

            data.CopyTo(Span[writeIndex..]);

            DataBufferMarker rm;
            rm.Valid = true;
            rm.Index = writeIndex;

            return rm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteSpan<T>(Span<T> span, DataBufferMarker marker = default)
            where T : unmanaged
        {
            return WriteDataSafe(MemoryMarshal.AsBytes(span), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteUnmanaged<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : unmanaged
        {
            return WriteDataSafe(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref val, 1)), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteValue<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return WriteDataSafe(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref val, 1)), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker CreateMarker(int index)
        {
            DataBufferMarker marker = default;
            marker.Valid = true;
            marker.Index = index;
            return marker;
        }

        public void Dispose()
        {
            NativeMemory.Free(m_Data);

            m_Data = default;
        }
    }

    public partial struct DataBufferWriter
    {
        public DataBufferMarker WriteByte(byte val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteValue(val, marker);
        }

        public DataBufferMarker WriteShort(short val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteValue(val, marker);
        }

        public DataBufferMarker WriteInt(int val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteValue(val, marker);
        }

        public DataBufferMarker WriteLong(long val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteValue(val, marker);
        }

        public void WriteBuffer(DataBufferWriter dataBuffer)
        {
            WriteDataSafe(dataBuffer.Span, default(DataBufferMarker));
        }

        public unsafe void WriteStaticString(string val)
        {
            var span = val.AsSpan();
            WriteInt(span.Length);
            fixed (char* buffer = span)
            {
                // convert from readOnly to readWrite
                WriteSpan(new Span<char>(buffer, span.Length));
            }
        }
    }
}