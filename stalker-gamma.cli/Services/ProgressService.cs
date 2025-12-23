namespace stalker_gamma.cli.Services;

public class ProgressService
{
    private int _completedAddons;
    public int TotalAddons { get; set; } = 1;
    public int CompletedAddons => _completedAddons;
    public double TotalProgress => _completedAddons / (double)TotalAddons;

    public void IncrementCompleted() => Interlocked.Increment(ref _completedAddons);
}
