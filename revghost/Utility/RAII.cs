//#define ENABLE_DEBUG_TRACE

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace revghost.Utility;

public static unsafe class RAII
{
    public enum DebugReferenceOption
    {
        None,
        Simple,
        Full
    }
    
    public static readonly Allocator Allocator;

    public static bool IsTracking = true;
    public static bool DebugMemory = false;
    public static DebugReferenceOption DebugReference = DebugReferenceOption.None;

    private struct Information
    {
#if ENABLE_DEBUG_TRACE
        public StackTrace? AllocStrackTrace;
        public List<StackTrace> References;
#endif
    }

    private static readonly ConcurrentDictionary<IntPtr, Information> Tracked = new();

    private static readonly Allocator.Data MethodTable = new()
    {
        Alloc = &Alloc,
        Free = &Free,
        Dispose = &Dispose
    };
    
    static RAII()
    {
        Allocator = Allocator.CreateCustomContext(
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

    public static void Increment(object obj
        #if ENABLE_DEBUG_TRACE
        , StackTrace referenceTrace
#endif
        )
    {
        var result = Interlocked.Increment(ref GetCounter(obj));
        if (DebugReference != DebugReferenceOption.None)
        {
            var managedPtr = IntPtr.Subtract(Unsafe.As<object, IntPtr>(ref obj), sizeof(int));
            var additional = string.Empty;
            if (DebugReference == DebugReferenceOption.Full)
            {
                var stackTrace = new StackTrace(1, true);
                additional = $"\n{stackTrace}";

#if ENABLE_DEBUG_TRACE
                if (referenceTrace == null)
                {
                    additional = " (Untracked!)" + additional;
                }
                else
                {
                    var references = Tracked[managedPtr].References;
                    lock (references)
                    {
                        references.Add(referenceTrace);
                    }
                }
#endif
            }

            HostLogger.Output.Info(
                $"Increment {managedPtr}, count={result}{additional}",
                "RAII",
                "ref/inc");
        }
    }

    public static bool Decrement(object obj
#if ENABLE_DEBUG_TRACE
    , StackTrace referenceTrace
#endif
        )
    {
        var result = Interlocked.Decrement(ref GetCounter(obj));
        if (DebugReference != DebugReferenceOption.None)
        {
            var additional = string.Empty;
            var managedPtr = IntPtr.Subtract(Unsafe.As<object, IntPtr>(ref obj), sizeof(int));
            
            if (DebugReference == DebugReferenceOption.Full)
            {
                var stackTrace = new StackTrace(1, true);
                additional = $"\n{stackTrace}";
                
#if ENABLE_DEBUG_TRACE
                if (referenceTrace == null)
                {
                    additional = " (Untracked!)" + additional;
                }
                else
                {
                    var references = Tracked[managedPtr].References;
                    lock (references)
                    {
                        references.Remove(referenceTrace);
                    }
                }
#endif
            }
            
            HostLogger.Output.Info(
                $"Decrement {managedPtr}, count={result}{additional}",
                "RAII",
                "ref/dec");
        }
        
        if (result <= 0)
        {
            Allocator.Free(ref obj);
            return true;
        }

        return false;
    }

    private static void* Alloc(ref Allocator.Data data, object companion, nuint size)
    {
        var memory = (byte*) NativeMemory.Alloc(size + sizeof(int));
        Unsafe.AsRef<int>(memory) = 0;

        if (DebugMemory)
        {
            HostLogger.Output.Info(
                $"Allocated pointer at: {(IntPtr) memory} (size={size})",
                "RAII",
                "alloc");
        }

        var information = new Information();
#if ENABLE_DEBUG_TRACE
        if (DebugReference == DebugReferenceOption.Full)
        {
            information.AllocStrackTrace = new StackTrace(1, true);
            information.References = new List<StackTrace>();
        }
#endif

        if (!Tracked.TryAdd((IntPtr) memory, information))
            throw new InvalidOperationException("invalid synchronization");

        return memory + sizeof(int);
    }

    private static bool Free(ref Allocator.Data data, object companion, void* ptr)
    {
        var managedPtr = (IntPtr) (byte*) ptr - sizeof(int);
        if (Tracked.ContainsKey(managedPtr))
        {
            NativeMemory.Free((byte*) ptr - sizeof(int));
            Tracked.TryRemove(managedPtr, out _);

            if (DebugMemory)
            {
                HostLogger.Output.Info(
                    $"Freed tracked pointer: {managedPtr}",
                    "RAII",
                    "free/success");
            }

            return true;
        }

        if (DebugMemory)
        {
            HostLogger.Output.Warn(
                $"Tried to freed an address that wasn't tracked: {managedPtr}",
                "RAII",
                "free/not-tracked");
        }

        return false;
    }

    private static void Dispose(ref Allocator.Data data, object companion)
    {
        
    }

    public static void ReadTracked<TList>(TList list)
        where TList : IList<TrackedInfo>
    {
        foreach (var (address, info) in Tracked)
        {
            list.Add(new TrackedInfo
            {
                Address = address,
                ReferenceCount = GetCounter(address),
#if ENABLE_DEBUG_TRACE
                AllocationStacktrace = info.AllocStrackTrace,
                References = info.References
#endif
            });
        }
    }

    public struct TrackedInfo
    {
        public IntPtr Address;
        public int ReferenceCount;
        public StackTrace? AllocationStacktrace;
        public List<StackTrace> References;

        [MethodImpl(MethodImplOptions.NoOptimization)]
        public override string ToString()
        {
            var additional = string.Empty;
            if (AllocationStacktrace is { } stacktrace)
            {
                additional += $"\n Created at:\n{stacktrace}";
            }

            if (References != null)
            {
                foreach (var referenceStackTrace in References)
                {
                    additional += $"Referenced at:\n{referenceStackTrace}";
                }
            }
            return $"Address={Address} References={ReferenceCount + 1}{additional}";
        }
    }
}

public unsafe struct RefClass<T> : IDisposable
    where T : class
{
    // we use this field to throw if we are doing any structural changes on a copied RefClass<T>
    private IntPtr _creationAddr;
    private T _object;

    #if ENABLE_DEBUG_TRACE
    private StackTrace _debugTrace;
#endif

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

    private void _Increment()
    {
#if ENABLE_DEBUG_TRACE
        if (RAII.DebugReference == RAII.DebugReferenceOption.Full)
            _debugTrace = new StackTrace(1, true);
        RAII.Increment(_object, _debugTrace);
#else
        RAII.Increment(_object);
#endif
    }

    private void _Decrement(object target)
    {
#if ENABLE_DEBUG_TRACE
        RAII.Decrement(target, _debugTrace);
#else
        RAII.Decrement(target);
#endif
    }

    internal void CoreCreate()
    {
        StructuralChange();

        _object = RAII.Allocator.New<T>();
        _Increment();
    }

    [StackTraceHidden]
    internal void CoreSetNoIncrement(T replace)
    {
        StructuralChange();
        
        var previous = _object;

        _object = replace;

        if (RAII.IsRAIIObject(previous))
        {
            _Decrement(previous);
        }
    }
    
    [StackTraceHidden]
    internal void CoreSet(T replace)
    {
        StructuralChange();
        
        var previous = _object;
        _object = replace;
        
        if (RAII.IsRAIIObject(_object))
        {
            _Increment();
        }

        if (RAII.IsRAIIObject(previous))
        {
            _Decrement(previous);
        }
    }
    
    [StackTraceHidden]
    internal void CoreSet(RefClass<T> replace)
    {
        StructuralChange();
        
        var previous = _object;
        _object = replace._object;
        
        if (RAII.IsRAIIObject(_object))
        {
            _Increment();
        }

        if (RAII.IsRAIIObject(previous))
        {
            _Decrement(previous);
        }
    }

    public T GetUnsafe()
    {
        return _object;
    }

    [StackTraceHidden]
    public TRet Act<TArg, TRet>(Func<T, TArg, TRet> func, TArg arg)
    {
        TRet ret;
        if (RAII.IsRAIIObject(_object))
        {
            _Increment();
            ret = func(_object, arg);
            _Decrement(_object);
        }
        else
        {
            ret = func(_object, arg);
        }

        return ret;
    }

    [StackTraceHidden]
    public TRet Act<TRet>(Func<T, TRet> func)
    {
        TRet ret;
        if (RAII.IsRAIIObject(_object))
        {
            _Increment();
            ret = func(_object);
            _Decrement(_object);
        }
        else
        {
            ret = func(_object);
        }

        return ret;
    }

    [StackTraceHidden]
    public void Act<TArg>(Action<T, TArg> action, TArg arg)
    {
        if (RAII.IsRAIIObject(_object))
        {
            _Increment(); 
            action(_object, arg);
            _Decrement(_object);
        }
        else
        {
            action(_object, arg);
        }
    }

    [StackTraceHidden]
    public void Act(Action<T> action)
    {
        if (RAII.IsRAIIObject(_object))
        {
            _Increment(); 
            action(_object);
            _Decrement(_object);
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
            _Decrement(_object);
        }
    }
}

public static class RefClass
{
    public static RefClass<T> Open<T>(T obj) where T : class
    {
        var ret = new RefClass<T>();
        ret.CoreSet(obj);
        
        return ret;
    }
    
    public static RefClass<T> Argument<T>(T obj) where T : class
    {
        var ret = new RefClass<T>();
        ret.CoreSetNoIncrement(obj);
        
        return ret;
    }
}

public static class RefClassExtension
{
    [StackTraceHidden]
    public static void Set<T>(this in RefClass<T> refClass, T obj) where T : class
    {
        ref var bypassReadonly = ref Unsafe.AsRef(in refClass);
        bypassReadonly.CoreSet(obj);
    }
    
    [StackTraceHidden]
    public static void Set<T>(this in RefClass<T> refClass, RefClass<T> other) where T : class
    {
        ref var bypassReadonly = ref Unsafe.AsRef(in refClass);
        bypassReadonly.CoreSet(other);
    }
}