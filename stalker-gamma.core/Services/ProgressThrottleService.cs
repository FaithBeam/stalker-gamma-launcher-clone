using stalker_gamma.core.Models;

namespace stalker_gamma.core.Services;

public class ProgressThrottleService(GlobalSettings gs)
{
    public TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(gs.ProgressUpdateIntervalMs);

    public Action<T> Throttle<T>(Action<T> action, TimeSpan? interval = null)
    {
        interval ??= Interval;
        var lastTrigger = DateTime.MinValue;
        return val =>
        {
            var now = DateTime.UtcNow;
            if (now - lastTrigger >= interval || val is >= 1.0)
            {
                lastTrigger = now;
                action(val);
            }
        };
    }
}
