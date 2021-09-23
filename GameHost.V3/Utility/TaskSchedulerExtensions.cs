using System;
using System.Threading;
using System.Threading.Tasks;

namespace GameHost.V3.Utility
{
    public static class TaskSchedulerExtensions
    {
        public static Task StartUnwrap(this TaskScheduler taskScheduler, Func<CancellationToken, Task> taskCreator, CancellationToken cancellationToken)
        {
            return Task.Factory
                .StartNew(() => taskCreator(cancellationToken), cancellationToken, TaskCreationOptions.AttachedToParent, taskScheduler)
                .Unwrap();
        }
		
        public static Task<T> StartUnwrap<T>(this TaskScheduler taskScheduler, Func<CancellationToken, Task<T>> taskCreator, CancellationToken cancellationToken)
        {
            return Task.Factory
                .StartNew(() => taskCreator(cancellationToken), cancellationToken, TaskCreationOptions.AttachedToParent, taskScheduler)
                .Unwrap();
        }

        public static Task StartUnwrap(this TaskScheduler taskScheduler, Func<Task> taskCreator)
        {
            return Task.Factory
                .StartNew(taskCreator, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler)
                .Unwrap();
        }
		
        public static Task<T> StartUnwrap<T>(this TaskScheduler taskScheduler, Func<Task<T>> taskCreator)
        {
            return Task.Factory
                .StartNew(taskCreator, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler)
                .Unwrap();
        }
    }
}