using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma.core.Models;

namespace stalker_gamma_gui.ViewModels.Tabs.MainTab;

public class ModDownloadExtractProgressVm : ReactiveObject, IActivatableViewModel, IDisposable
{
    public string AddonName { get; }

    private Status _status;
    public Status Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    private Progress<double> Progress { get; }
    public IProgress<double> ProgressInterface => Progress;

    public double OverallProgressValue => _overallProgressValue?.Value ?? 0;

    public ModListRecord ModListRecord { get; }

    public ModDownloadExtractProgressVm(ModListRecord modListRecord)
    {
        _status = Status.Queued;
        ModListRecord = modListRecord;
        AddonName = modListRecord switch
        {
            ModpackSpecific modpackSpecific => modpackSpecific.AddonName ?? "N/A",
            GitRecord gitRecord => gitRecord.AddonName ?? "N/A",
            DownloadableRecord downloadableRecord => downloadableRecord.AddonName ?? "N/A",
            Separator separator => $"{separator.Name} Separator",
            _ => "N/A",
        };

        Progress = new Progress<double>();
        var dlProgChanged = Observable.FromEventPattern<EventHandler<double>, double>(
            handler => Progress.ProgressChanged += handler,
            handler => Progress.ProgressChanged -= handler
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                _setCompleteObservable = this.WhenAnyValue(
                        x => x.Status,
                        selector: status => status != Status.Done
                    )
                    .Subscribe(_ => ProgressInterface.Report(0))
                    .DisposeWith(d);

                _overallProgressValue = dlProgChanged
                    .Select(x => x.EventArgs)
                    .StartWith(0.0)
                    .Select(x => x * 100)
                    .ToProperty(this, x => x.OverallProgressValue)
                    .DisposeWith(d);
            }
        );
    }

    private IDisposable? _setCompleteObservable;
    private ObservableAsPropertyHelper<double>? _overallProgressValue;
    public ViewModelActivator Activator { get; } = new();

    public void Dispose()
    {
        _setCompleteObservable?.Dispose();
        _overallProgressValue?.Dispose();
        Activator.Dispose();
    }
}

public enum Status
{
    Queued,
    Checking,
    CheckingMd5,
    Downloading,
    Downloaded,
    Extracting,
    ExtractAtEnd,
    Done,
    Warning,
    Error,
    Cancelled,
    Retry,
}
