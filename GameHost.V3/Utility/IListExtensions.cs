using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Collections.Pooled;

namespace GameHost.V3.Utility
{
    public static class IListExtensions
    {
        public static void ClearReference<T>(this PooledList<T> list)
        {
            list.Span.Clear();
            list.Clear();
        }

        public static void AddRange<TList, TValue, TOriginal>(this TList list, IEnumerable<TValue> elements)
            where TList : IList<TOriginal>
            where TValue : TOriginal
        {
            foreach (var element in elements)
                list.Add(element);
        }

        public static void AddRange<TList, T>(this TList list, IEnumerable<T> elements)
            where TList : IList<T>
        {
            foreach (var element in elements)
                list.Add(element);
        }
        
        public static void AddRange<TList>(this TList list, Stream stream) where TList : IList<byte>
        {
            switch (list)
            {
                case PooledList<byte> cast:
                    var span = cast.AddSpan((int) stream.Length);
                    stream.Read(span);
                    return;

                case List<byte> cast:
                    cast.Capacity = Math.Max(cast.Capacity, cast.Count + (int) stream.Length);
                    stream.Read(CollectionsMarshal.AsSpan(cast).Slice(cast.Count, (int) stream.Length));
                    return;
            }

            using var disposable = DisposableArray<byte>.Rent((int) stream.Length, out var mem);
            stream.Read(mem, 0, (int) stream.Length);

            var length = stream.Length;
            for (var i = 0; i < length; i++)
                list.Add(mem[0]);
        }

        public static async Task AddRangeAsync<TList>(this TList list, Stream stream) where TList : IList<byte>
        {
            using var disposable = DisposableArray<byte>.Rent((int) stream.Length, out var mem);
            await stream.ReadAsync(mem.AsMemory(0, (int) stream.Length));

            switch (list)
            {
                case PooledList<byte> cast:
                    cast.AddRange(mem);
                    return;

                case List<byte> cast:
                    cast.AddRange(mem);
                    return;
            }

            var length = stream.Length;
            for (var i = 0; i < length; i++)
                list.Add(mem[0]);
        }
    }
}