using System;
using System.Runtime.CompilerServices;
using GameHost.Native.Char;

namespace RevolutionSnapshot.Core.Buffers
{
    public unsafe ref partial struct DataBufferReader
    {
        public int CurrReadIndex;
        public int Length => Span.Length;
        
        public DataBufferReader(IntPtr dataPtr, int length) : this((byte*) dataPtr, length)
        {
        }

        public DataBufferReader(byte* dataPtr, int length)
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

        public void ReadUnsafe(byte* data, int index, int size)
        {
            Span.Slice(index, size).CopyTo(new Span<byte>(data, size)); 
        }

        public void ReadDataSafe(byte* data, int size, DataBufferMarker marker = default)
        {
            var readIndex = GetReadIndexAndSetNew(marker, size);
            // Set it for later usage
            CurrReadIndex = readIndex + size;
            // Read the value
            ReadUnsafe(data, readIndex, size);
        }

        public void ReadDataSafe<T>(Span<T> span, DataBufferMarker marker = default)
            where T : struct
        {
            ReadDataSafe((byte*) Unsafe.AsPointer(ref span.GetPinnableReference()), span.Length * Unsafe.SizeOf<T>(), marker);
        }

        public unsafe T ReadValue<T>(DataBufferMarker marker = default(DataBufferMarker))
            where T : struct
        {
            T value = default;
            ReadDataSafe((byte*) Unsafe.AsPointer(ref value), Unsafe.SizeOf<T>());
            return value;
        }

        public DataBufferMarker CreateMarker(int index)
        {
            return new DataBufferMarker(index);
        }

        public ulong ReadDynInteger(DataBufferMarker marker = default(DataBufferMarker))
        {
            var byteCount = ReadValue<byte>();

            if (byteCount == 0) return 0;
            if (byteCount == sizeof(byte)) return ReadValue<byte>();
            if (byteCount == sizeof(ushort)) return ReadValue<ushort>();
            if (byteCount == sizeof(uint)) return ReadValue<uint>();
            if (byteCount == sizeof(ulong)) return ReadValue<ulong>();

            throw new InvalidOperationException($"Expected byte count range: [{sizeof(byte)}..{sizeof(ulong)}], received: {byteCount}");
        }

        public void ReadDynIntegerFromMask(out ulong r1, out ulong r2)
        {
            void getval(ref DataBufferReader data, int mr, ref ulong i)
            {
                if (mr == 0) i = data.ReadValue<byte>();
                if (mr == 1) i = data.ReadValue<ushort>();
                if (mr == 2) i = data.ReadValue<uint>();
                if (mr == 3) i = data.ReadValue<ulong>();
            }

            var mask = ReadValue<byte>();
            var val1 = (mask & 3);
            var val2 = (mask & 12) >> 2;

            r1 = default;
            r2 = default;

            getval(ref this, val1, ref r1);
            getval(ref this, val2, ref r2);
        }

        public void ReadDynIntegerFromMask(out ulong r1, out ulong r2, out ulong r3)
        {
            void getval(ref DataBufferReader data, int mr, ref ulong i)
            {
                if (mr == 0) i = data.ReadValue<byte>();
                if (mr == 1) i = data.ReadValue<ushort>();
                if (mr == 2) i = data.ReadValue<uint>();
                if (mr == 3) i = data.ReadValue<ulong>();
            }

            var mask = ReadValue<byte>();
            var val1 = (mask & 3);
            var val2 = (mask & 12) >> 2;
            var val3 = (mask & 48) >> 4;

            r1 = default;
            r2 = default;
            r3 = default;

            getval(ref this, val1, ref r1);
            getval(ref this, val2, ref r2);
            getval(ref this, val3, ref r3);
        }

        public void ReadDynIntegerFromMask(out ulong r1, out ulong r2, out ulong r3, out ulong r4)
        {
            void getval(ref DataBufferReader data, int mr, ref ulong i)
            {
                if (mr == 0) i = data.ReadValue<byte>();
                if (mr == 1) i = data.ReadValue<ushort>();
                if (mr == 2) i = data.ReadValue<uint>();
                if (mr == 3) i = data.ReadValue<ulong>();
            }

            var mask = ReadValue<byte>();
            var val1 = (mask & 3);
            var val2 = (mask & 12) >> 2;
            var val3 = (mask & 48) >> 4;
            var val4 = (mask & 192) >> 6;

            r1 = default;
            r2 = default;
            r3 = default;
            r4 = default;

            getval(ref this, val1, ref r1);
            getval(ref this, val2, ref r2);
            getval(ref this, val3, ref r3);
            getval(ref this, val3, ref r4);
        }

        public string ReadString(DataBufferMarker marker = default)
        {
            var length = ReadValue<int>();
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            
            if (length < 64)
            {
                Span<char> span = stackalloc char[length];
                ReadDataSafe(new Span<char>(Unsafe.AsPointer(ref span.GetPinnableReference()), length), marker);
                return new string(span);
            }

            var ptr = UnsafeUtility.Malloc(length * sizeof(char));
            ReadDataSafe(new Span<char>(ptr, length), marker);
            UnsafeUtility.Free(ptr);
            return new string((char*) ptr, 0, length);
        }

        public TCharBuffer ReadBuffer<TCharBuffer>(DataBufferMarker marker = default(DataBufferMarker))
            where TCharBuffer : struct, ICharBuffer
        {
            var length = ReadValue<int>();
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            
            if (length < 64)
            {
                Span<char> span = stackalloc char[length];
                ReadDataSafe(new Span<char>(Unsafe.AsPointer(ref span.GetPinnableReference()), length), marker);
                return CharBufferUtility.Create<TCharBuffer>(span);
            }

            var ptr = UnsafeUtility.Malloc(length * sizeof(char));
            ReadDataSafe((byte*) ptr, length * sizeof(char), marker);
            var buffer = CharBufferUtility.Create<TCharBuffer>(new Span<char>(ptr, length));
            UnsafeUtility.Free(ptr);
            return buffer;
        }
    }
}