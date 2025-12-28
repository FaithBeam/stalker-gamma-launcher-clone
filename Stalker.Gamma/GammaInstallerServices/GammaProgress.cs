namespace Stalker.Gamma.GammaInstallerServices;

public interface IGammaProgress
{
    event EventHandler<GammaProgress.GammaInstallProgressEventArgs>? ProgressChanged;
}

public class GammaProgress : IGammaProgress
{
    private int _completedMods;

    public int TotalMods { get; set; }

    public void IncrementCompletedMods() => Interlocked.Increment(ref _completedMods);

    public void Reset()
    {
        _completedMods = 0;
        TotalMods = 0;
    }

    public event EventHandler<GammaInstallProgressEventArgs>? ProgressChanged;

    internal void OnProgressChanged(GammaInstallProgressEventArgs e)
    {
        var pct = (double)_completedMods / TotalMods;
        e.TotalProgress = pct;
        ProgressChanged?.Invoke(this, e);
    }

    public class GammaInstallProgressEventArgs(string name, string progressType, double progress)
        : EventArgs
    {
        public string Name { get; } = name;
        public string ProgressType { get; } = progressType;
        public double Progress { get; } = progress;
        public double TotalProgress { get; set; }
    }
}
