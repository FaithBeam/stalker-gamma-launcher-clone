namespace stalker_gamma.core.Utilities;

public static class ActionUtils
{
    public static TimeSpan Interval { get; set; } = TimeSpan.FromMilliseconds(500);

    public static Action<T> Debounce<T>(Action<T> action, TimeSpan? interval = null)
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
