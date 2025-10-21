using System.ComponentModel;
using System.Diagnostics;
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
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Models;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public interface IMainTabVm
{
    bool NeedUpdate { get; set; }
    bool NeedModDbUpdate { get; set; }
    bool InGrokModDir { get; set; }
    string GammaVersionToolTip { get; set; }
    string ModVersionToolTip { get; set; }
    IsBusyService IsBusyService { get; }
    Interaction<string, Unit> AppendLineInteraction { get; }
    double Progress { get; }
    bool CheckMd5 { get; set; }
    bool PreserveUserLtx { get; set; }
    bool ForceGitDownload { get; set; }
    bool ForceZipExtraction { get; set; }
    bool DeleteReshadeDlls { get; set; }
    string VersionString { get; set; }
    bool IsRanWithWine { get; }
    ReactiveCommand<Unit, bool> IsRanWithWineCmd { get; set; }
    ReactiveCommand<Unit, string> AddFoldersToWinDefenderExclusionCmd { get; set; }
    CombinedReactiveCommand<Unit, Unit> FirstInstallInitialization { get; }
    ReactiveCommand<Unit, Unit> InstallUpdateGamma { get; }
    ReactiveCommand<Unit, Unit> Play { get; }
    ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    ReactiveCommand<Unit, Unit> DowngradeModOrganizerCmd { get; }
    ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    ReactiveCommand<Unit, Unit> InGroksModPackDir { get; }
    ViewModelActivator Activator { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changing { get; }
    IObservable<IReactivePropertyChangedEventArgs<IReactiveObject>> Changed { get; }
    IObservable<Exception> ThrownExceptions { get; }
    IDisposable SuppressChangeNotifications();
    bool AreChangeNotificationsEnabled();
    IDisposable DelayChangeNotifications();
    event PropertyChangingEventHandler? PropertyChanging;
    event PropertyChangedEventHandler? PropertyChanged;
}

public class MainTabVm : ViewModelBase, IActivatableViewModel, IMainTabVm
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
    private string _gammaVersionsToolTip = "";
    private string _modsVersionsToolTip = "";
    private readonly ObservableAsPropertyHelper<bool> _toolsReady;
    private readonly ObservableAsPropertyHelper<bool> _isRanWithWine;

    public MainTabVm(
        IIsRanWithWineService isRanWithWineService,
        EnableLongPathsOnWindows.Handler enableLongPathsOnWindows,
        AddFoldersToWinDefenderExclusion.Handler addFoldersToWinDefenderExclusion,
        GetAnomalyPath.Handler getAnomalyPathHandler,
        GetGammaPath.Handler getGammaPathHandler,
        GetGammaBackupFolder.Handler getGammaBackupFolderHandler,
        CurlService curlService,
        GammaInstaller gammaInstaller,
        ProgressService progressService,
        GlobalSettings globalSettings,
        DowngradeModOrganizer downgradeModOrganizer,
        VersionService versionService,
        IsBusyService isBusyService,
        DiffMods.Handler diffMods,
        GetStalkerGammaLastCommit.Handler getStalkerGammaLastCommit,
        GetGitHubRepoCommits.Handler getGitHubRepoCommits
    )
    {
        Activator = new ViewModelActivator();
        IsBusyService = isBusyService;
        _versionString = $"{versionService.GetVersion()} (Based on 6.7.0.0)";

        IsRanWithWineCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(isRanWithWineService.IsRanWithWine)
        );
        IsRanWithWineCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR DETERMINING IF RAN WITH WINE
                {x}
                """
            )
        );
        _isRanWithWine = IsRanWithWineCmd.ToProperty(this, x => x.IsRanWithWine);

        ToolsReadyCommand = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() => new ToolsReadyRecord(curlService.Ready))
        );
        _toolsReady = ToolsReadyCommand
            .Select(x => x.CurlReady)
            .ToProperty(this, x => x.ToolsReady);
        ToolsReadyCommand
            .Where(x => !x.CurlReady)
            .Subscribe(x =>
            {
                List<string> notRdy = [];
                if (!x.CurlReady)
                {
                    notRdy.Add("Curl not found");
                }
                var notRdyTools = string.Join("\n", notRdy);
                progressService.UpdateProgress(
                    $"""

                    TOOLS NOT READY
                    {notRdyTools}

                    Did you place the executable in the correct directory? .Grok's Modpack Installer
                    """
                );
            });

        OpenUrlCmd = ReactiveCommand.Create<string>(OpenUrlUtility.OpenUrl);

        EnableLongPathsOnWindowsCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
            {
                if (IsRanWithWine)
                {
                    return;
                }
                enableLongPathsOnWindows.Execute();
                progressService.UpdateProgress("Enabled long paths via registry");
            })
        );

        var canAddFoldersToWinDefenderExclusion = this.WhenAnyValue(
            x => x.IsRanWithWine,
            selector: ranWithWine => !ranWithWine
        );
        AddFoldersToWinDefenderExclusionCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var anomalyPath = getAnomalyPathHandler.Execute()!.Replace(@"\\", "\\");
                var gammaPath = getGammaPathHandler.Execute();
                var gammaBackupPath = getGammaBackupFolderHandler.Execute();
                await Task.Run(() =>
                    addFoldersToWinDefenderExclusion.Execute(
                        new AddFoldersToWinDefenderExclusion.Command(
                            anomalyPath,
                            gammaPath,
                            gammaBackupPath
                        )
                    )
                );
                return $"""

                Added folder exclusions to Microsoft Defender for:
                {anomalyPath}
                {gammaPath}
                {gammaBackupPath}
                """;
            },
            canAddFoldersToWinDefenderExclusion
        );
        AddFoldersToWinDefenderExclusionCmd.Subscribe(progressService.UpdateProgress);
        AddFoldersToWinDefenderExclusionCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                User either denied UAC prompt or there was an error.
                """
            )
        );

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
        FirstInstallInitialization = ReactiveCommand.CreateCombined(
            [
                EnableLongPathsOnWindowsCmd,
                ReactiveCommand.CreateFromTask(
                    async () =>
                    {
                        IsBusyService.IsBusy = true;
                        await gammaInstaller.FirstInstallInitialization();
                        IsBusyService.IsBusy = false;
                    },
                    canFirstInstallInitialization
                ),
            ]
        );
        FirstInstallInitialization.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                Error in first install initialization:
                {x}
                """
            )
        );

        BackgroundCheckUpdatesCmd = ReactiveCommand.CreateFromTask(() =>
            Task.Run(async () =>
            {
                var needUpdates = await gammaInstaller.CheckGammaData(
                    globalSettings.UseCurlImpersonate
                );
                var remoteGammaVersionHash = (
                    await getGitHubRepoCommits.ExecuteAsync(
                        new GetGitHubRepoCommits.Query("Grokitach", "Stalker_GAMMA")
                    )
                )
                    ?.FirstOrDefault()
                    ?[..9];
                var localGammaVersionHash = (
                    await getStalkerGammaLastCommit.ExecuteAsync(
                        new GetStalkerGammaLastCommit.Query(
                            Path.Join(_dir, "resources", "Stalker_GAMMA")
                        )
                    )
                )[..9];
                GammaVersionToolTip = $"""
                Remote Version: {needUpdates.gammaVersions.RemoteVersion} ({remoteGammaVersionHash})
                Local Version: {needUpdates.gammaVersions.LocalVersion} ({localGammaVersionHash})
                """;
                ModVersionToolTip = string.Join(
                    Environment.NewLine,
                    await diffMods.Execute(new Queries.DiffMods.Query(needUpdates.modVersions))
                );
                NeedUpdate =
                    needUpdates.gammaVersions.LocalVersion
                        != needUpdates.gammaVersions.RemoteVersion
                    || localGammaVersionHash != remoteGammaVersionHash;
                NeedModDbUpdate =
                    needUpdates.modVersions.LocalVersion != needUpdates.modVersions.RemoteVersion;
            })
        );
        BackgroundCheckUpdatesCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR CHECKING FOR UPDATES
                {x.Message}
                {x.StackTrace}
                """
            )
        );

        var canInstallUpdateGamma = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            x => x.ToolsReady,
            selector: (isBusy, inGrokModDir, toolsReady) => !isBusy && inGrokModDir && toolsReady
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
                InGrokModDir = _dir.Contains(
#if DEBUG
                    "net9.0",
#else
                    ".Grok's Modpack Installer",
#endif
                    StringComparison.OrdinalIgnoreCase);
                InGroksModPackDir.Execute().Subscribe();

                IsRanWithWineCmd.Execute().Subscribe();

                ToolsReadyCommand
                    .Execute()
                    .Where(x => x.CurlReady)
                    .Subscribe(_ =>
                    {
                        BackgroundCheckUpdatesCmd.Execute().Subscribe();
                    });
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

    private bool ToolsReady => _toolsReady.Value;

    public bool InGrokModDir
    {
        get => _inGrokModDir;
        set => this.RaiseAndSetIfChanged(ref _inGrokModDir, value);
    }

    public string GammaVersionToolTip
    {
        get => _gammaVersionsToolTip;
        set => this.RaiseAndSetIfChanged(ref _gammaVersionsToolTip, value);
    }

    public string ModVersionToolTip
    {
        get => _modsVersionsToolTip;
        set => this.RaiseAndSetIfChanged(ref _modsVersionsToolTip, value);
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

    public bool IsRanWithWine => _isRanWithWine.Value;

    public ReactiveCommand<Unit, bool> IsRanWithWineCmd { get; set; }
    public ReactiveCommand<Unit, Unit> EnableLongPathsOnWindowsCmd { get; set; }
    public ReactiveCommand<Unit, string> AddFoldersToWinDefenderExclusionCmd { get; set; }
    private ReactiveCommand<Unit, ToolsReadyRecord> ToolsReadyCommand { get; }
    public CombinedReactiveCommand<Unit, Unit> FirstInstallInitialization { get; }
    public ReactiveCommand<Unit, Unit> InstallUpdateGamma { get; }
    public ReactiveCommand<Unit, Unit> Play { get; }
    public ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    public ReactiveCommand<Unit, Unit> DowngradeModOrganizerCmd { get; }
    public ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    public ReactiveCommand<Unit, Unit> InGroksModPackDir { get; }
    public ViewModelActivator Activator { get; }
}
