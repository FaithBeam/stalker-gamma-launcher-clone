﻿using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CliWrap;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs;

public class MainTabVm : ViewModelBase, IActivatableViewModel
{
    private bool _checkMd5 = true;
    private bool _forceGitDownload = true;
    private bool _forceZipExtraction = true;
    private bool _deleteReshadeDlls = true;
    private bool _inGrokModDir;
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private bool _preserveUserLtx;
    private readonly ObservableAsPropertyHelper<double?> _progress;
    private bool _needUpdate;
    private bool _needModDbUpdate;
    private string _versionString;

    public MainTabVm(
        GammaInstaller gammaInstaller,
        ProgressService progressService,
        GlobalSettings globalSettings,
        DowngradeModOrganizer downgradeModOrganizer,
        VersionService versionService,
        IsBusyService isBusyService
    )
    {
        Activator = new ViewModelActivator();
        IsBusyService = isBusyService;
        var gammaInstaller1 = gammaInstaller;
        var globalSettings1 = globalSettings;
        _versionString = $"{versionService.GetVersion()} (Based on 6.7.0.0)";

        OpenUrlCmd = ReactiveCommand.Create<string>(OpenUrlUtility.OpenUrl);

        var mo2Path = Path.Join(
            Path.GetDirectoryName(AppContext.BaseDirectory),
            "..",
            "ModOrganizer.exe"
        );

        var canFirstInstallInitialization = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            selector: (isBusy, inGrokModDir) => !isBusy && File.Exists(mo2Path) && inGrokModDir
        );
        FirstInstallInitialization = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await gammaInstaller.FirstInstallInitialization();
                IsBusyService.IsBusy = false;
            },
            canFirstInstallInitialization
        );
        FirstInstallInitialization.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                Error in first install initialization:
                {x}
                """
            )
        );

        BackgroundCheckUpdatesCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var needUpdates = await gammaInstaller1.CheckGammaData(
                globalSettings1.UseCurlImpersonate
            );
            NeedUpdate = needUpdates.NeedUpdate;
            NeedModDbUpdate = needUpdates.NeedModDBUpdate;
        });
        BackgroundCheckUpdatesCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(x.Message)
        );

        var canInstallUpdateGamma = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            selector: (isBusy, inGrokModDir) => !isBusy && inGrokModDir
        );
        InstallUpdateGamma = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await Task.Run(() =>
                    gammaInstaller.InstallUpdateGammaAsync(
                        ForceGitDownload,
                        CheckMd5,
                        true,
                        ForceZipExtraction,
                        DeleteReshadeDlls,
                        globalSettings.UseCurlImpersonate,
                        PreserveUserLtx
                    )
                );
                BackgroundCheckUpdatesCmd.Execute().Subscribe();
                IsBusyService.IsBusy = false;
            },
            canInstallUpdateGamma
        );
        InstallUpdateGamma.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                ERROR INSTALLING/UPDATING GAMMA:
                {x.Message}
                {x.StackTrace}
                """
            )
        );

        var canPlay = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            selector: (isBusy, inGrokModDir) => !isBusy && File.Exists(mo2Path) && inGrokModDir
        );
        Play = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await Cli.Wrap(mo2Path).ExecuteAsync();
                IsBusyService.IsBusy = false;
            },
            canPlay
        );
        Play.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                ERROR PLAYING:
                {x}
                """
            )
        );

        var canDowngradeModOrganizer = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            selector: (isBusy, inGrokModDir) =>
                !isBusy
                && (
                    (
                        File.Exists(mo2Path)
                        && FileVersionInfo.GetVersionInfo(mo2Path).FileVersion != "2.4.4"
                    ) || !File.Exists(mo2Path)
                )
                && inGrokModDir
        );
        DowngradeModOrganizerCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await Task.Run(() => downgradeModOrganizer.DowngradeAsync());
                IsBusyService.IsBusy = false;
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

        InGroksModPackDir = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
            {
                if (!InGrokModDir)
                {
                    progressService.UpdateProgress(
                        """
                        ERROR: This launcher is not put in the correct directory.
                        It needs to be in the .Grok's Modpack Installer directory which is from GAMMA RC3 archive in the discord.
                        """
                    );
                }
            })
        );

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
#if DEBUG
                InGrokModDir = true;
#else
                InGrokModDir = _dir.Contains(
                    ".Grok's Modpack Installer",
                    StringComparison.OrdinalIgnoreCase
                );
#endif
                InGroksModPackDir.Execute().Subscribe();
                BackgroundCheckUpdatesCmd.Execute().Subscribe();
            }
        );
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

    public bool InGrokModDir
    {
        get => _inGrokModDir;
        set => this.RaiseAndSetIfChanged(ref _inGrokModDir, value);
    }

    public IsBusyService IsBusyService { get; }

    public Interaction<string, Unit> AppendLineInteraction { get; }

    public double Progress => _progress.Value ?? 0;

    public bool CheckMd5
    {
        get => _checkMd5;
        set => this.RaiseAndSetIfChanged(ref _checkMd5, value);
    }

    public bool PreserveUserLtx
    {
        get => _preserveUserLtx;
        set => this.RaiseAndSetIfChanged(ref _preserveUserLtx, value);
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
    public ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    public ReactiveCommand<Unit, Unit> InGroksModPackDir { get; }
    public ViewModelActivator Activator { get; }
}
