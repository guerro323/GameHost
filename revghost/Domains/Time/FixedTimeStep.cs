using System;

namespace revghost.Domains.Time;

public struct FixedTimeStep
{
    private double _accumulatedTime;
    private int _targetFrameTimeMs;

    public void Reset()
    {
        _accumulatedTime = 0;
    }

    public FixedTimeStep(TimeSpan timeSpan)
    {
        _accumulatedTime = 0;
        _targetFrameTimeMs = (int) timeSpan.TotalMilliseconds;
    }

    public void SetTargetFrameTime(TimeSpan timeSpan)
    {
        _targetFrameTimeMs = (int) timeSpan.TotalMilliseconds;
    }

    public int GetUpdateCount(double deltaTime)
    {
        if (deltaTime < 0.0001)
            return 0;

        var targetFrameTime = _targetFrameTimeMs * 0.001f;

        _accumulatedTime += deltaTime;

        var updateCount = (int) (_accumulatedTime / targetFrameTime);
        _accumulatedTime -= updateCount * targetFrameTime;

        return updateCount;
    }
}