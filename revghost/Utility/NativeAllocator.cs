using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace revghost.Utility;

public unsafe struct NativeAllocator : IDisposable
{
    public struct Data
    {
        public delegate*<ref Data, object, nuint, void*> Alloc;
        public delegate*<ref Data, object, void*, bool> Free;
        public delegate*<ref Data, object, void> Dispose;
    }

    internal Data* Context;
    internal object ContextManagedObject;
    
    public T AllocZeroed<T>(int additionalSize = 0)
    {
        var size = (nuint) (((int*) typeof(T).TypeHandle.Value)![1] + additionalSize);
        var memory = (byte*) Context->Alloc(ref *Context, ContextManagedObject, size);

        MemoryMarshal.CreateSpan(ref *memory, (int) size).Clear();

        *(IntPtr*) memory = typeof(T).TypeHandle.Value;
                
        return Unsafe.AsRef<T>(&memory);
    }

    public readonly T Alloc<T>(int additionalSize = 0)
    {
        var size = (nuint) (((int*) typeof(T).TypeHandle.Value)![1] + additionalSize);
        var memory = (byte*) Context->Alloc(ref *Context, ContextManagedObject, size);
        
        *(IntPtr*) memory = typeof(T).TypeHandle.Value;
                
        return Unsafe.AsRef<T>(&memory);
    }
    
    public readonly ref byte GetObjectBaseMemory(object obj)
    {
        return ref *(byte*) Unsafe.As<object, IntPtr>(ref obj);
    }

    public bool Free<T>(ref T obj)
    {
        if (obj == null)
            return false;
        
        ref var memory = ref Unsafe.NullRef<byte>();
        if (typeof(T).IsValueType)
        {
            memory = Unsafe.As<T, byte>(ref obj);
        }
        else
        {
            memory = ref GetObjectBaseMemory(obj);
        }

        obj = default;
        return Context->Free(ref *Context, ContextManagedObject, Unsafe.AsPointer(ref memory));
    }

    public void Dispose()
    {
        if (Context->Dispose != null)
            Context->Dispose(ref *Context, ContextManagedObject);
        
        NativeMemory.Free(Context);
    }

    public static NativeAllocator CreateContext(bool tracking = true)
    {
        NativeAllocator allocator;
        allocator.Context = (Data*) NativeMemory.Alloc((nuint) Unsafe.SizeOf<Data>());
        allocator.Context->Alloc = tracking ? &TrackingAlloc : &DefaultAlloc;
        allocator.Context->Free = tracking ? &TrackingFree : &DefaultFree;
        allocator.Context->Dispose = tracking ? &TrackingDispose : null;
        allocator.ContextManagedObject = tracking ? new HashSet<IntPtr>() : null;

        return allocator;
    }

    public static NativeAllocator CreateContext(ref Data data, object managedObjectCompanion = null)
    {
        NativeAllocator allocator;
        allocator.Context = (Data*) Unsafe.AsPointer(ref data);
        allocator.ContextManagedObject = managedObjectCompanion;

        return allocator;
    }

    private static void* DefaultAlloc(ref Data data, object _, nuint size)
    {
        return NativeMemory.Alloc(size);
    }

    private static bool DefaultFree(ref Data data, object _, void* memory)
    {
        NativeMemory.Free(memory);
        return true;
    }
    
    private static void* TrackingAlloc(ref Data data, object companion, nuint size)
    {
        var hashset = Unsafe.As<object, HashSet<IntPtr>>(ref companion);
        var memory = NativeMemory.Alloc(size);
        hashset.Add((IntPtr) memory);
        
        return memory;
    }

    private static bool TrackingFree(ref Data data, object companion, void* memory)
    {
        var hashset = Unsafe.As<object, HashSet<IntPtr>>(ref companion);
        var managedPtr = (IntPtr) memory;
        if (!hashset.Contains(managedPtr))
            return false;
        
        NativeMemory.Free(memory);
        hashset.Remove(managedPtr);
        return true;
    }

    private static void TrackingDispose(ref Data data, object companion)
    {
        var hashset = Unsafe.As<object, HashSet<IntPtr>>(ref companion);
        foreach (var ptr in hashset)
        {
            NativeMemory.Free((void*) ptr);
        }
    }
}

public static class NativeAllocatorExtensions
{
    public static string AllocString(this in NativeAllocator allocator, int size)
    {
        var str = allocator.Alloc<string>((size + 1) * sizeof(char));
        var memorySpan = MemoryMarshal.CreateSpan(
            ref allocator.GetObjectBaseMemory(str),
            // Header + Length + FirstChar
            sizeof(long) + sizeof(int) + sizeof(char)
        );
        MemoryMarshal.Cast<byte, int>(memorySpan)[2] = size;

        var charsWithNull = memorySpan[(sizeof(long) + sizeof(int))..];
        charsWithNull.Clear();

        return str;
    }

    public static string AllocString(this in NativeAllocator allocator, ReadOnlySpan<char> chars)
    {
        var str = AllocString(allocator, chars.Length);
        var span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(str.AsSpan()), chars.Length);
        chars.CopyTo(span);

        return str;
    }
}