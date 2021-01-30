using System;
using System.Threading;
using NUnit.Framework;
using StormiumTeam.GameBase.Utility.Misc.EntitySystem;

namespace GameHost.Simulation.Tests
{
	public class TestBatch
	{
		[Test]
		public void TestOne()
		{
			var runner = new ThreadBatchRunner(0.5f);

			var size         = 64;
			var batchObjects = new __BatchTest[size * 4];
			var batchIds     = new BatchRequest[size * 4];
			for (var i = 0; i < batchIds.Length; i += 4)
			{
				batchIds[i]     = runner.Queue(batchObjects[i] = new __BatchTest(size, 1));
				batchIds[i + 1] = runner.Queue(batchObjects[i + 1] = new __BatchTest(size, 2));
				batchIds[i + 2] = runner.Queue(batchObjects[i + 2] = new __BatchTest(size, 3));
				batchIds[i + 3] = runner.Queue(batchObjects[i + 3] = new __BatchTest(size, 4));
			}

			foreach (var id in batchIds)
			{
				while (!runner.IsCompleted(id))
				{
					Thread.Sleep(1);
				}
			}
			
			Assert.IsTrue(runner.IsCompleted());

			foreach (var batch in batchObjects)
			{
				Assert.AreEqual(batch.Size, batch.ComputedValue);
			}

			Console.WriteLine("Runner Completed");
			runner.Dispose();
		}

		public class __BatchTest : IBatch
		{
			public readonly int Size;
			public readonly int ToProcess;

			public int ComputedValue;

			public __BatchTest(int size, int toProcess)
			{
				Size      = size;
				ToProcess = toProcess;
			}

			public int PrepareBatch(int count)
			{
				ComputedValue = 0;
				return Size == 0 ? 0 : Math.Max((int) Math.Ceiling((float) Size / ToProcess), 1);
			}

			public void Execute(int index, int maxIndex, int taskIndex, int taskCount)
			{
				int batchSize;
				if (index == maxIndex)
				{
					var r = ToProcess * index;
					batchSize = Size - r;
				}
				else
					batchSize = ToProcess;
				
				Interlocked.Add(ref ComputedValue, batchSize);
			}
		}
	}
}