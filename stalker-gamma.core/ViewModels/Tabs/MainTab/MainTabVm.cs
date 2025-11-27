using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using CliWrap;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using stalker_gamma.core.Services.GammaInstaller;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Enums;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Factories;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Models;
using stalker_gamma.core.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.MainTab;

public partial class MainTabVm : ViewModelBase, IActivatableViewModel
{
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly string _modsPath = Path.GetFullPath(Path.Join(Dir, "..", "mods"));
    private ObservableAsPropertyHelper<double?>? _progress;
    private string _versionString;
    private ObservableAsPropertyHelper<bool>? _toolsReady;
    private ObservableAsPropertyHelper<bool>? _isRanWithWine;
    private ObservableAsPropertyHelper<bool?>? _longPathsStatus;
    private ObservableAsPropertyHelper<bool?>? _isMo2VersionDowngraded;
    private ObservableAsPropertyHelper<bool>? _isMo2Initialized;
    private ObservableAsPropertyHelper<string?>? _localGammaVersion;
    private ObservableAsPropertyHelper<bool?>? _userLtxSetToFullscreenWine;
    private ObservableAsPropertyHelper<string?>? _anomalyPath;
    private ObservableAsPropertyHelper<string?>? _userLtxPath;
    private readonly ReadOnlyObservableCollection<ModDownloadExtractProgressVm> _modDownloadExtractProgressVms;
    private readonly ReadOnlyObservableCollection<ModListRecord> _localMods;

    // lmao
    private Func<ModDownloadExtractProgressVm, bool> CreateModFilterPredicate(
        (InstallType installType, ReadOnlyObservableCollection<ModListRecord> localMods) tuple
    ) =>
        vm =>
        {
            return IsNotDone()
                && (
                    vm.ModListRecord is GitRecord or ModpackSpecific
                    || tuple.installType == InstallType.FullInstall
                        && vm.ModListRecord is not Separator or ModDbRecord or GithubRecord
                    || vm.ModListRecord is ModDbRecord mdr && (IsNewMod(mdr) || IsVersionUpdate())
                    || vm.ModListRecord is Separator s && NewSeparatorFolder(s)
                );

            bool NewSeparatorFolder(Separator s)
            {
                return !Path.Exists(Path.Join(_modsPath, s.FolderName));
            }

            bool IsNewMod(ModDbRecord modDbRecord)
            {
                return tuple
                    .localMods.Where(lm => lm is ModDbRecord)
                    .Cast<ModDbRecord>()
                    .All(lm => lm.AddonName != modDbRecord.AddonName);
            }

            bool IsVersionUpdate()
            {
                return tuple
                    .localMods.Where(lm => lm is ModDbRecord)
                    .Cast<ModDbRecord>()
                    .FirstOrDefault(lm =>
                        lm.AddonName == mdr.AddonName
                        && FileNameVersionRx().Match(lm.ZipName!).Groups["version"].Value
                            != FileNameVersionRx().Match(mdr.ZipName!).Groups["version"].Value
                    )
                    is not null;
            }

            bool IsNotDone()
            {
                return vm.Status != Status.Done;
            }
        };

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
        GetGitHubRepoCommits.Handler getGitHubRepoCommits,
        Queries.GetModDownloadExtractVms.Handler getModDownloadExtractVmsHandler,
        ModDownloadExtractProgressVmFactory modDownloadExtractProgressVmFactory,
        GetLocalMods.Handler getLocalModsHandler
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

        var localModsSourceList = new SourceList<ModListRecord>();
        var localModObs = localModsSourceList.Connect().Bind(out _localMods).Subscribe();
        var modProgressVms = new SourceList<ModListRecord>();
        var locker = new object();
        var modFilter = this.WhenAnyValue(x => x.SelectedInstallType, x => x.LocalMods)
            .Select(CreateModFilterPredicate);
        var modProgObs = modProgressVms.Connect();
        modProgObs
            .Transform(modDownloadExtractProgressVmFactory.Create)
            .BindToObservableList(out var unfiltered);

        unfiltered
            .Connect()
            .AutoRefresh(x => x.Status)
            .Filter(modFilter)
            .Sort(
                SortExpressionComparer<ModDownloadExtractProgressVm>.Ascending(x =>
                    x.ModListRecord.Counter
                )
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Synchronize(locker)
            .BindToObservableList(out var observableList)
            .Subscribe();

        unfiltered
            .Connect()
            .ObserveOn(RxApp.MainThreadScheduler)
            .AutoRefresh(x => x.Status)
            .SelectMany(x => x)
            .Where(x =>
                x.Reason is ListChangeReason.Refresh or ListChangeReason.Replace
                && x.Item.Current.Status == Status.Done
            )
            .Subscribe(_ =>
            {
                var done = unfiltered.Items.Where(x => x.Status == Status.Done).ToList();
                var total = unfiltered.Count;
                progressService.UpdateProgress((double)done.Count / total * 100);
            });

        observableList.Connect().Bind(out _modDownloadExtractProgressVms).Subscribe();

        GetModDownloadExtractProgressVmsCmd = ReactiveCommand.CreateFromTask(async () =>
            await getModDownloadExtractVmsHandler.ExecuteAsync()
        );

        GetLocalModsCmd = ReactiveCommand.CreateFromTask(async () =>
            await getLocalModsHandler.ExecuteAsync()
        );

        IsRanWithWineCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(isRanWithWineService.IsRanWithWine)
        );

        AnomalyPathCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(getAnomalyPathHandler.Execute)
        );

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

        UserLtxReplaceFullscreenWithBorderlessFullscreen = ReactiveCommand.CreateFromTask<string>(
            async pathToUserLtx =>
                await userLtxReplaceFullscreenWithBorderlessFullscreen.ExecuteAsync(
                    new UserLtxReplaceFullscreenWithBorderlessFullscreen.Command(pathToUserLtx)
                )
        );

        LocalGammaVersionsCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
                getLocalGammaVersion.ExecuteAsync(
                    new GetLocalGammaVersion.Query(
                        Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory), "version.txt")
                    )
                )
            )
        );

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

        IsMo2VersionDowngradedCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() =>
                isMo2VersionDowngraded.Execute(new IsMo2VersionDowngraded.Query(mo2Path))
            )
        );

        LongPathsStatusCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(longPathsStatusHandler.Execute)
        );

        ToolsReadyCommand = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(() => new ToolsReadyRecord(curlService.Ready))
        );

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
                            Path.Join(Dir, "resources", "Stalker_GAMMA")
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
                        DeleteReshadeDlls,
                        globalSettings.UseCurlImpersonate,
                        PreserveUserLtx,
                        ModDownloadExtractProgressVms ?? throw new InvalidOperationException(),
                        locker
                    )
                );

                IsBusyService.IsBusy = false;
            },
            canInstallUpdateGamma
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

        AppendLineInteraction = new Interaction<string, Unit>();

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
                GetLocalModsCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR GETTING LOCAL MODS
                            {x}
                            """
                        )
                    )
                    .DisposeWith(d);
                GetLocalModsCmd
                    .Subscribe(x =>
                        localModsSourceList.Edit(inner =>
                        {
                            inner.Clear();
                            inner.AddRange(x);
                        })
                    )
                    .DisposeWith(d);
                _progress = progressService
                    .ProgressObservable.ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x => x.Progress)
                    .WhereNotNull()
                    .ToProperty(this, x => x.Progress)
                    .DisposeWith(d);
                GetModDownloadExtractProgressVmsCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR GETTING MOD DOWNLOAD PROGRESS VMS
                            {x}
                            """
                        )
                    )
                    .DisposeWith(d);
                GetModDownloadExtractProgressVmsCmd
                    .Subscribe(x =>
                        modProgressVms.Edit(inner =>
                        {
                            inner.Clear();
                            inner.AddRange(
                                [
                                    new GitRecord { AddonName = "Stalker_GAMMA", Counter = -4 },
                                    new GitRecord
                                    {
                                        AddonName = "gamma_large_files_v2",
                                        Counter = -3,
                                    },
                                    new GitRecord
                                    {
                                        AddonName = "teivaz_anomaly_gunslinger",
                                        Counter = -2,
                                    },
                                ]
                            );
                            inner.AddRange(x);
                            inner.AddRange(
                                [
                                    new ModpackSpecific
                                    {
                                        AddonName = "modpack_addons",
                                        Counter = 999999,
                                    },
                                ]
                            );
                        })
                    )
                    .DisposeWith(d);
                IsMo2InitializedCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""
                               
                            ERROR DETERMINING MODORGANIZER INITIALIZED
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                _isMo2Initialized = IsMo2InitializedCmd
                    .ToProperty(this, x => x.IsMo2Initialized)
                    .DisposeWith(d);
                _localGammaVersion = LocalGammaVersionsCmd
                    .ToProperty(this, x => x.LocalGammaVersion)
                    .DisposeWith(d);
                _isMo2VersionDowngraded = IsMo2VersionDowngradedCmd
                    .ToProperty(this, x => x.IsMo2VersionDowngraded)
                    .DisposeWith(d);
                _longPathsStatus = LongPathsStatusCmd
                    .ToProperty(this, x => x.LongPathsStatus)
                    .DisposeWith(d);

                IsRanWithWineCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR DETERMINING IF RAN WITH WINE
                            {x}
                            """
                        )
                    )
                    .DisposeWith(d);
                _isRanWithWine = IsRanWithWineCmd
                    .ToProperty(this, x => x.IsRanWithWine)
                    .DisposeWith(d);
                DowngradeModOrganizerCmd
                    .ThrownExceptions.Subscribe(x => progressService.UpdateProgress(x.Message))
                    .DisposeWith(d);
                DowngradeModOrganizerCmd
                    .Subscribe(_ => IsMo2VersionDowngradedCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
                PlayCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""
                            ERROR PLAYING:
                            {x}
                            """
                        )
                    )
                    .DisposeWith(d);
                InstallUpdateGammaCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""
                            ERROR INSTALLING/UPDATING GAMMA:
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                InstallUpdateGammaCmd
                    .Subscribe(_ => LocalGammaVersionsCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
                InstallUpdateGammaCmd
                    .Subscribe(_ => BackgroundCheckUpdatesCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
                InstallUpdateGammaCmd
                    .Where(_ => IsRanWithWine)
                    .Subscribe(_ =>
                        UserLtxReplaceFullscreenWithBorderlessFullscreen
                            .Execute(UserLtxPath ?? "")
                            .Subscribe()
                    )
                    .DisposeWith(d);
                BackgroundCheckUpdatesCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR CHECKING FOR UPDATES
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                AddFoldersToWinDefenderExclusionCmd
                    .Subscribe(progressService.UpdateProgress)
                    .DisposeWith(d);
                AddFoldersToWinDefenderExclusionCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            User either denied UAC prompt or there was an error.
                            """
                        )
                    )
                    .DisposeWith(d);
                FirstInstallInitializationCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""
                            Error in first install initialization:
                            {x.Message}
                            {x.InnerException?.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                FirstInstallInitializationCmd
                    .Subscribe(_ => IsMo2InitializedCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
                EnableLongPathsOnWindowsCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR ENABLING LONG PATHS
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                EnableLongPathsOnWindowsCmd
                    .Subscribe(_ => LongPathsStatusCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
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
                    })
                    .DisposeWith(d);
                LocalGammaVersionsCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""
                               
                            ERROR DETERMINING LOCAL GAMMA VERSION
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                UserLtxReplaceFullscreenWithBorderlessFullscreen
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR EDITING USER.LTX WITH BORDERLESS FULLSCREEN
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                UserLtxSetToFullscreenWineCmd
                    .Where(x => x.HasValue && x.Value)
                    .Subscribe(_ =>
                    {
                        UserLtxReplaceFullscreenWithBorderlessFullscreen
                            .Execute(UserLtxPath!)
                            .Subscribe();
                        progressService.UpdateProgress(
                            """

                            Replaced user.ltx fullscreen option with borderless fullscreen to avoid issues
                            """
                        );
                    })
                    .DisposeWith(d);
                this.WhenAnyValue(
                        x => x.UserLtxPath,
                        x => x.IsRanWithWine,
                        selector: (userLtxPath, ranWithWine) =>
                            ranWithWine && File.Exists(userLtxPath)
                    )
                    .Where(x => x)
                    .Subscribe(_ =>
                        UserLtxSetToFullscreenWineCmd.Execute().Subscribe().DisposeWith(d)
                    )
                    .DisposeWith(d);
                AnomalyPathCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR FINDING ANOMALY PATH
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                IsMo2VersionDowngradedCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR DETERMINING MODORGANIZER'S VERSION
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                LongPathsStatusCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR RETRIEVING LONG PATHS STATUS
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                UserLtxSetToFullscreenWineCmd
                    .ThrownExceptions.Subscribe(x =>
                        progressService.UpdateProgress(
                            $"""

                            ERROR DETERMINING IF USER.LTX IS SET TO FULLSCREEN
                            {x.Message}
                            {x.StackTrace}
                            """
                        )
                    )
                    .DisposeWith(d);
                _userLtxSetToFullscreenWine = UserLtxSetToFullscreenWineCmd
                    .ToProperty(this, x => x.UserLtxSetToFullscreenWine)
                    .DisposeWith(d);
                _anomalyPath = AnomalyPathCmd.ToProperty(this, x => x.AnomalyPath).DisposeWith(d);

                _userLtxPath = this.WhenAnyValue(x => x.AnomalyPath)
                    .Select(x => Path.Join(x, "appdata", "user.ltx"))
                    .ToProperty(this, x => x.UserLtxPath)
                    .DisposeWith(d);
                _toolsReady = ToolsReadyCommand
                    .Select(x => x.CurlReady)
                    .ToProperty(this, x => x.ToolsReady)
                    .DisposeWith(d);

                InGrokModDir = Dir.Contains(
#if DEBUG
                    "net10.0",
#else
                    ".Grok's Modpack Installer",
#endif
                    StringComparison.OrdinalIgnoreCase);

                InGroksModPackDir.Execute().Subscribe().DisposeWith(d);

                AnomalyPathCmd.Execute().Subscribe().DisposeWith(d);

                LocalGammaVersionsCmd.Execute().Subscribe().DisposeWith(d);

                IsMo2VersionDowngradedCmd.Execute().Subscribe().DisposeWith(d);

                IsMo2InitializedCmd.Execute().Subscribe().DisposeWith(d);

                IsRanWithWineCmd.Execute().Subscribe().DisposeWith(d);

                LongPathsStatusCmd.Execute().Subscribe().DisposeWith(d);

                ToolsReadyCommand
                    .Execute()
                    .Where(x => x.CurlReady)
                    .Subscribe(_ =>
                    {
                        GetModDownloadExtractProgressVmsCmd.Execute().Subscribe().DisposeWith(d);
                        BackgroundCheckUpdatesCmd.Execute().Subscribe().DisposeWith(d);
                        GetLocalModsCmd.Execute().Subscribe().DisposeWith(d);
                    })
                    .DisposeWith(d);
            }
        );
    }

    public ReadOnlyObservableCollection<ModDownloadExtractProgressVm> ModDownloadExtractProgressVms =>
        _modDownloadExtractProgressVms;

    public ReadOnlyObservableCollection<ModListRecord> LocalMods => _localMods;

    public bool NeedUpdate
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool NeedModDbUpdate
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private bool ToolsReady => _toolsReady?.Value ?? false;

    public bool InGrokModDir
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string GammaVersionToolTip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public string ModVersionToolTip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public IIsBusyService IsBusyService { get; }

    public Interaction<string, Unit> AppendLineInteraction { get; }

    public double Progress => _progress?.Value ?? 0;

    public bool CheckMd5
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool PreserveUserLtx
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool ForceGitDownload
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool ForceZipExtraction
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public bool DeleteReshadeDlls
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public string VersionString
    {
        get => _versionString;
        set => this.RaiseAndSetIfChanged(ref _versionString, value);
    }

    public bool? IsMo2VersionDowngraded => _isMo2VersionDowngraded?.Value;

    public bool? LongPathsStatus => _longPathsStatus?.Value;

    public bool IsRanWithWine => _isRanWithWine?.Value ?? false;

    public bool IsMo2Initialized => _isMo2Initialized?.Value ?? false;

    public string? LocalGammaVersion => _localGammaVersion?.Value;

    public bool? UserLtxSetToFullscreenWine => _userLtxSetToFullscreenWine?.Value;

    public string? AnomalyPath => _anomalyPath?.Value;
    public string? UserLtxPath => _userLtxPath?.Value;

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
    public ReactiveCommand<Unit, IList<ModListRecord>> GetModDownloadExtractProgressVmsCmd { get; }
    private ReactiveCommand<Unit, IList<ModListRecord>> GetLocalModsCmd { get; }
    public ViewModelActivator Activator { get; }
    public IReadOnlyList<InstallType> InstallTypes { get; } =
        [InstallType.FullInstall, InstallType.Update];
    public InstallType SelectedInstallType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = InstallType.FullInstall;

    [GeneratedRegex(@".+(?<version>\d+\.\d+\.\d*.*)\.*")]
    private static partial Regex FileNameVersionRx();
}
