namespace GameHost.Core.Game
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
            var targetFrameTime = TargetFrameTimeMs * 0.001f;
            
            accumulatedTime += deltaTime;

            var updateCount = (int)(accumulatedTime / targetFrameTime);
            accumulatedTime -= updateCount * targetFrameTime;

            return updateCount;
        }
    }
}
