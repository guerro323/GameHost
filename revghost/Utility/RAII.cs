using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace revghost.Utility;

public static unsafe class RAII
{
    public static readonly NativeAllocator Allocator;

    public static bool IsTracking = true;

    private struct Information
    {
        
    }

    private static readonly Dictionary<IntPtr, Information> Tracked = new();

    private static readonly NativeAllocator.Data MethodTable = new()
    {
        Alloc = &Alloc,
        Free = &Free,
        Dispose = &Dispose
    };
    
    static RAII()
    {
        Allocator = NativeAllocator.CreateCustomContext(
            ref MethodTable
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref int GetCounter(object obj)
    {
        var ptr = (int*) Unsafe.As<object, IntPtr>(ref obj);
        return ref *(ptr - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsRAIIObject(object obj)
    {
        var ptr = Unsafe.As<object, IntPtr>(ref obj);
        return Tracked.ContainsKey(IntPtr.Subtract(ptr, sizeof(int)));
    }

    public static void Increment(object obj)
    {
        GetCounter(obj) += 1;
    }

    public static bool Decrement(object obj)
    {
        ref var counter = ref GetCounter(obj);
        counter -= 1;
        if (counter <= 0)
        {
            Allocator.Free(ref obj);
            return true;
        }

        return false;
    }

    private static void* Alloc(ref NativeAllocator.Data data, object companion, nuint size)
    {
        var memory = (byte*) NativeMemory.Alloc(size + sizeof(int));
        Unsafe.AsRef<int>(memory) = 0;
        
        Console.WriteLine($"alloc at {(IntPtr) memory}");
        
        Tracked.Add((IntPtr) memory, new Information());

        return memory + sizeof(int);
    }

    private static bool Free(ref NativeAllocator.Data data, object companion, void* ptr)
    {
        var managedPtr = (IntPtr) (byte*) ptr - sizeof(int);
        Console.WriteLine($"free at {managedPtr}");

        if (Tracked.ContainsKey(managedPtr))
        {
            NativeMemory.Free((byte*) ptr - sizeof(int));
            Tracked.Remove(managedPtr);
            
            return true;
        }
        
        return false;
    }

    private static void Dispose(ref NativeAllocator.Data data, object companion)
    {
        
    }
}

public unsafe struct RefClass<T> : IDisposable
    where T : class
{
    // we use this field to throw if we are doing any structural changes on a copied RefClass<T>
    private IntPtr _creationAddr;
    private T _object;

    public RefClass()
    {
        this = default;
        
        _creationAddr = (IntPtr) Unsafe.AsPointer(ref this);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void StructuralChange()
    {
        var currentAddr = (IntPtr) Unsafe.AsPointer(ref this);
        if (_creationAddr != IntPtr.Zero && _creationAddr != currentAddr)
            throw new InvalidOperationException(
                $"A RefClass<{typeof(T)}> had incoming structural changed but was copied. " +
                $"Original={_creationAddr}, Current={currentAddr}"
            );

        _creationAddr = currentAddr;
    }

    public void Create()
    {
        StructuralChange();

        _object = RAII.Allocator.New<T>();
        RAII.Increment(_object);
    }

    public void Set(T replace)
    {
        StructuralChange();
        
        var previous = _object;
        if (RAII.IsRAIIObject(replace))
        {
            RAII.Increment(replace);
        }

        _object = replace;

        if (RAII.IsRAIIObject(previous))
        {
            RAII.Decrement(previous);
        }
    }

    public T Get()
    {
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Increment(_object);
        }

        return _object;
    }
    
    public T GetUnsafe()
    {
        return _object;
    }

    public TRet Act<TArg, TRet>(Func<T, TArg, TRet> func, TArg arg)
    {
        TRet ret;
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Increment(_object);
            ret = func(_object, arg);
            RAII.Decrement(_object);
        }
        else
        {
            ret = func(_object, arg);
        }

        return ret;
    }

    public TRet Act<TRet>(Func<T, TRet> func)
    {
        TRet ret;
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Increment(_object);
            ret = func(_object);
            RAII.Decrement(_object);
        }
        else
        {
            ret = func(_object);
        }

        return ret;
    }

    public void Act<TArg>(Action<T, TArg> action, TArg arg)
    {
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Increment(_object); 
            action(_object, arg);
            RAII.Decrement(_object);
        }
        else
        {
            action(_object, arg);
        }
    }

    public void Act(Action<T> action)
    {
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Increment(_object); 
            action(_object);
            RAII.Decrement(_object);
        }
        else
        {
            action(_object);
        }
    }

    public void Dispose()
    {
        if (RAII.IsRAIIObject(_object))
        {
            RAII.Decrement(_object);
        }
    }

    public static implicit operator RefClass<T>(T obj)
    {
        var ret = new RefClass<T>();
        ret.Set(obj);

        return ret;
    }
}