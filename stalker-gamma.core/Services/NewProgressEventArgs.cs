using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace stalker_gamma.core.Services;

public class NewProgressEventArgs(double? progress = null, string? message = null)
{
    public double? Progress { get; } = progress;
    public string? Message { get; } = message;
}

public class ProgressService
{
    private readonly Subject<NewProgressEventArgs> _progress = new();
    public IObservable<NewProgressEventArgs> ProgressObservable => _progress.AsObservable();

    public void UpdateProgress(double progress, string message) =>
        _progress.OnNext(new NewProgressEventArgs(progress, message));

    public void UpdateProgress(double progress) =>
        _progress.OnNext(new NewProgressEventArgs(progress));

    public void UpdateProgress(string message) =>
        _progress.OnNext(new NewProgressEventArgs(message: message));
}
