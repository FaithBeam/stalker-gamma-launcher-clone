using System.Reactive.Disposables;
using System.Reactive.Linq;
using ReactiveUI;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public class ModDownloadExtractProgressVm : ReactiveObject, IActivatableViewModel, IDisposable
{
    public string AddonName { get; }

    private Status _status;
    public Status Status
    {
        get => _status;
        set => this.RaiseAndSetIfChanged(ref _status, value);
    }

    private Progress<double> DownloadProgress { get; }
    public IProgress<double> DownloadProgressInterface => DownloadProgress;

    private Progress<double> ExtractProgress { get; }
    public IProgress<double> ExtractProgressInterface => ExtractProgress;

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

        DownloadProgress = new Progress<double>();
        var dlProgChanged = Observable.FromEventPattern<EventHandler<double>, double>(
            handler => DownloadProgress.ProgressChanged += handler,
            handler => DownloadProgress.ProgressChanged -= handler
        );

        ExtractProgress = new Progress<double>();
        var extractProgChanged = Observable.FromEventPattern<EventHandler<double>, double>(
            handler => ExtractProgress.ProgressChanged += handler,
            handler => ExtractProgress.ProgressChanged -= handler
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                _setCompleteObservable = this.WhenAnyValue(
                        x => x.Status,
                        selector: status => status
                    )
                    .Subscribe(onNext =>
                    {
                        switch (onNext)
                        {
                            case Status.Queued:
                                break;
                            case Status.CheckingMd5:
                                break;
                            case Status.Downloading:
                                break;
                            case Status.Downloaded:
                                DownloadProgressInterface.Report(100);
                                break;
                            case Status.Extracting:
                                break;
                            case Status.ExtractAtEnd:
                                break;
                            case Status.Done:
                                DownloadProgressInterface.Report(100);
                                ExtractProgressInterface.Report(100);
                                break;
                            case Status.Warning:
                                break;
                            case Status.Error:
                                break;
                            case Status.Cancelled:
                                break;
                            case Status.Retry:
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(onNext), onNext, null);
                        }
                    })
                    .DisposeWith(d);

                _overallProgressValue = dlProgChanged
                    .Select(x => x.EventArgs)
                    .StartWith(0.0)
                    .CombineLatest(
                        extractProgChanged.Select(extract => extract.EventArgs).StartWith(0.0),
                        (download, extract) => (download + extract) / 2.0
                    )
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
