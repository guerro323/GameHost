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
		int  GetBatchCount(int taskCount);
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

	public class ThreadBatchRunner : IBatchRunner, IDisposable
	{
		// 512 is a good value since it we run very short tasks
		public const int MaxRunningBatches = 512;

		private class TaskState
		{
			public CancellationToken Token;
			public int               TaskIndex;
			public int               TaskCount;

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
		private ConcurrentBag<QueuedBatch>[] taskBatches;
		private BatchResult[]                batchResults;

		private int[] batchVersions;

		private static void runTask(object obj)
		{
			if (obj is not TaskState state)
				throw new InvalidOperationException($"Task {obj} is not a {typeof(TaskState)}");

			while (false == state.Token.IsCancellationRequested)
			{
				var hasRanBatch = !state.Batches.IsEmpty;
				while (state.Batches.TryTake(out var queued))
				{
					queued.Batch.Execute(queued.Index, queued.MaxUseIndex, state.TaskIndex, state.TaskCount);
					Interlocked.Increment(ref state.Results[queued.BatchId].SuccessfulWrite);
				}

				Thread.Sleep(0);
			}
		}

		private CancellationTokenSource ccs;

		public ThreadBatchRunner(float corePercentile)
		{
			var coreCount = Math.Clamp((int) (Environment.ProcessorCount / corePercentile), 1, Environment.ProcessorCount);
			
			ccs           = new CancellationTokenSource();
			tasks         = new Task[coreCount];
			taskBatches   = new ConcurrentBag<QueuedBatch>[coreCount];
			batchResults  = new BatchResult[MaxRunningBatches];
			batchVersions = new int[MaxRunningBatches];
			var ts = tasks.AsSpan();
			for (var index = 0; index < ts.Length; index++)
			{
				ts[index] = new Task(runTask, new TaskState
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
			var use = batch.GetBatchCount(tasks.Length);
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
			
			for (var i = 0; i < use; i++)
			{
				var taskIndex = i % tasks.Length;
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

		public Task QueueAsync(IBatch batch)
		{
			return Task.CompletedTask;
		}
	}
}