using System;
using System.Runtime.CompilerServices;
using Collections.Pooled;
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

    public unsafe partial struct DataBufferWriter : IDisposable
    {
        internal struct DataBuffer
        {
            public byte* buffer;
            public int   length;
            public int   capacity;
        }

        private DataBuffer* m_Data;

        public bool IsCreated => m_Data != null;

        public int Length
        {
            get => m_Data->length;
            set => m_Data->length = value;
        }

        public int Capacity
        {
            get => m_Data->capacity;
            set
            {
                var dataCapacity = m_Data->capacity;
                if (dataCapacity == value)
                    return;

                if (dataCapacity > value)
                    throw new InvalidOperationException("New capacity is shorter than current one");

                if (m_Data->capacity < m_Data->length)
                    throw new InvalidOperationException("length bigger than capacity");

                var newBuffer = (byte*) UnsafeUtility.Malloc(value);
                UnsafeUtility.MemCpy(newBuffer, m_Data->buffer, m_Data->length);
#if DEBUG
                if (!new Span<byte>(m_Data->buffer, m_Data->length).SequenceEqual(new Span<byte>(newBuffer, m_Data->length)))
                    throw new InvalidOperationException($"not equal buffer");
#endif
                UnsafeUtility.Free(m_Data->buffer);

                m_Data->buffer   = newBuffer;
                m_Data->capacity = value;
            }
        }

        public IntPtr     GetSafePtr() => (IntPtr) m_Data->buffer;
        public Span<byte> Span         => new Span<byte>(m_Data->buffer, m_Data->length);
        public Span<byte> CapacitySpan => new Span<byte>(m_Data->buffer, m_Data->capacity);


        public DataBufferWriter(int capacity)
        {
            m_Data           = (DataBuffer*) UnsafeUtility.Malloc(sizeof(DataBuffer));
            m_Data->buffer   = (byte*) UnsafeUtility.Malloc(capacity);
            m_Data->length   = 0;
            m_Data->capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteData(byte* data, int index, int length)
        {
            UnsafeUtility.MemCpy(m_Data->buffer + index, data, length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteDataSafe(byte* data, int writeSize, DataBufferMarker marker)
        {
            ref var buffer = ref *m_Data;
            
            var dataLength = buffer.length;

            int writeIndex;
            if (marker.Valid)
                writeIndex = marker.Index;
            else
                writeIndex = dataLength;
           // var writeIndex = marker.Index * (*(byte*) &marker.Valid) + dataLength * (1 - (*(byte*) &marker.Valid));
            
            // Copy from GetWriteInfo()

            var predictedLength = writeIndex + writeSize;

            // Copy from TryResize()
            if (buffer.capacity <= predictedLength)
            {
                Capacity = predictedLength * 2;
            }

            // Copy from WriteData()
            UnsafeUtility.MemCpy(buffer.buffer + writeIndex, data, writeSize);

            buffer.length = Math.Max(predictedLength, dataLength);

            DataBufferMarker rm;
            rm.Valid = true;
            rm.Index = writeIndex;

            return rm;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteSpan<T>(Span<T> span, DataBufferMarker marker = default)
        {
            return WriteDataSafe((byte*) Unsafe.AsPointer(ref span.GetPinnableReference()), Unsafe.SizeOf<T>() * span.Length, marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteRef<T>(ref T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return WriteDataSafe((byte*) Unsafe.AsPointer(ref val), Unsafe.SizeOf<T>(), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteUnmanaged<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : unmanaged
        {
            return WriteDataSafe((byte*) &val, sizeof(T), marker);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DataBufferMarker WriteValue<T>(T val, DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            return WriteDataSafe((byte*) Unsafe.AsPointer(ref val), Unsafe.SizeOf<T>(), marker);
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
            UnsafeUtility.Free(m_Data->buffer);
            UnsafeUtility.Free(m_Data);

            m_Data = null;
        }
    }

    public unsafe partial struct DataBufferWriter
    {
        public DataBufferMarker WriteByte(byte val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(byte), marker);
        }

        public DataBufferMarker WriteShort(short val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(short), marker);
        }

        public DataBufferMarker WriteInt(int val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(int), marker);
        }

        public DataBufferMarker WriteLong(long val, DataBufferMarker marker = default(DataBufferMarker))
        {
            return WriteDataSafe((byte*) &val, sizeof(long), marker);
        }

        public void WriteDynamicInt(ulong integer)
        {
            if (integer == 0)
            {
                WriteUnmanaged<byte>((byte) 0);
            }
            else if (integer <= byte.MaxValue)
            {
                WriteByte((byte) sizeof(byte));
                WriteUnmanaged<byte>((byte) integer);
            }
            else if (integer <= ushort.MaxValue)
            {
                WriteByte((byte) sizeof(ushort));
                WriteUnmanaged((ushort) integer);
            }
            else if (integer <= uint.MaxValue)
            {
                WriteByte((byte) sizeof(uint));
                WriteUnmanaged((uint) integer);
            }
            else
            {
                WriteByte((byte) sizeof(ulong));
                WriteUnmanaged(integer);
            }
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);

            WriteByte((byte) (m1 | (m2 << 2)), maskMarker);
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2, in ulong r3)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);
            var m3         = setval(ref this, r3);

            WriteByte((byte) (m1 | (m2 << 2) | (m3 << 4)), maskMarker);
        }

        public void WriteDynamicIntWithMask(in ulong r1, in ulong r2, in ulong r3, in ulong r4)
        {
            byte setval(ref DataBufferWriter data, in ulong i)
            {
                if (i <= byte.MaxValue)
                {
                    data.WriteUnmanaged((byte) i);
                    return 0;
                }

                if (i <= ushort.MaxValue)
                {
                    data.WriteUnmanaged((ushort) i);
                    return 1;
                }

                if (i <= uint.MaxValue)
                {
                    data.WriteUnmanaged((uint) i);
                    return 2;
                }

                data.WriteUnmanaged(i);
                return 3;
            }

            var maskMarker = WriteByte(0);
            var m1         = setval(ref this, r1);
            var m2         = setval(ref this, r2);
            var m3         = setval(ref this, r3);
            var m4         = setval(ref this, r4);

            WriteByte((byte) (m1 | (m2 << 2) | (m3 << 4) | (m4 << 6)), maskMarker);
        }

        public void WriteBuffer(DataBufferWriter dataBuffer)
        {
            WriteDataSafe((byte*) dataBuffer.GetSafePtr(), dataBuffer.Length, default(DataBufferMarker));
        }

        public void WriteStaticString(string val)
        {
            fixed (char* strPtr = val)
            {
                WriteStaticString(strPtr, val.Length);
            }
        }
        
        public void WriteStaticString(Span<char> val)
        {
            fixed (char* strPtr = val)
            {
                WriteStaticString(strPtr, val.Length);
            }
        }

        public void WriteStaticString(char* val, int strLength)
        {
            /*WriteInt(strLength);
            WriteDataSafe((byte*) val, strLength * sizeof(char), default);*/
            var span = new Span<char>(val, strLength);
            WriteInt(span.Length);
            WriteSpan(span);
        }
    }

    public unsafe partial struct DataBufferWriter
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
    
    public unsafe partial struct DataBufferReader
    {
        public Span<byte> ReadDecompressed(PooledList<byte> fill)
        {
            var compressedSize   = ReadValue<int>();
            var uncompressedSize = ReadValue<int>();

            var uncompressed = fill.AddSpan(uncompressedSize);

            unsafe
            {
                // var compressed = new Span<byte>(DataPtr + GetReadIndexAndSetNew(default, compressedSize * sizeof(byte)), compressedSize);
                var compressed = Span.Slice(GetReadIndexAndSetNew(default, compressedSize));
                LZ4Codec.Decode(compressed, uncompressed);
            }

            return uncompressed;
        }
    }
}