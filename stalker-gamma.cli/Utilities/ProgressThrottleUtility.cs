namespace stalker_gamma.cli.Utilities;

public static class ProgressThrottleUtility
{
    private static readonly TimeSpan _interval = TimeSpan.FromSeconds(1);

    public static Action<T> Throttle<T>(Action<T> action, TimeSpan? interval = null)
    {
        interval ??= _interval;
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
