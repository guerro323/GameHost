using System;
using System.Diagnostics;

namespace revghost.Threading.V2.Apps;

public class DomainWorker : IDisposable, IReadOnlyDomainWorker
{
    private readonly Stopwatch deltaStopwatch;
    private readonly Stopwatch elapsedStopwatch;
    private readonly object synchronizationObject = new();

    private TimeSpan targetFrameRate;

    public DomainWorker(string name)
    {
        deltaStopwatch = new Stopwatch();
        elapsedStopwatch = new Stopwatch();

        Name = name ?? $"Unnamed '{GetType().Name}'";
    }

    public void Dispose()
    {
        deltaStopwatch.Stop();
        elapsedStopwatch.Stop();
    }

    public string Name { get; }

    public TimeSpan OptimalDeltaTarget
    {
        get
        {
            lock (synchronizationObject)
            {
                return targetFrameRate;
            }
        }
    }

    public TimeSpan Elapsed { get; private set; }

    public TimeSpan Delta { get; private set; }

    public TimeSpan RealtimeElapsed => elapsedStopwatch.Elapsed;
    public TimeSpan RealtimeDelta => RealtimeElapsed - Elapsed;

    public float Performance
    {
        get
        {
            // it did not ran yet...
            if (Delta <= TimeSpan.Zero) return 1f;

            return (float) (OptimalDeltaTarget.TotalMilliseconds / Delta.TotalMilliseconds);
        }
    }

    public WorkerMonitor StartMonitoring(TimeSpan targetFrameRate)
    {
        return new WorkerMonitor(this, targetFrameRate);
    }

    public struct WorkerMonitor : IDisposable
    {
        public readonly DomainWorker worker;

        public WorkerMonitor(DomainWorker worker, TimeSpan targetFrameRate)
        {
            this.worker = worker;
            lock (this.worker.synchronizationObject)
            {
                this.worker.targetFrameRate = targetFrameRate;
                this.worker.Delta = TimeSpan.Zero;
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
                worker.Elapsed = worker.elapsedStopwatch.Elapsed;
                worker.Delta = worker.deltaStopwatch.Elapsed;
            }
        }
    }
}