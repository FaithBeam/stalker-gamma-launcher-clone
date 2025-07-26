using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using AsyncAwaitBestPractices;
using CliWrap;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs;

public class MainTabVm : ViewModelBase
{
    private bool _checkMd5 = true;
    private bool _forceGitDownload = true;
    private bool _forceZipExtraction = true;
    private bool _deleteReshadeDlls = true;
    private readonly ObservableAsPropertyHelper<double?> _progress;
    private bool _isBusy;
    private bool _needUpdate;
    private bool _needModDbUpdate;
    private readonly GammaInstaller _gammaInstaller;
    private string _versionString;

    public MainTabVm(
        GammaInstaller gammaInstaller,
        ProgressService progressService,
        GlobalSettings globalSettings,
        DowngradeModOrganizer downgradeModOrganizer,
        VersionService versionService
    )
    {
        // Activator = new ViewModelActivator();
        _gammaInstaller = gammaInstaller;
        _versionString = $"{versionService.GetVersion()} (Based on 6.7.0.0)";

        OpenUrlCmd = ReactiveCommand.Create<string>(OpenUrlUtility.OpenUrl);

        var canFirstInstallInitialization = this.WhenAnyValue(
            x => x.IsBusy,
            selector: isBusy => !isBusy
        );
        FirstInstallInitialization = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusy = true;
                await gammaInstaller.FirstInstallInitialization();
                IsBusy = false;
            },
            canFirstInstallInitialization
        );

        var canInstallUpdateGamma = this.WhenAnyValue(x => x.IsBusy, selector: isBusy => !isBusy);
        InstallUpdateGamma = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusy = true;
                await Task.Run(() =>
                    gammaInstaller.InstallUpdateGammaAsync(
                        ForceGitDownload,
                        CheckMd5,
                        true,
                        ForceZipExtraction,
                        DeleteReshadeDlls,
                        globalSettings.UseCurlImpersonate
                    )
                );
                await CheckUpdates();
                IsBusy = false;
            },
            canInstallUpdateGamma
        );
        InstallUpdateGamma.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress($"{x.Message}\n{x.StackTrace}")
        );

        var mo2Path = Path.Join(
            Path.GetDirectoryName(AppContext.BaseDirectory),
            "..",
            "ModOrganizer.exe"
        );

        var canPlay = this.WhenAnyValue(
            x => x.IsBusy,
            selector: isBusy => !isBusy && File.Exists(mo2Path)
        );
        Play = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusy = true;
                await Cli.Wrap(mo2Path).ExecuteAsync();
                IsBusy = false;
            },
            canPlay
        );

        var canDowngradeModOrganizer = this.WhenAnyValue(
            x => x.IsBusy,
            selector: isBusy =>
                !isBusy
                && (
                    (
                        File.Exists(mo2Path)
                        && FileVersionInfo.GetVersionInfo(mo2Path).FileVersion != "2.4.4"
                    ) || !File.Exists(mo2Path)
                )
        );
        DowngradeModOrganizerCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusy = true;
                await Task.Run(() => downgradeModOrganizer.DowngradeAsync());
                IsBusy = false;
            },
            canDowngradeModOrganizer
        );
        DowngradeModOrganizerCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(x.Message)
        );

        AppendLineInteraction = new Interaction<string, Unit>();

        _progress = progressService
            .ProgressObservable.ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x.Progress)
            .WhereNotNull()
            .ToProperty(this, x => x.Progress);
        var progressServiceDisposable = progressService
            .ProgressObservable.ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x.Message)
            .WhereNotNull()
            .Subscribe(async x => await AppendLineInteraction.Handle(x));
        // this.WhenActivated(d =>
        // {
        //
        //     progressServiceDisposable.DisposeWith(d);
        // });

        CheckUpdates().SafeFireAndForget();
    }

    private async Task CheckUpdates()
    {
        var needUpdates = await _gammaInstaller.CheckGammaData(true);
        NeedUpdate = needUpdates.NeedUpdate;
        NeedModDbUpdate = needUpdates.NeedModDBUpdate;
    }

    public bool NeedUpdate
    {
        get => _needUpdate;
        set => this.RaiseAndSetIfChanged(ref _needUpdate, value);
    }

    public bool NeedModDbUpdate
    {
        get => _needModDbUpdate;
        set => this.RaiseAndSetIfChanged(ref _needModDbUpdate, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
    }

    public Interaction<string, Unit> AppendLineInteraction { get; }

    public double Progress => _progress.Value ?? 0;

    public bool CheckMd5
    {
        get => _checkMd5;
        set => this.RaiseAndSetIfChanged(ref _checkMd5, value);
    }

    public bool ForceGitDownload
    {
        get => _forceGitDownload;
        set => this.RaiseAndSetIfChanged(ref _forceGitDownload, value);
    }

    public bool ForceZipExtraction
    {
        get => _forceZipExtraction;
        set => this.RaiseAndSetIfChanged(ref _forceZipExtraction, value);
    }

    public bool DeleteReshadeDlls
    {
        get => _deleteReshadeDlls;
        set => this.RaiseAndSetIfChanged(ref _deleteReshadeDlls, value);
    }

    public string VersionString
    {
        get => _versionString;
        set => this.RaiseAndSetIfChanged(ref _versionString, value);
    }

    public ReactiveCommand<Unit, Unit> FirstInstallInitialization { get; }
    public ReactiveCommand<Unit, Unit> InstallUpdateGamma { get; }
    public ReactiveCommand<Unit, Unit> Play { get; }
    public ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    public ReactiveCommand<Unit, Unit> DowngradeModOrganizerCmd { get; }
    // public ViewModelActivator Activator { get; }
}
