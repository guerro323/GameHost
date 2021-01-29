using System;
using System.Threading.Tasks;

namespace GameHost.Core.Threading
{
	public static class SchedulerExtensions
	{
		private static readonly Action<(Action, TaskCompletionSource<bool>)> ScheduleAsyncCached = args =>
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
		};

		private static class WithArgs<T>
		{
			public static readonly Action<(Action<T>, T, TaskCompletionSource<bool>)> ScheduleAsyncCached = args =>
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
			};
		}

		public static Task ScheduleAsync(this IScheduler scheduler, Action action, SchedulingParameters parameters)
		{	
			var t = new TaskCompletionSource<bool>();
			scheduler.Schedule(ScheduleAsyncCached, (action, t), parameters.Once ? SchedulingParametersWithArgs.AsOnceWithArgs : default);
			return t.Task;
		}

		public static Task ScheduleAsync<T>(this IScheduler scheduler, Action<T> action, T args, SchedulingParametersWithArgs parameters)
		{
			var t = new TaskCompletionSource<bool>();
			scheduler.Schedule(WithArgs<T>.ScheduleAsyncCached, (action, args, t), parameters);
			return t.Task;
		}
	}
}