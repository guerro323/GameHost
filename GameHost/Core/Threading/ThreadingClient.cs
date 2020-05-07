using System;
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
            if (thread != null)
                Console.WriteLine(string.Format("Successfuly connected to '{0}' thread", typeof(TListener).Name));

            IsConnected = true;
        }

        public override void Dispose()
        {
        }

        public ThreadLocker SynchronizeThread() => ThreadingHost.Synchronize<TListener>();
        public TListener Listener => ThreadingHost.GetListener<TListener>();
    }
}
