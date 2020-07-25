using System;

namespace GameHost.Simulation
{
	public struct FixedTimeStep
	{
		private double accumulatedTime;

		public int TargetFrameTimeMs;

		public void Reset()
		{
			accumulatedTime = 0;
		}

		public int GetUpdateCount(double deltaTime)
		{
			if (deltaTime < 0.0001)
				return 0;

			//Console.WriteLine(TargetFrameTimeMs);
			
			var targetFrameTime = TargetFrameTimeMs * 0.001f;
            
			accumulatedTime += deltaTime;

			var updateCount = (int)(accumulatedTime / targetFrameTime);
			accumulatedTime -= updateCount * targetFrameTime;

			return updateCount;
		}
	}
}