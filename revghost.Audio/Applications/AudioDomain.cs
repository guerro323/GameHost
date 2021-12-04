using DefaultEcs;
using revghost.Domains;
using revghost.Domains.Time;
using revghost.Threading.V2;
using revghost.Threading.V2.Apps;

namespace GameHost.Audio.Applications;

public class AudioDomain : CommonDomainThreadListener
{
    private FixedTimeStep fts;

    private readonly TimeSpan targetFrequency;
    private readonly TimeApp timeApp;

    private readonly DomainWorker worker;

    public AudioDomain(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
    {
        targetFrequency = TimeSpan.FromSeconds(1f / 500f);
        timeApp = new TimeApp(Data.Context);
        fts = new FixedTimeStep(targetFrequency);

        worker = new DomainWorker("Audio");
    }

    public void SetTargetFrameRate(TimeSpan span)
    {
        Scheduler.Add(span =>
        {
            fts.SetTargetFrameTime(span);
            return true;
        }, span, default);
    }

    protected override ListenerUpdate OnUpdate()
    {
        var delta = worker.Delta;
        var updateCount = fts.GetUpdateCount(delta.TotalSeconds);

        var elapsed = worker.Elapsed;
        using (worker.StartMonitoring(targetFrequency))
        {
            timeApp.Update(elapsed, delta);
            using (CurrentUpdater.SynchronizeThread())
            {
                Scheduler.Run();

                while (updateCount-- > 0)
                {
                    timeApp.Update(elapsed - updateCount * delta, delta);
                    Data.Loop();
                }
            }
        }

        var timeToSleep = TimeSpan.FromTicks(Math.Max(targetFrequency.Ticks - worker.Delta.Ticks, 0));
        if (timeToSleep.Ticks > 0)
            worker.Delta += timeToSleep;

        return new ListenerUpdate
        {
            TimeToSleep = timeToSleep
        };
    }

    private class TimeApp : AppObject
    {
        private ManagedWorldTime managedWorldTime;
        private World world;

        private Entity worldTimeEntity;

        public TimeApp(Context context) : base(context)
        {
            DependencyResolver.Add(() => ref world);
            DependencyResolver.OnComplete(deps =>
            {
                worldTimeEntity = world.CreateEntity();
                managedWorldTime = new ManagedWorldTime();
            });
        }

        public void Update(TimeSpan total, TimeSpan delta)
        {
            if (DependencyResolver.Dependencies.Count > 0)
                return;

            if (!worldTimeEntity.Has<WorldTime>()) Context.BindExisting<IManagedWorldTime>(managedWorldTime);

            managedWorldTime.Total = total;
            managedWorldTime.Delta = delta;
            worldTimeEntity.Set(managedWorldTime.ToStruct());
        }
    }
}