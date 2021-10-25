namespace revghost.Shared.Threading.Schedulers;

public static class SchedulerAsyncExtensions
{
    private static readonly Func<(Action, TaskCompletionSource<bool>), bool> ScheduleAsyncCached = args =>
    {
        try
        {
            args.Item1();
            args.Item2.SetResult(true);
        }
        catch (Exception ex)
        {
            args.Item2.SetException(ex);
            throw;
        }

        return true;
    };

    public static Task AddAsync(this IScheduler scheduler,
        Action action, SchedulingParameters parameters)
    {
        var t = new TaskCompletionSource<bool>();
        scheduler.Add(ScheduleAsyncCached, (action, t),
            parameters.Once ? SchedulingParametersWithArgs.AsOnceWithArgs : default);
        return t.Task;
    }

    public static Task AddAsync<T>(this IScheduler scheduler,
        Action<T> action, T args, SchedulingParametersWithArgs parameters)
    {
        var t = new TaskCompletionSource<bool>();
        scheduler.Add(WithArgs<T>.ScheduleAsyncCached, (action, args, t), parameters);
        return t.Task;
    }

    public static void ContinueWithScheduler(this Task task,
        IScheduler scheduler, Action action)
    {
        task.ContinueWith(_ => { scheduler.Add(action); });
    }

    public static void ContinueWithScheduler<T>(this Task<T> task,
        IScheduler scheduler, Action<T> action)
    {
        task.ContinueWith(t => { scheduler.Add(action, t.Result); });
    }

    private static class WithArgs<T>
    {
        public static readonly Func<(Action<T>, T, TaskCompletionSource<bool>), bool> ScheduleAsyncCached = args =>
        {
            try
            {
                args.Item1(args.Item2);
                args.Item3.SetResult(true);
            }
            catch (Exception ex)
            {
                args.Item3.SetException(ex);
                throw;
            }

            return true;
        };
    }
}