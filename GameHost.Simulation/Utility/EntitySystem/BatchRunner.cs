using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Collections.Pooled;

namespace StormiumTeam.GameBase.Utility.Misc.EntitySystem
{
	public interface IBatch
	{
		int  PrepareBatch(int taskCount);
		void Execute(int index, int maxUseIndex, int task, int taskCount);
	}

	public struct BatchRequest
	{
		public bool Equals(BatchRequest other)
		{
			return Id == other.Id && Version == other.Version;
		}

		public override bool Equals(object obj)
		{
			return obj is BatchRequest other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Version);
		}

		public readonly int Id, Version;

		public BatchRequest(int id, int version)
		{
			Id      = id;
			Version = version;
		}
	}

	public interface IBatchRunner
	{
		bool         IsCompleted(BatchRequest request);
		BatchRequest Queue(IBatch    batch);
	}

	public static class BatchRunnerExtensions
	{
		public static void WaitForCompletion(this IBatchRunner runner, BatchRequest request)
		{
			while (!runner.IsCompleted(request))
				Thread.Sleep(0);
		}
	}

	public class ThreadBatchRunner : IBatchRunner, IDisposable
	{
		// 512 is a good value since it we run very short tasks
		public const int MaxRunningBatches = 512;

		private class TaskState
		{
			public CancellationToken Token;
			public int               TaskIndex;
			public int               TaskCount;

			public bool IsPerformanceCritical;
			public int  ProcessorId;
			
			public ConcurrentBag<QueuedBatch> Batches;
			public BatchResult[]              Results;
		}

		private struct QueuedBatch
		{
			public int BatchId;
			
			public IBatch Batch;
			public int    Index;
			public int    MaxUseIndex;
		}

		private struct BatchResult
		{
			public int SuccessfulWrite;
			public int MaxIndex;

			public bool IsCompleted => SuccessfulWrite >= MaxIndex;
		}

		private Task[]                       tasks;
		private TaskState[]                  states;
		private ConcurrentBag<QueuedBatch>[] taskBatches;
		private BatchResult[]                batchResults;

		private int[] batchVersions;

		private static void runTask(object obj)
		{
			if (obj is not TaskState state)
				throw new InvalidOperationException($"Task {obj} is not a {typeof(TaskState)}");

			try
			{
				Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

				var spin            = new SpinWait();
				var sleep0Threshold = 10;
				var sleepCount      = 0;
				while (false == state.Token.IsCancellationRequested)
				{
					Volatile.Write(ref state.ProcessorId, Thread.GetCurrentProcessorId());
					
					var hasRanBatch = !state.Batches.IsEmpty;
					while (state.Batches.TryTake(out var queued))
					{
						queued.Batch.Execute(queued.Index, queued.MaxUseIndex, state.TaskIndex, state.TaskCount);
						Interlocked.Increment(ref state.Results[queued.BatchId].SuccessfulWrite);
					}

					if (state.IsPerformanceCritical)
					{
						if (sleepCount++ > sleep0Threshold)
						{
							Thread.Sleep(0);
							sleepCount = 0;
						}

						continue;
					}
					
					spin.SpinOnce(30);
					if (hasRanBatch)
						spin.Reset();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		private CancellationTokenSource ccs;

		public ThreadBatchRunner(float corePercentile)
		{
			var coreCount = Math.Clamp((int) (Environment.ProcessorCount * corePercentile), 1, Environment.ProcessorCount);
			
			ccs           = new CancellationTokenSource();
			tasks         = new Task[coreCount];
			states        = new TaskState[coreCount];
			taskBatches   = new ConcurrentBag<QueuedBatch>[coreCount];
			batchResults  = new BatchResult[MaxRunningBatches];
			batchVersions = new int[MaxRunningBatches];
			var ts = tasks.AsSpan();
			for (var index = 0; index < ts.Length; index++)
			{
				ts[index] = new Task(runTask, states[index] = new TaskState
				{
					Token     = ccs.Token,
					TaskIndex = index,
					TaskCount = ts.Length,
					Batches   = taskBatches[index] = new(),
					Results   = batchResults
				}, TaskCreationOptions.LongRunning);
				ts[index].Start();
			}
		}

		public void Dispose()
		{
			ccs.Dispose();
		}

		public bool IsCompleted()
		{
			foreach (var batches in taskBatches)
				if (batches.IsEmpty == false)
					return false;

			return true;
		}

		public bool IsCompleted(BatchRequest request)
		{
			return batchVersions[request.Id] > request.Version || batchResults[request.Id].IsCompleted;
		}

		public BatchRequest Queue(IBatch batch)
		{
			var use = batch.PrepareBatch(tasks.Length);
			if (use <= 0)
				return default;

			var batchNumber = -1;

			for (var i = 0; i < MaxRunningBatches; i++)
			{
				if (!batchResults[i].IsCompleted)
					continue;

				batchNumber = i;
				break;
			}

			if (batchNumber < 0)
				throw new InvalidOperationException("Couldn't assign an ID to the batch. Too busy");
			
			var batchVersion = batchVersions[batchNumber]++ + 1;

			batchResults[batchNumber] = new BatchResult
			{
				SuccessfulWrite = -1,
				MaxIndex        = use - 1
			};

			var sortByPerformanceCritical = IsPerformanceCritical && ThreadInCriticalContext > 0 ? tasks.Length : 0;
			for (var i = 0; i < use; i++)
			{
				var taskIndex = i % tasks.Length;
				if (sortByPerformanceCritical > 0)
				{
					var beginning = taskIndex;
					while (states[taskIndex] is {IsPerformanceCritical: false})
					{
						taskIndex++;
						if (taskIndex >= tasks.Length)
							taskIndex = 0;
						if (taskIndex == beginning)
							break;
					}

					sortByPerformanceCritical--;
				}

				taskBatches[taskIndex].Add(new QueuedBatch
				{
					BatchId     = batchNumber,
					Batch       = batch,
					Index       = i,
					MaxUseIndex = use - 1
				});
			}

			return new BatchRequest(batchNumber, batchVersion);
		}

		public bool IsPerformanceCritical   { get; private set; }
		public int  ThreadInCriticalContext { get; private set; }

		/// <summary>
		/// Asks threads (which are not on the same processor of the caller) to enter a performance critical context.
		/// There will be no spinning, Thread.Sleep(1) or Thread.Yield() while this phase is active.
		/// </summary>
		/// <remarks>
		///	A Start should be followed by a Stop, as fast as possible.
		/// </remarks>
		public void StartPerformanceCriticalSection()
		{
			IsPerformanceCritical   = true;
			ThreadInCriticalContext = 0;
			
			var currentProcessor = Thread.GetCurrentProcessorId();
			foreach (var state in states)
			{
				var processorId = Volatile.Read(ref state.ProcessorId);

				// this is needed in case there is a task on the same processor as the caller of this runner.
				// or else everything would block
				state.IsPerformanceCritical = processorId != default && processorId != currentProcessor;
				if (state.IsPerformanceCritical)
					ThreadInCriticalContext++;
			}
		}

		public void StopPerformanceCriticalSection()
		{
			IsPerformanceCritical   = false;
			ThreadInCriticalContext = 0;
			
			foreach (var state in states)
			{
				state.IsPerformanceCritical = false;
			}
		}
	}
}