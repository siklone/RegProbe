namespace OpenTraceProject.App.Services;

/// <summary>
/// Throttles UI updates to maintain 60 FPS (16.67ms frame budget).
/// Prevents update storms that cause stuttering.
/// </summary>
public class UpdateThrottler
{
    private DateTime _lastUpdate = DateTime.MinValue;
    private readonly TimeSpan _minInterval;

    public UpdateThrottler(TimeSpan minInterval)
    {
        _minInterval = minInterval;
    }

    public UpdateThrottler(int milliseconds) : this(TimeSpan.FromMilliseconds(milliseconds))
    {
    }

    /// <summary>
    /// Check if enough time has elapsed since last update.
    /// </summary>
    public bool ShouldUpdate()
    {
        var now = DateTime.UtcNow;
        if (now - _lastUpdate >= _minInterval)
        {
            _lastUpdate = now;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Force reset the throttle timer.
    /// </summary>
    public void Reset()
    {
        _lastUpdate = DateTime.MinValue;
    }

    public TimeSpan TimeSinceLastUpdate => DateTime.UtcNow - _lastUpdate;
    public TimeSpan TimeUntilNextUpdate => _minInterval - TimeSinceLastUpdate;
}
