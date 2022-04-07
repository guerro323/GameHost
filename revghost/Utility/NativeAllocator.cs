using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace revghost.Utility;

/// <summary>
/// Allocator that allocate managed object in native memory
/// </summary>
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
    
    /// <summary>
    /// Allocate an object and zero its memory
    /// </summary>
    /// <param name="additionalSize">The additional size to add to the object</param>
    /// <typeparam name="T">The type of the managed object</typeparam>
    /// <returns>Return the managed object on native memory</returns>
    /// <remarks><see cref="additionalSize"/> can be used for strings (<see cref="NativeAllocatorExtensions.AllocString"/>)</remarks>
    public readonly T AllocZeroed<T>(int additionalSize = 0)
    {
        var size = (nuint) (((int*) typeof(T).TypeHandle.Value)![1] + additionalSize);
        var memory = (byte*) Context->Alloc(ref *Context, ContextManagedObject, size);

        MemoryMarshal.CreateSpan(ref *memory, (int) size).Clear();

        *(IntPtr*) memory = typeof(T).TypeHandle.Value;
                
        return Unsafe.AsRef<T>(&memory);
    }

    /// <summary>
    /// Allocate an object with garbage memory
    /// </summary>
    /// <param name="additionalSize">The additional size to add to the object</param>
    /// <typeparam name="T">The type of the managed object</typeparam>
    /// <returns>Return the managed object on native memory</returns>
    /// <remarks><see cref="additionalSize"/> can be used for strings (<see cref="NativeAllocatorExtensions.AllocString"/>)</remarks>
    public readonly T New<T>(int additionalSize = 0)
    {
        var size = (nuint) (((int*) typeof(T).TypeHandle.Value)![1] + additionalSize);
        var memory = (byte*) Context->Alloc(ref *Context, ContextManagedObject, size);
        
        *(IntPtr*) memory = typeof(T).TypeHandle.Value;
                
        return Unsafe.AsRef<T>(&memory);
    }
    
    /// <summary>
    /// Get the base memory of an object (past of its header)
    /// </summary>
    /// <param name="obj">The object to get the memory pointer from</param>
    /// <returns>The pointer to the object memory</returns>
    public readonly ref byte GetObjectBaseMemory(object obj)
    {
        return ref *(byte*) Unsafe.As<object, IntPtr>(ref obj);
    }

    /// <summary>
    /// Free an object
    /// </summary>
    /// <param name="obj">The object to free</param>
    /// <typeparam name="T">The type of the managed object</typeparam>
    /// <returns>Whether or not it was successfully freed (if you don't use a tracking allocator the result will always be true)</returns>
    public readonly bool Free<T>(ref T obj)
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

    /// <summary>
    /// Dispose a <see cref="NativeAllocator"/> and its context
    /// </summary>
    public void Dispose()
    {
        if (Context->Dispose != null)
            Context->Dispose(ref *Context, ContextManagedObject);
        
        NativeMemory.Free(Context);
    }

    /// <summary>
    /// Create a new <see cref="NativeAllocator"/> context
    /// </summary>
    /// <param name="tracking">Whether or not allocation should be tracked</param>
    /// <returns>A new <see cref="NativeAllocator"/></returns>
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

    /// <summary>
    /// Create a custom <see cref="NativeAllocator"/> context
    /// </summary>
    /// <param name="data">Must be created from <see cref="NativeMemory.Alloc"/></param>
    /// <param name="managedObjectCompanion">Managed object that can be used as a companion for allocator methods</param>
    /// <returns>A new <see cref="NativeAllocator"/></returns>
    public static NativeAllocator CreateCustomContext(ref Data data, object managedObjectCompanion = null)
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
    public static string NewString(this in NativeAllocator allocator, int size)
    {
        var str = allocator.New<string>((size + 1) * sizeof(char));
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

    public static string NewString(this in NativeAllocator allocator, ReadOnlySpan<char> chars)
    {
        var str = NewString(allocator, chars.Length);
        var span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(str.AsSpan()), chars.Length);
        chars.CopyTo(span);

        return str;
    }

    public static string Join(this in NativeAllocator allocator, ReadOnlySpan<char> left, ReadOnlySpan<char> right)
    {
        var final = NewString(allocator, left.Length + right.Length);
        var span = MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(final.AsSpan()), final.Length);
        
        left.CopyTo(span[..left.Length]);
        right.CopyTo(span.Slice(left.Length, right.Length));

        return final;
    }

    public static GuardAllocation<T> GuardAlloc<T>(this in NativeAllocator allocator)
    {
        return new GuardAllocation<T>(allocator, allocator.AllocZeroed<T>());
    }
}

public unsafe struct GuardAllocation<T> : IDisposable
{
    private NativeAllocator _allocator;
    private object _object;

    public GuardAllocation(NativeAllocator allocator, object obj)
    {
        _allocator = allocator;
        _object = obj;
    }

    public T Value => Unsafe.As<object, T>(ref Unsafe.AsRef<GuardAllocation<T>>(Unsafe.AsPointer(ref this))._object);

    public void Dispose()
    {
        _allocator.Dispose();
    }
}