using Collections.Pooled;
using GameHost.Simulation.TabEcs;
using NUnit.Framework;

namespace GameHost.Simulation.Tests
{
	public class TestBuffer
	{
		public struct LongValue
		{
			public byte GarbageOffset;
			public long Value;

			public static implicit operator LongValue(long l) => new() {Value = l};
		}
		
		[Test]
		public void TestPredicateContains()
		{
			var list   = new PooledList<byte>();
			var buffer = new ComponentBuffer<LongValue>(list);
			buffer.AddRange(new LongValue[] {4, 16, 192, 36, 5});
			
			Assert.IsTrue(buffer.Contains((ref LongValue i) => ref i.Value, 4), "buffer.Contains((ref LongValue i) => ref i.Value, 4)");
			Assert.IsTrue(buffer.Contains((ref LongValue i) => ref i.Value, 16), "buffer.Contains((ref LongValue i) => ref i.Value, 16)");
			Assert.IsTrue(buffer.Contains((ref LongValue i) => ref i.Value, 192), "buffer.Contains((ref LongValue i) => ref i.Value, 192)");
			Assert.IsTrue(buffer.Contains((ref LongValue i) => ref i.Value, 36), "buffer.Contains((ref LongValue i) => ref i.Value, 36)");
			Assert.IsTrue(buffer.Contains((ref LongValue i) => ref i.Value, 5), "buffer.Contains((ref LongValue i) => ref i.Value, 5)");
		}
	}
}