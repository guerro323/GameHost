using System;
using System.Diagnostics;
using System.Threading;
using GameHost.Core.Applications;

namespace GameHost.Core.Threading
{
    public class ThreadingClient<TListener> : ApplicationClientBase
            where TListener : ThreadingHost<TListener>
    {
        private Lazy<Thread> thread = new Lazy<Thread>(() => ThreadingHost.TypeToThread[typeof(TListener)].Thread);

        public bool IsConnected { get; private set; }

        public override void Connect()
        {
            var sw = new Stopwatch();
            sw.Start();
            while (!ThreadingHost.TypeToThread.ContainsKey(typeof(TListener)))
            {
                if (sw.Elapsed.TotalSeconds > 1)
                    throw new InvalidOperationException($"Connecting to {typeof(TListener)} has taken too much time.");
            }

            if (thread != null)
                Console.WriteLine($"Successfuly connected to '{typeof(TListener).Name}' thread");

            IsConnected = true;
        }

        public override void Dispose()
        {
        }

        public ThreadLocker SynchronizeThread() => ThreadingHost.Synchronize<TListener>();
        public TListener Listener => ThreadingHost.GetListener<TListener>();
    }
}
