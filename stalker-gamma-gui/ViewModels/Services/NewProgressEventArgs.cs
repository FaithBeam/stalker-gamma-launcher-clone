using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace stalker_gamma_gui.Services;

public class NewProgressEventArgs(double? progress = null)
{
    public double? Progress { get; } = progress;
}

public class ProgressService
{
    private readonly Subject<NewProgressEventArgs> _progress = new();
    public IObservable<NewProgressEventArgs> ProgressObservable => _progress.AsObservable();

    public void UpdateProgress(double progress) =>
        _progress.OnNext(new NewProgressEventArgs(progress));
}
