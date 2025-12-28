namespace Stalker.Gamma.GammaInstallerServices;

public interface IGammaProgress
{
    event EventHandler<GammaProgress.GammaInstallProgressEventArgs>? ProgressChanged;
}

public class GammaProgress : IGammaProgress
{
    private int _completedMods;

    internal int TotalMods { get; set; }

    internal void IncrementCompletedMods() => Interlocked.Increment(ref _completedMods);

    internal void Reset()
    {
        _completedMods = 0;
        TotalMods = 0;
    }

    public event EventHandler<GammaInstallProgressEventArgs>? ProgressChanged;

    internal void OnProgressChanged(GammaInstallProgressEventArgs e)
    {
        e.Complete = _completedMods;
        e.Total = TotalMods;
        ProgressChanged?.Invoke(this, e);
    }

    public class GammaInstallProgressEventArgs(string name, string progressType, double progress)
        : EventArgs
    {
        public string Name { get; } = name;
        public string ProgressType { get; } = progressType;
        public double Progress { get; } = progress;
        public int Complete { get; set; }
        public int Total { get; set; }
    }
}
