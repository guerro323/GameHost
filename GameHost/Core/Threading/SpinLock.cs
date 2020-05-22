using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace GameHost.Core.Threading
{
    // i saw that there is already an implementation like that... WITH THE SAME NAME.
    /*public class SpinLock
    {
        public int CurrentLockId;

        public void Wait()
        {
            var spin = new SpinWait();
            var target = Thread.CurrentThread.ManagedThreadId;
            while (Interlocked.CompareExchange(ref CurrentLockId, target, 0) != target)
            {
                spin.SpinOnce();
            }
        }

        public void Release()
        {
            Interlocked.CompareExchange(ref CurrentLockId, 0, Thread.CurrentThread.ManagedThreadId);
        }
    }*/

    public static class SpinLockExtension
    {
        public unsafe ref struct Safety
        {
            public bool      WasTaken;
            public SpinLock* SpinLock;

            public Safety(ref SpinLock spinLock)
            {
                WasTaken = false;

                SpinLock = (SpinLock*)Unsafe.AsPointer(ref spinLock);
                SpinLock->Enter(ref WasTaken);
            }

            public void Dispose()
            {
                if (WasTaken)
                    SpinLock->Exit();
                SpinLock = null;
            }
        }

        public static Safety SafeTake(this ref SpinLock spinLock)
        {
            return new Safety(ref spinLock);
        }
    }
}
