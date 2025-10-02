using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Services;

public record BackupProgressEventArgs(string? Message, double? Progress);

public class BackupTabProgressService
{
    private readonly Subject<BackupProgressEventArgs> _progress = new();
    public IObservable<BackupProgressEventArgs> BackupProgressObservable =>
        _progress.AsObservable();

    public void UpdateProgress(string? message, double? progress) =>
        _progress.OnNext(new BackupProgressEventArgs(message, progress));

    public void UpdateProgress(double progress) =>
        _progress.OnNext(new BackupProgressEventArgs(null, progress));

    public void UpdateProgress(string message) =>
        _progress.OnNext(new BackupProgressEventArgs(message, null));
}
