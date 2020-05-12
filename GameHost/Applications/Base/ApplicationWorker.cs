using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using GameHost.Core.Threading;

namespace GameHost.Applications
{
    public class ApplicationWorker : Worker, IWorkerDelta, IWorkerWithFrames, INamedWorker
    {
        private readonly object synchronizationObject = new object();

        private readonly Stopwatch deltaStopwatch;
        private          TimeSpan  elapsed;

        private readonly Stopwatch frameDeltaStopwatch;

        private readonly Stopwatch elapsedStopwatch;

        private TimeSpan targetFrameRate;

        public ApplicationWorker(string name)
        {
            deltaStopwatch      = new Stopwatch();
            elapsedStopwatch    = new Stopwatch();
            frameDeltaStopwatch = new Stopwatch();

            FrameListener = new ConcurrentBag<IFrameListener>();

            Name = name ?? $"Unnamed '{GetType().Name}'";
        }

        public override EWorkerType Type => EWorkerType.Cycle;

        public TimeSpan TargetFrameRate
        {
            get
            {
                lock (synchronizationObject)
                {
                    return targetFrameRate;
                }
            }
        }

        public override float Performance
        {
            get
            {
                // it did not ran yet...
                if (Delta <= TimeSpan.Zero)
                {
                    return 0f;
                }

                return (float)(Delta.TotalMilliseconds / TargetFrameRate.TotalMilliseconds);
            }
        }

        public override bool     IsRunning { get; }
        public override TimeSpan Elapsed   => elapsed;

        public string Name { get; }

        public TimeSpan Delta { get; set; }

        public IProducerConsumerCollection<IFrameListener> FrameListener { get; private set; }

        private int MonitorFrame;
        public WorkerMonitor StartMonitoring(TimeSpan targetFrameRate)
        {
            MonitorFrame++;
            return new WorkerMonitor(this, targetFrameRate);
        }

        private int Frame;
        public FrameMonitor StartFrame()
        {
            Frame++;
            return new FrameMonitor(this);
        }

        public struct WorkerMonitor : IDisposable
        {
            public readonly ApplicationWorker worker;

            public WorkerMonitor(ApplicationWorker worker, TimeSpan targetFrameRate)
            {
                this.worker = worker;
                lock (this.worker.synchronizationObject)
                {
                    this.worker.targetFrameRate = targetFrameRate;
                    this.worker.Delta           = TimeSpan.Zero;
                }

                if (!this.worker.elapsedStopwatch.IsRunning)
                    this.worker.elapsedStopwatch.Start();
                this.worker.deltaStopwatch.Restart();
            }

            public void Dispose()
            {
                worker.deltaStopwatch.Stop();

                lock (worker.synchronizationObject)
                {
                    worker.elapsed = worker.elapsedStopwatch.Elapsed;
                    worker.Delta   = worker.deltaStopwatch.Elapsed;
                }
            }
        }

        public struct FrameMonitor : IDisposable
        {
            public readonly ApplicationWorker worker;

            public FrameMonitor(ApplicationWorker worker)
            {
                this.worker = worker;
                this.worker.frameDeltaStopwatch.Restart();
            }

            public void Dispose()
            {
                this.worker.frameDeltaStopwatch.Stop();

                var wf = new WorkerFrame {CollectionIndex = this.worker.MonitorFrame, Frame = this.worker.Frame, Delta = this.worker.frameDeltaStopwatch.Elapsed};
                foreach (var listener in this.worker.FrameListener) // it does allocate :(
                    listener.Add(wf);
            }
        }
    }
}
