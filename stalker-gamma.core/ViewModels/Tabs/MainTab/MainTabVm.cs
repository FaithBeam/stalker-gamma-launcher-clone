using System.ComponentModel;
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
    IIsBusyService IsBusyService { get; }
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
    ReactiveCommand<Unit, Unit> EnableLongPathsOnWindowsCmd { get; set; }
    ReactiveCommand<Unit, Unit> FirstInstallInitializationCmd { get; }
    ReactiveCommand<Unit, Unit> InstallUpdateGammaCmd { get; }
    ReactiveCommand<Unit, Unit> PlayCmd { get; }
    ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    ReactiveCommand<Unit, Unit> DowngradeModOrganizerCmd { get; }
    ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    ReactiveCommand<Unit, Unit> InGroksModPackDir { get; }
    ReactiveCommand<Unit, bool?> LongPathsStatusCmd { get; }
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
    private readonly ObservableAsPropertyHelper<bool?> _longPathsStatus;
    private readonly ObservableAsPropertyHelper<bool?> _isMo2VersionDowngraded;
    private readonly ObservableAsPropertyHelper<bool> _isMo2Initialized;
    private readonly ObservableAsPropertyHelper<string?> _localGammaVersion;
    private readonly ObservableAsPropertyHelper<bool?> _userLtxSetToFullscreenWine;
    private readonly ObservableAsPropertyHelper<string?> _anomalyPath;
    private readonly ObservableAsPropertyHelper<string?> _userLtxPath;

    public MainTabVm(
        IUserLtxReplaceFullscreenWithBorderlessFullscreen userLtxReplaceFullscreenWithBorderlessFullscreen,
        IUserLtxSetToFullscreenWine userLtxSetToFullscreenWine,
        IGetLocalGammaVersion getLocalGammaVersion,
        IIsMo2Initialized isMo2Initialized,
        IIsMo2VersionDowngraded isMo2VersionDowngraded,
        IOperatingSystemService operatingSystemService,
        IILongPathsStatusService longPathsStatusHandler,
        IIsRanWithWineService isRanWithWineService,
        EnableLongPathsOnWindows.Handler enableLongPathsOnWindows,
        AddFoldersToWinDefenderExclusion.Handler addFoldersToWinDefenderExclusion,
        GetAnomalyPath.Handler getAnomalyPathHandler,
        GetGammaPath.Handler getGammaPathHandler,
        GetGammaBackupFolder.Handler getGammaBackupFolderHandler,
        ICurlService curlService,
        GammaInstaller gammaInstaller,
        ProgressService progressService,
        GlobalSettings globalSettings,
        DowngradeModOrganizer downgradeModOrganizer,
        VersionService versionService,
        IIsBusyService isBusyService,
        DiffMods.Handler diffMods,
        GetStalkerGammaLastCommit.Handler getStalkerGammaLastCommit,
        GetGitHubRepoCommits.Handler getGitHubRepoCommits
    )
    {
        Activator = new ViewModelActivator();
        IsBusyService = isBusyService;
        _versionString = $"{versionService.GetVersion()} (Based on 6.7.0.0)";
        var mo2Path = Path.Join(
            Path.GetDirectoryName(AppContext.BaseDirectory),
            "..",
            "ModOrganizer.exe"
        );

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

        AnomalyPathCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(getAnomalyPathHandler.Execute)
        );
        AnomalyPathCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR FINDING ANOMALY PATH
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _anomalyPath = AnomalyPathCmd.ToProperty(this, x => x.AnomalyPath);

        _userLtxPath = this.WhenAnyValue(x => x.AnomalyPath)
            .Select(x => Path.Join(x, "appdata", "user.ltx"))
            .ToProperty(this, x => x.UserLtxPath);

        var userLtxSetToFullscreenWineCanExec = this.WhenAnyValue(
            x => x.UserLtxPath,
            x => x.IsRanWithWine,
            selector: (userLtxPath, ranWithWine) => ranWithWine && File.Exists(userLtxPath)
        );
        UserLtxSetToFullscreenWineCmd = ReactiveCommand.CreateFromTask(
            async () =>
                await userLtxSetToFullscreenWine.ExecuteAsync(
                    new UserLtxSetToFullscreenWine.Query(UserLtxPath!)
                ),
            userLtxSetToFullscreenWineCanExec
        );
        UserLtxSetToFullscreenWineCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR DETERMINING IF USER.LTX IS SET TO FULLSCREEN
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _userLtxSetToFullscreenWine = UserLtxSetToFullscreenWineCmd.ToProperty(
            this,
            x => x.UserLtxSetToFullscreenWine
        );
        this.WhenAnyValue(
                x => x.UserLtxPath,
                x => x.IsRanWithWine,
                selector: (userLtxPath, ranWithWine) => ranWithWine && File.Exists(userLtxPath)
            )
            .Where(x => x)
            .Subscribe(_ => UserLtxSetToFullscreenWineCmd.Execute().Subscribe());

        UserLtxReplaceFullscreenWithBorderlessFullscreen = ReactiveCommand.CreateFromTask<string>(
            async pathToUserLtx =>
                await userLtxReplaceFullscreenWithBorderlessFullscreen.ExecuteAsync(
                    new UserLtxReplaceFullscreenWithBorderlessFullscreen.Command(pathToUserLtx)
                )
        );
        UserLtxReplaceFullscreenWithBorderlessFullscreen.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR EDITING USER.LTX WITH BORDERLESS FULLSCREEN
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        UserLtxSetToFullscreenWineCmd
            .Where(x => x.HasValue && x.Value)
            .Subscribe(_ =>
            {
                UserLtxReplaceFullscreenWithBorderlessFullscreen.Execute(UserLtxPath!).Subscribe();
                progressService.UpdateProgress(
                    """

                    Replaced user.ltx fullscreen option with borderless fullscreen to avoid issues
                    """
                );
            });

        LocalGammaVersionsCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
                getLocalGammaVersion.ExecuteAsync(
                    new GetLocalGammaVersion.Query(
                        Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "version.txt")
                    )
                )
            )
        );
        LocalGammaVersionsCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                   
                ERROR DETERMINING LOCAL GAMMA VERSION
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _localGammaVersion = LocalGammaVersionsCmd.ToProperty(this, x => x.LocalGammaVersion);

        IsMo2InitializedCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
                isMo2Initialized.Execute(
                    new IsMo2Initialized.Query(
                        Path.Join(
                            Path.GetDirectoryName(AppContext.BaseDirectory),
                            "..",
                            "ModOrganizer.ini"
                        )
                    )
                )
            )
        );
        IsMo2InitializedCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                   
                ERROR DETERMINING MODORGANIZER INITIALIZED
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _isMo2Initialized = IsMo2InitializedCmd.ToProperty(this, x => x.IsMo2Initialized);

        IsMo2VersionDowngradedCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
                isMo2VersionDowngraded.Execute(new IsMo2VersionDowngraded.Query(mo2Path))
            )
        );
        IsMo2VersionDowngradedCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR DETERMINING MODORGANIZER'S VERSION
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _isMo2VersionDowngraded = IsMo2VersionDowngradedCmd.ToProperty(
            this,
            x => x.IsMo2VersionDowngraded
        );

        LongPathsStatusCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(longPathsStatusHandler.Execute)
        );
        LongPathsStatusCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR RETRIEVING LONG PATHS STATUS
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        _longPathsStatus = LongPathsStatusCmd.ToProperty(this, x => x.LongPathsStatus);

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

        var canEnableLongPathsOnWindows = this.WhenAnyValue(
            x => x.IsRanWithWine,
            x => x.LongPathsStatus,
            selector: (ranWithWine, longPathsStatus) =>
                !ranWithWine
                && operatingSystemService.IsWindows()
                && longPathsStatus.HasValue
                && !longPathsStatus.Value
        );
        EnableLongPathsOnWindowsCmd = ReactiveCommand.CreateFromTask(
            async () =>
                await Task.Run(() =>
                {
                    enableLongPathsOnWindows.Execute();
                    progressService.UpdateProgress(
                        """

                        Enabled long paths via registry.
                        HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem
                        Set DWORD LongPathsEnabled 1
                        A restart is recommended before installing / updating gamma.
                        """
                    );
                }),
            canEnableLongPathsOnWindows
        );
        EnableLongPathsOnWindowsCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""

                ERROR ENABLING LONG PATHS
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        EnableLongPathsOnWindowsCmd.Subscribe(_ => LongPathsStatusCmd.Execute().Subscribe());

        var canAddFoldersToWinDefenderExclusion = this.WhenAnyValue(
            x => x.IsRanWithWine,
            x => x.IsBusyService.IsBusy,
            selector: (ranWithWine, isBusy) =>
                !isBusy && !ranWithWine && operatingSystemService.IsWindows()
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

        var canFirstInstallInitialization = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            x => x.IsRanWithWine,
            x => x.IsMo2VersionDowngraded,
            selector: (isBusy, inGrokModDir, ranWithWine, isMo2Downgraded) =>
                !isBusy
                && File.Exists(mo2Path)
                && inGrokModDir
                && (
                    !ranWithWine
                    || (ranWithWine && isMo2Downgraded.HasValue && isMo2Downgraded.Value)
                )
        );
        FirstInstallInitializationCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await gammaInstaller.FirstInstallInitialization();
                IsBusyService.IsBusy = false;
            },
            canFirstInstallInitialization
        );
        FirstInstallInitializationCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                Error in first install initialization:
                {x.Message}
                {x.InnerException?.Message}
                {x.StackTrace}
                """
            )
        );
        FirstInstallInitializationCmd.Subscribe(_ => IsMo2InitializedCmd.Execute().Subscribe());

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
            x => x.LongPathsStatus,
            x => x.IsMo2VersionDowngraded,
            x => x.IsRanWithWine,
            x => x.IsMo2Initialized,
            selector: (
                isBusy,
                inGrokModDir,
                toolsReady,
                longPathsStatus,
                mo2Downgraded,
                isRanWithWine,
                mo2Initialized
            ) =>
                !isBusy
                && inGrokModDir
                && toolsReady
                && mo2Initialized
                && (
                    !operatingSystemService.IsWindows()
                    || (
                        operatingSystemService.IsWindows()
                        && !isRanWithWine
                        && longPathsStatus.HasValue
                        && longPathsStatus.Value
                    )
                    || operatingSystemService.IsWindows()
                        && isRanWithWine
                        && mo2Downgraded.HasValue
                        && mo2Downgraded.Value
                )
        );
        InstallUpdateGammaCmd = ReactiveCommand.CreateFromTask(
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

                IsBusyService.IsBusy = false;
            },
            canInstallUpdateGamma
        );
        InstallUpdateGammaCmd.ThrownExceptions.Subscribe(x =>
            progressService.UpdateProgress(
                $"""
                ERROR INSTALLING/UPDATING GAMMA:
                {x.Message}
                {x.StackTrace}
                """
            )
        );
        InstallUpdateGammaCmd.Subscribe(_ => LocalGammaVersionsCmd.Execute().Subscribe());
        InstallUpdateGammaCmd.Subscribe(_ => BackgroundCheckUpdatesCmd.Execute().Subscribe());
        InstallUpdateGammaCmd
            .Where(_ => IsRanWithWine)
            .Subscribe(_ =>
                UserLtxReplaceFullscreenWithBorderlessFullscreen
                    .Execute(UserLtxPath ?? "")
                    .Subscribe()
            );

        var canPlay = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.InGrokModDir,
            x => x.IsMo2Initialized,
            x => x.LongPathsStatus,
            x => x.IsRanWithWine,
            x => x.LocalGammaVersion,
            selector: (
                isBusy,
                inGrokModDir,
                mo2Initialized,
                longPathsStatus,
                ranWithWine,
                localGammaVersion
            ) =>
                !isBusy
                && File.Exists(mo2Path)
                && inGrokModDir
                && mo2Initialized
                && (
                    ranWithWine
                    || (
                        !ranWithWine
                        && operatingSystemService.IsWindows()
                        && longPathsStatus.HasValue
                        && longPathsStatus.Value
                    )
                )
                && !string.IsNullOrWhiteSpace(localGammaVersion)
                && localGammaVersion != "200"
                && localGammaVersion != "865"
        );
        PlayCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                await Cli.Wrap(mo2Path).ExecuteAsync();
                IsBusyService.IsBusy = false;
            },
            canPlay
        );
        PlayCmd.ThrownExceptions.Subscribe(x =>
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
            x => x.IsMo2VersionDowngraded,
            x => x.IsRanWithWine,
            selector: (isBusy, inGrokModDir, isMo2Downgraded, ranWithWine) =>
                !isBusy
                && isMo2Downgraded.HasValue
                && !isMo2Downgraded.Value
                && inGrokModDir
                && ranWithWine
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
        DowngradeModOrganizerCmd.Subscribe(_ => IsMo2VersionDowngradedCmd.Execute().Subscribe());

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

                AnomalyPathCmd.Execute().Subscribe();

                LocalGammaVersionsCmd.Execute().Subscribe();

                IsMo2VersionDowngradedCmd.Execute().Subscribe();

                IsMo2InitializedCmd.Execute().Subscribe();

                IsRanWithWineCmd.Execute().Subscribe();

                LongPathsStatusCmd.Execute().Subscribe();

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

    public IIsBusyService IsBusyService { get; }

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

    public bool? IsMo2VersionDowngraded => _isMo2VersionDowngraded.Value;

    public bool? LongPathsStatus => _longPathsStatus.Value;

    public bool IsRanWithWine => _isRanWithWine.Value;

    public bool IsMo2Initialized => _isMo2Initialized.Value;

    public string? LocalGammaVersion => _localGammaVersion.Value;

    public bool? UserLtxSetToFullscreenWine => _userLtxSetToFullscreenWine.Value;

    public string? AnomalyPath => _anomalyPath.Value;
    public string? UserLtxPath => _userLtxPath.Value;

    public ReactiveCommand<Unit, bool> IsRanWithWineCmd { get; set; }
    public ReactiveCommand<Unit, Unit> EnableLongPathsOnWindowsCmd { get; set; }
    public ReactiveCommand<Unit, string> AddFoldersToWinDefenderExclusionCmd { get; set; }
    private ReactiveCommand<Unit, ToolsReadyRecord> ToolsReadyCommand { get; }
    public ReactiveCommand<Unit, Unit> FirstInstallInitializationCmd { get; }
    public ReactiveCommand<Unit, Unit> InstallUpdateGammaCmd { get; }
    public ReactiveCommand<Unit, Unit> PlayCmd { get; }
    public ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    public ReactiveCommand<Unit, Unit> DowngradeModOrganizerCmd { get; }
    public ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    public ReactiveCommand<Unit, Unit> InGroksModPackDir { get; }
    public ReactiveCommand<Unit, bool?> LongPathsStatusCmd { get; }
    public ReactiveCommand<Unit, bool?> IsMo2VersionDowngradedCmd { get; }
    public ReactiveCommand<Unit, bool> IsMo2InitializedCmd { get; }
    public ReactiveCommand<Unit, string?> LocalGammaVersionsCmd { get; }
    public ReactiveCommand<Unit, bool?> UserLtxSetToFullscreenWineCmd { get; }
    public ReactiveCommand<string, Unit> UserLtxReplaceFullscreenWithBorderlessFullscreen { get; }
    public ReactiveCommand<Unit, string?> AnomalyPathCmd { get; }
    public ViewModelActivator Activator { get; }
}
