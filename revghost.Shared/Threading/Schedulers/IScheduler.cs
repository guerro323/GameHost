namespace revghost.Shared.Threading.Schedulers;

public interface IScheduler : IDisposable
{
    void Add<T>(Func<T, bool> action, T args, in SchedulingParametersWithArgs parameters);
}

public interface IRunnableScheduler : IScheduler
{
    void Run();
}

public static class SchedulerExtensions
{
    private static readonly Func<Action, bool> SchedulingWithoutParameters = ac =>
    {
        ac();
        return true;
    };

    public static void Add(this IScheduler scheduler,
        Action action, bool scheduleOnce = false)
    {
        scheduler.Add(SchedulingWithoutParameters, action, new SchedulingParametersWithArgs
        {
            OnceWithMethodAndArgs = scheduleOnce
        });
    }

    public static void Add<T>(this IScheduler scheduler,
        Action<T> action, T data, bool scheduleOnce = false)
    {
        scheduler.Add(With<T>.SchedulingWithoutReturn, (action, data), new SchedulingParametersWithArgs
        {
            OnceWithMethodAndArgs = scheduleOnce
        });
    }

    private static class With<T>
    {
        public static readonly Func<(Action<T> ac, T data), bool> SchedulingWithoutReturn = tuple =>
        {
            tuple.ac(tuple.data);
            return true;
        };
    }
}