using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace GameHost.Core.Threading
{
    [Flags]
    public enum EWorkerType
    {
        /// <summary>
        /// Unknown type
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Small works, mostly used to get information quickly. Or to send information quickly and forget about them...
        /// </summary>
        Small = 1,

        /// <summary>
        /// Heavy work, used for sending/receive a lot or long information.
        /// </summary>
        Heavy = Small << 1,

        /// <summary>
        /// A worker that can be updated multiple time. Mostly used with applications or scheduler
        /// </summary>
        Cycle = Heavy << 1
    }

    public struct WorkerFrame
    {
        public int      CollectionIndex;
        public int      Frame;
        public TimeSpan Delta;
    }

    public abstract class Worker
    {
        public abstract EWorkerType Type { get; }

        /// <summary>
        /// Indicate the performance of a worker between [0..1..+infinity]
        /// </summary>
        /// <remarks>
        /// A value that goes near of 0 indicate a better performance than expected
        /// </remarks>
        /// <remarks>
        /// A value near of 1 indicate that this worker is optimal
        /// </remarks>
        /// <remarks>
        /// A value that's more than 1 indicate this worker's performance is bad.
        /// </remarks>
        public abstract float Performance { get; }

        /// <summary>
        /// Is this worker currently running?
        /// </summary>
        public abstract bool IsRunning { get; }

        /// <summary>
        /// Get the elapsed time from this worker
        /// </summary>
        public abstract TimeSpan Elapsed { get; }

        public static string GetName(Worker worker)
        {
            if (worker == null)
                return "<Null Worker>";
            
            if (worker is INamedWorker named)
                return named.Name;
            return worker.GetType().Name;
        }
    }

    /// <summary>
    /// A worker that support delta timing. Mostly for <see cref="EWorkerType.Cycle"/> work type...
    /// </summary>
    public interface IWorkerDelta
    {
        /// <summary>
        /// Delta time span
        /// </summary>
        public TimeSpan Delta { get; }
    }
    
    public interface IFrameListener
    {
        bool Add(WorkerFrame frame);
    }

    public interface IWorkerWithFrames
    {
        IProducerConsumerCollection<IFrameListener> FrameListener { get; }
    }

    /// <summary>
    /// Name of the worker
    /// </summary>
    public interface INamedWorker
    {
        public string Name { get; }
    }
}
