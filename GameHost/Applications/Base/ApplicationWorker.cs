using System;
using System.Diagnostics;
using GameHost.Core.Threading;

namespace GameHost.Applications
{
    public class ApplicationWorker : Worker, IWorkerDelta, INamedWorker
    {
        private readonly object synchronizationObject = new object();

        private readonly Stopwatch deltaStopwatch;
        private          TimeSpan  elapsed;

        private readonly Stopwatch elapsedStopwatch;

        private TimeSpan targetFrameRate;

        public ApplicationWorker(string name)
        {
            deltaStopwatch   = new Stopwatch();
            elapsedStopwatch = new Stopwatch();

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

        public TimeSpan Delta { get; private set; }

        public WorkerMonitor StartMonitoring(TimeSpan targetFrameRate)
        {
            return new WorkerMonitor(this, targetFrameRate);
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

                this.worker.elapsedStopwatch.Start();
                this.worker.deltaStopwatch.Restart();
            }

            public void Dispose()
            {
                worker.elapsedStopwatch.Stop();
                worker.deltaStopwatch.Stop();

                lock (worker.synchronizationObject)
                {
                    worker.elapsed = worker.elapsedStopwatch.Elapsed;
                    worker.Delta   = worker.deltaStopwatch.Elapsed;
                }
            }
        }
    }
}
