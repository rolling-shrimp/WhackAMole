using WhackAMole;

namespace WhackAMole.Tests;

public class ManualTimeProvider : TimeProvider
{
    private long _timestamp;

    public override long TimestampFrequency
    {
        get { return TimeSpan.TicksPerSecond; }
    }

    public override long GetTimestamp()
    {
        return _timestamp;
    }

    public void Advance(TimeSpan duration)
    {
        _timestamp += duration.Ticks;
    }
}
