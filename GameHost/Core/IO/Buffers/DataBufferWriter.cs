using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.IO;
using K4os.Compression.LZ4;

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

    public partial struct DataBufferWriter : IDisposable
    {
        internal struct DataBuffer
        {
            public AllocatedMemory memory;
            public int             length;
            public int             capacity;
        }

        private AllocatedMemory m_Data;
        private IAllocator      m_Allocator;

        public bool IsCreated => m_Data.IsValid;

        public int Length
        {
            get => m_Data.As<DataBuffer>().length;
            set => m_Data.As<DataBuffer>().length = value;
        }

        public int Capacity
        {
            get => m_Data.As<DataBuffer>().capacity;
            set
            {
                ref var data = ref m_Data.As<DataBuffer>();

                var dataCapacity = data.capacity;
                if (dataCapacity == value)
                    return;

                if (dataCapacity > value)
                    throw new InvalidOperationException("New capacity is shorter than current one");

                if (data.capacity < data.length)
                    throw new InvalidOperationException("length bigger than capacity");

                var newBuffer = m_Allocator.Alloc((uint) value);

                data.memory.Allocator = m_Allocator; // make sure that it remember our original allocator
                data.memory.Span.Slice(0, data.length).CopyTo(newBuffer.Span.Slice(0, data.length));
                
                data.memory.Dispose();

                data.memory   = newBuffer;
                data.capacity = value;
            }
        }

        private AllocatedMemory getMemory()
        {
            ref var data = ref m_Data.As<DataBuffer>();
            data.memory.Allocator = m_Allocator;
            return data.memory;
        }

        public IntPtr     GetSafePtr() => getMemory().DataPtr;
        public Span<byte> Span         => getMemory().Span.Slice(0, m_Data.As<DataBuffer>().length);
        public Span<byte> CapacitySpan => getMemory().Span;

        public DataBufferWriter(int capacity, IAllocator allocator = null)
        {
            m_Allocator = allocator ?? ManagedAllocator.Default;

            m_Data = m_Allocator.Alloc((uint) Unsafe.SizeOf<DataBuffer>());

            ref var data = ref m_Data.As<DataBuffer>();
            data.length   = 0;
            data.capacity = capacity;
            data.memory   = m_Allocator.Alloc((uint) capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteDataSafe(Span<byte> data, DataBufferMarker marker)
        {
            ref var buffer = ref m_Data.As<DataBuffer>();

            var dataLength = buffer.length;
            var writeIndex = marker.Valid ? marker.Index : dataLength;

            var predictedLength = writeIndex + data.Length;

            if (buffer.capacity <= predictedLength)
            {
                Capacity = predictedLength * 2;
            }

            buffer.length = Math.Max(predictedLength, dataLength);

            data.CopyTo(Span.Slice(writeIndex));

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
            getMemory().Dispose();
            m_Data.Dispose();

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

    public partial struct DataBufferWriter
    {
        public int WriteCompressed(Span<byte> data, LZ4Level level = LZ4Level.L05_HC)
        {
            var compressedSize   = LZ4Codec.MaximumOutputSize(data.Length);
            var compressedMarker = WriteInt(compressedSize);
            WriteInt(data.Length);
            
            Capacity += compressedSize;

            //Console.WriteLine($"Write at {Length} with size {compressedSize} (write back compressedSize at {compressedMarker.Index})");
            var size = LZ4Codec.Encode(data, CapacitySpan.Slice(Length, compressedSize), level);
            WriteInt(size, compressedMarker);

            Length += size;

            return size;
        }
    }
    
    public partial struct DataBufferReader
    {
        public Span<byte> ReadDecompressed(PooledList<byte> fill)
        {
            var compressedSize   = ReadValue<int>();
            var uncompressedSize = ReadValue<int>();

            var uncompressed = fill.AddSpan(uncompressedSize);
            var compressed   = Span.Slice(GetReadIndexAndSetNew(default, compressedSize));
            LZ4Codec.Decode(compressed, uncompressed);

            return uncompressed;
        }
    }
}