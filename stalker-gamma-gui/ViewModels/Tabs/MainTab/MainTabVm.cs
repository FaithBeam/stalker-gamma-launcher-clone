using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CliWrap;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using stalker_gamma_gui.Services;
using stalker_gamma_gui.ViewModels.Services;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Commands;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Enums;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Factories;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Models;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Queries;
using stalker_gamma_gui.ViewModels.Tabs.Queries;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DownloadModOrganizer;
using stalker_gamma.core.Utilities;
using GammaInstaller = stalker_gamma_gui.Services.GammaInstaller.GammaInstaller;
using ProgressService = stalker_gamma_gui.Services.ProgressService;

namespace stalker_gamma_gui.ViewModels.Tabs.MainTab;

public partial class MainTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly SettingsFileService _settingsFileService;
    private static readonly string Dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private readonly string _modsPath = Path.GetFullPath(Path.Join(Dir, "..", "mods"));
    private ObservableAsPropertyHelper<double?>? _progress;
    private ObservableAsPropertyHelper<bool>? _toolsReady;
    private ObservableAsPropertyHelper<bool>? _isRanWithWine;
    private ObservableAsPropertyHelper<bool?>? _longPathsStatus;
    private ObservableAsPropertyHelper<bool?>? _isMo2VersionDowngraded;
    private ObservableAsPropertyHelper<bool>? _isMo2Initialized;
    private ObservableAsPropertyHelper<string?>? _localGammaVersion;
    private ObservableAsPropertyHelper<bool?>? _userLtxSetToFullscreenWine;
    private ObservableAsPropertyHelper<string?>? _anomalyPath;
    private ObservableAsPropertyHelper<string?>? _gammaPath;
    private ObservableAsPropertyHelper<string?>? _userLtxPath;
    private readonly ReadOnlyObservableCollection<ModDownloadExtractProgressVm> _modDownloadExtractProgressVms;
    private readonly ReadOnlyObservableCollection<ModListRecord> _localMods;

    private Func<ModDownloadExtractProgressVm, bool> CreateModFilterPredicate(
        (InstallType installType, ReadOnlyObservableCollection<ModListRecord> localMods) tuple
    ) =>
        vm =>
        {
            if (StatusIsDone())
            {
                return false;
            }

            if (vm.ModListRecord is GitRecord or ModpackSpecific)
            {
                return true;
            }

            if (tuple.installType == InstallType.FullInstall)
            {
                return true;
            }

            if (tuple.installType == InstallType.Update)
            {
                if (vm.ModListRecord is ModDbRecord mdr && (IsNewMod(mdr) || IsVersionUpdate(mdr)))
                {
                    return true;
                }

                if (vm.ModListRecord is Separator s && NewSeparatorFolder(s))
                {
                    return true;
                }
            }

            return false;

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

            bool IsVersionUpdate(ModDbRecord modDbRecord)
            {
                return tuple
                    .localMods.Where(lm => lm is ModDbRecord)
                    .Cast<ModDbRecord>()
                    .FirstOrDefault(lm =>
                        lm.AddonName == modDbRecord.AddonName
                        && FileNameVersionRx().Match(lm.ZipName!).Groups["version"].Value
                            != FileNameVersionRx()
                                .Match(modDbRecord.ZipName!)
                                .Groups["version"]
                                .Value
                    )
                    is not null;
            }

            bool StatusIsDone()
            {
                return vm.Status == Status.Done;
            }
        };

    public MainTabVm(
        FilePickerService filePickerService,
        AnomalyInstaller anomalyInstaller,
        IUserLtxReplaceFullscreenWithBorderlessFullscreen userLtxReplaceFullscreenWithBorderlessFullscreen,
        IUserLtxSetToFullscreenWine userLtxSetToFullscreenWine,
        IGetLocalGammaVersion getLocalGammaVersion,
        IIsMo2Initialized isMo2Initialized,
        IIsMo2VersionDowngraded isMo2VersionDowngraded,
        IILongPathsStatusService longPathsStatusHandler,
        IIsRanWithWineService isRanWithWineService,
        EnableLongPathsOnWindows.Handler enableLongPathsOnWindows,
        AddFoldersToWinDefenderExclusion.Handler addFoldersToWinDefenderExclusion,
        GetGammaBackupFolder.Handler getGammaBackupFolderHandler,
        ICurlService curlService,
        GammaInstaller gammaInstaller,
        ProgressService progressService,
        GlobalSettings globalSettings,
        DownloadModOrganizerService downloadModOrganizerService,
        IVersionService versionService,
        IIsBusyService isBusyService,
        DiffMods.Handler diffMods,
        GetStalkerGammaLastCommit.Handler getStalkerGammaLastCommit,
        GetGitHubRepoCommits.Handler getGitHubRepoCommits,
        Queries.GetModDownloadExtractVms.Handler getModDownloadExtractVmsHandler,
        ModDownloadExtractProgressVmFactory modDownloadExtractProgressVmFactory,
        GetLocalMods.Handler getLocalModsHandler,
        ModalService modalService,
        SettingsFileService settingsFileService,
        WriteModOrganizerIniService writeModOrganizerIniService
    )
    {
        var fileService1 = filePickerService;
        _settingsFileService = settingsFileService;
        Activator = new ViewModelActivator();
        IsBusyService = isBusyService;
        VersionString = $"{versionService.GetVersion()} (Based on 6.7.0.0)";
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
                progressService.UpdateProgress((double)done.Count / InitialFilteredListCount * 100);
            });

        observableList.Connect().Bind(out _modDownloadExtractProgressVms).Subscribe();

        ShowSelectFolderCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            if (settingsFileService.SettingsInitialized)
            {
                return;
            }

            while (string.IsNullOrWhiteSpace(settingsFileService.SettingsFile.BaseGammaDirectory))
            {
                settingsFileService.SettingsFile.BaseGammaDirectory =
                    await fileService1.SelectFolder("Select Your Base GAMMA Folder");
            }
            await settingsFileService.SaveAsync();

            if (!string.IsNullOrWhiteSpace(settingsFileService.SettingsFile.BaseGammaDirectory))
            {
                settingsFileService.SettingsInitialized = true;
            }
        });

        GetModDownloadExtractProgressVmsCmd = ReactiveCommand.CreateFromTask(async () =>
            await getModDownloadExtractVmsHandler.ExecuteAsync()
        );

        GetLocalModsCmd = ReactiveCommand.CreateFromTask(async () =>
            await getLocalModsHandler.ExecuteAsync()
        );

        IsRanWithWineCmd = ReactiveCommand.CreateFromTask(async () =>
            await Task.Run(isRanWithWineService.IsRanWithWine)
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

        var canIsMo2Initialized = this.WhenAnyValue(
            x => x._settingsFileService.SettingsInitialized,
            x => x._settingsFileService.SettingsFile.GammaDir,
            selector: (initialized, gammaDir) => initialized && !string.IsNullOrWhiteSpace(gammaDir)
        );
        IsMo2InitializedCmd = ReactiveCommand.CreateFromTask(
            async () =>
                await Task.Run(() =>
                    isMo2Initialized.Execute(
                        new IsMo2Initialized.Query(
                            Path.Join(
                                _settingsFileService.SettingsFile.GammaDir,
                                "ModOrganizer.ini"
                            )
                        )
                    )
                ),
            canIsMo2Initialized
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
                && OperatingSystem.IsWindows()
                && longPathsStatus.HasValue
                && !longPathsStatus.Value
        );
        EnableLongPathsOnWindowsCmd = ReactiveCommand.CreateFromTask(
            async () =>
                await Task.Run(() =>
                {
                    enableLongPathsOnWindows.Execute();
                    modalService.ShowInformationDlg(
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
            x => x.AnomalyPath,
            x => x.GammaPath,
            selector: (ranWithWine, isBusy, anomalyPath, gammaPath) =>
                !isBusy
                && !ranWithWine
                && OperatingSystem.IsWindows()
                && !string.IsNullOrWhiteSpace(anomalyPath)
                && !string.IsNullOrWhiteSpace(gammaPath)
        );
        AddFoldersToWinDefenderExclusionCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var gammaBackupPath = getGammaBackupFolderHandler.Execute();
                await Task.Run(() =>
                    addFoldersToWinDefenderExclusion.Execute(
                        new AddFoldersToWinDefenderExclusion.Command(
                            AnomalyPath!,
                            GammaPath!,
                            gammaBackupPath
                        )
                    )
                );
                return $"""
                Added folder exclusions to Microsoft Defender for:
                {AnomalyPath}
                {GammaPath}
                {gammaBackupPath}
                """;
            },
            canAddFoldersToWinDefenderExclusion
        );

        var canFirstInstallInitialization = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.IsMo2Initialized,
            x => x._settingsFileService.SettingsInitialized,
            selector: (isBusy, mo2Initialized, settingsInitialized) =>
                !isBusy && !mo2Initialized && settingsInitialized
        );
        FirstInstallInitializationCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                var mo2Version = OperatingSystem.IsWindows() ? "v.2.5.2" : "v2.4.4";
                IsBusyService.IsBusy = true;
                await downloadModOrganizerService.DownloadAsync(
                    mo2Version,
                    _settingsFileService.SettingsFile.CacheDir!,
                    _settingsFileService.SettingsFile.GammaDir
                );
                await writeModOrganizerIniService.WriteAsync(
                    _settingsFileService.SettingsFile.GammaDir!,
                    _settingsFileService.SettingsFile.AnomalyDir!,
                    mo2Version
                );
                IsBusyService.IsBusy = false;
            },
            canFirstInstallInitialization
        );

        BackgroundCheckUpdatesCmd = ReactiveCommand.CreateFromTask(() =>
            Task.Run(async () =>
            {
                var needUpdates = await gammaInstaller.CheckGammaData();
                var remoteGammaVersionHash = (
                    await getGitHubRepoCommits.ExecuteAsync(
                        new GetGitHubRepoCommits.Query("Grokitach", "Stalker_GAMMA")
                    )
                )
                    ?.FirstOrDefault()
                    ?[..9];
                var stalkerGammaRepoPath = Path.Join(
                    _settingsFileService.SettingsFile.CacheDir,
                    "Stalker_GAMMA"
                );
                var localGammaVersionHash = File.Exists(stalkerGammaRepoPath)
                    ? (
                        await getStalkerGammaLastCommit.ExecuteAsync(
                            new GetStalkerGammaLastCommit.Query(stalkerGammaRepoPath)
                        )
                    )[..9]
                    : "";
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
            x => x.ToolsReady,
            x => x.LongPathsStatus,
            x => x.IsMo2VersionDowngraded,
            x => x.IsRanWithWine,
            x => x.IsMo2Initialized,
            selector: (
                isBusy,
                toolsReady,
                longPathsStatus,
                mo2Downgraded,
                isRanWithWine,
                mo2Initialized
            ) =>
                !isBusy
                && toolsReady
                && (
                    (!OperatingSystem.IsWindows() && mo2Initialized)
                    || (
                        OperatingSystem.IsWindows()
                        && mo2Initialized
                        && !isRanWithWine
                        && longPathsStatus.HasValue
                        && longPathsStatus.Value
                    )
                    || OperatingSystem.IsWindows()
                        && mo2Initialized
                        && isRanWithWine
                        && mo2Downgraded.HasValue
                        && mo2Downgraded.Value
                )
        );
        InstallUpdateGammaCmd = ReactiveCommand.CreateFromTask(
            async () =>
            {
                IsBusyService.IsBusy = true;
                InitialFilteredListCount = observableList.Count;
                await Task.Run(() =>
                    gammaInstaller.InstallUpdateGammaAsync(
                        DeleteReshadeDlls,
                        PreserveUserLtx,
                        ModDownloadExtractProgressVms ?? throw new InvalidOperationException(),
                        locker,
                        GammaPath!,
                        AnomalyPath!
                    )
                );
                IsBusyService.IsBusy = false;
                modalService.ShowInformationDlg("Installation complete. You can now play.");
            },
            canInstallUpdateGamma
        );

        var canPlay = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            x => x.IsMo2Initialized,
            x => x.LongPathsStatus,
            x => x.IsRanWithWine,
            x => x.LocalGammaVersion,
            selector: (isBusy, mo2Initialized, longPathsStatus, ranWithWine, localGammaVersion) =>
                !isBusy
                && File.Exists(mo2Path)
                && mo2Initialized
                && (
                    ranWithWine
                    || (
                        !ranWithWine
                        && OperatingSystem.IsWindows()
                        && longPathsStatus.HasValue
                        && longPathsStatus.Value
                    )
                )
                && !string.IsNullOrWhiteSpace(localGammaVersion)
                && int.TryParse(localGammaVersion, out var parsedLocalGammaVersion)
                && parsedLocalGammaVersion >= 920
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

        AppendLineInteraction = new Interaction<string, Unit>();

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                ShowSelectFolderCmd.ThrownExceptions.Subscribe(x =>
                    modalService.ShowErrorDlg(
                        $"""
                        ERROR SELECTING FOLDER: 
                        {x}
                        """
                    )
                );
                ShowSelectFolderCmd.Execute().Subscribe().DisposeWith(d);
                GetLocalModsCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR GETTING LOCAL MODS
                            {x}
                            """
                        );
                    })
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
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR GETTING MOD DOWNLOAD PROGRESS VMS
                            {x}
                            """
                        );
                    })
                    .DisposeWith(d);
                GetModDownloadExtractProgressVmsCmd
                    .Subscribe(x =>
                        modProgressVms.Edit(inner =>
                        {
                            inner.Clear();

                            var minCounter = x.MinBy(y => y.Counter)!.Counter;
                            inner.AddRange(
                                [
                                    new ModpackSpecific
                                    {
                                        AddonName = "Anomaly",
                                        Counter = --minCounter,
                                    },
                                ]
                            );
                            inner.AddRange(x);
                            var maxCounter = x.MaxBy(y => y.Counter)!.Counter;
                            inner.AddRange(
                                [
                                    new GitRecord
                                    {
                                        AddonName = "Stalker_GAMMA",
                                        Counter = ++maxCounter,
                                    },
                                    new GitRecord
                                    {
                                        AddonName = "gamma_large_files_v2",
                                        Counter = ++maxCounter,
                                    },
                                    new GitRecord
                                    {
                                        AddonName = "teivaz_anomaly_gunslinger",
                                        Counter = ++maxCounter,
                                    },
                                    new ModpackSpecific
                                    {
                                        AddonName = "modpack_addons",
                                        Counter = ++maxCounter,
                                    },
                                ]
                            );
                        })
                    )
                    .DisposeWith(d);
                IsMo2InitializedCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR DETERMINING MODORGANIZER INITIALIZED
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
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
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR DETERMINING IF RAN WITH WINE
                            {x}
                            """
                        );
                    })
                    .DisposeWith(d);
                _isRanWithWine = IsRanWithWineCmd
                    .ToProperty(this, x => x.IsRanWithWine)
                    .DisposeWith(d);
                PlayCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR PLAYING:
                            {x}
                            """
                        );
                    })
                    .DisposeWith(d);
                InstallUpdateGammaCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR INSTALLING/UPDATING GAMMA:
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
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
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR CHECKING FOR UPDATES
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                AddFoldersToWinDefenderExclusionCmd
                    .Subscribe(modalService.ShowInformationDlg)
                    .DisposeWith(d);
                AddFoldersToWinDefenderExclusionCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            "User either denied UAC prompt or there was an error."
                        );
                    })
                    .DisposeWith(d);
                FirstInstallInitializationCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            Error in first install initialization:
                            {x.Message}
                            {x.InnerException?.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                FirstInstallInitializationCmd
                    .Subscribe(_ => IsMo2InitializedCmd.Execute().Subscribe().DisposeWith(d))
                    .DisposeWith(d);
                EnableLongPathsOnWindowsCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR ENABLING LONG PATHS
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
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
                        modalService.ShowErrorDlg(
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
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR DETERMINING LOCAL GAMMA VERSION
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                UserLtxReplaceFullscreenWithBorderlessFullscreen
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR EDITING USER.LTX WITH BORDERLESS FULLSCREEN
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                UserLtxSetToFullscreenWineCmd
                    .Where(x => x.HasValue && x.Value && globalSettings.ForceBorderlessFullscreen)
                    .Subscribe(_ =>
                    {
                        UserLtxReplaceFullscreenWithBorderlessFullscreen
                            .Execute(UserLtxPath!)
                            .Subscribe();
                        modalService.ShowInformationDlg(
                            "Replaced user.ltx fullscreen option with borderless fullscreen to avoid issues"
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
                IsMo2VersionDowngradedCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR DETERMINING MODORGANIZER'S VERSION
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                LongPathsStatusCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR RETRIEVING LONG PATHS STATUS
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                UserLtxSetToFullscreenWineCmd
                    .ThrownExceptions.Subscribe(x =>
                    {
                        modalService.ShowErrorDlg(
                            $"""
                            ERROR DETERMINING IF USER.LTX IS SET TO FULLSCREEN
                            {x.Message}
                            {x.StackTrace}
                            """
                        );
                    })
                    .DisposeWith(d);
                _userLtxSetToFullscreenWine = UserLtxSetToFullscreenWineCmd
                    .ToProperty(this, x => x.UserLtxSetToFullscreenWine)
                    .DisposeWith(d);

                _anomalyPath = this.WhenAnyValue(
                        x => x._settingsFileService.SettingsInitialized,
                        x => x._settingsFileService.SettingsFile.AnomalyDir,
                        selector: (initialized, anomalyDir) => (initialized, anomalyDir)
                    )
                    .Where(x => x.initialized)
                    .Select(x => x.anomalyDir)
                    .ToProperty(this, x => x.AnomalyPath)
                    .DisposeWith(d);
                _gammaPath = this.WhenAnyValue(
                        x => x._settingsFileService.SettingsInitialized,
                        x => x._settingsFileService.SettingsFile.GammaDir,
                        selector: (initialized, gammaDir) => (initialized, gammaDir)
                    )
                    .Where(x => x.initialized)
                    .Select(x => x.gammaDir)
                    .ToProperty(this, x => x.GammaPath)
                    .DisposeWith(d);

                _userLtxPath = this.WhenAnyValue(x => x.AnomalyPath)
                    .Select(x => Path.Join(x, "appdata", "user.ltx"))
                    .ToProperty(this, x => x.UserLtxPath)
                    .DisposeWith(d);
                _toolsReady = ToolsReadyCommand
                    .Select(x => x.CurlReady)
                    .ToProperty(this, x => x.ToolsReady)
                    .DisposeWith(d);

                IsRanWithWineCmd.Execute().Subscribe().DisposeWith(d);

                LongPathsStatusCmd.Execute().Subscribe().DisposeWith(d);
                this.WhenAnyValue(x => x._settingsFileService.SettingsInitialized)
                    .Where(settingsInitialized => settingsInitialized)
                    .Subscribe(_ =>
                    {
                        ToolsReadyCommand
                            .Execute()
                            .Where(x => x.CurlReady)
                            .Subscribe(_ =>
                            {
                                IsMo2InitializedCmd.Execute().Subscribe().DisposeWith(d);
                                IsMo2VersionDowngradedCmd.Execute().Subscribe().DisposeWith(d);

                                LocalGammaVersionsCmd.Execute().Subscribe().DisposeWith(d);

                                GetModDownloadExtractProgressVmsCmd
                                    .Execute()
                                    .Subscribe()
                                    .DisposeWith(d);
                                BackgroundCheckUpdatesCmd.Execute().Subscribe().DisposeWith(d);
                                GetLocalModsCmd.Execute().Subscribe().DisposeWith(d);
                            })
                            .DisposeWith(d);
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

    public bool DeleteReshadeDlls
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = true;

    public string VersionString
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool? IsMo2VersionDowngraded => _isMo2VersionDowngraded?.Value;

    public bool? LongPathsStatus => _longPathsStatus?.Value;

    public bool IsRanWithWine => _isRanWithWine?.Value ?? false;

    public bool IsMo2Initialized => _isMo2Initialized?.Value ?? false;

    public string? LocalGammaVersion => _localGammaVersion?.Value;

    public bool? UserLtxSetToFullscreenWine => _userLtxSetToFullscreenWine?.Value;

    public string? AnomalyPath => _anomalyPath?.Value;
    public string? UserLtxPath => _userLtxPath?.Value;
    public string? GammaPath => _gammaPath?.Value;

    public ReactiveCommand<Unit, bool> IsRanWithWineCmd { get; set; }
    public ReactiveCommand<Unit, Unit> EnableLongPathsOnWindowsCmd { get; set; }
    public ReactiveCommand<Unit, string> AddFoldersToWinDefenderExclusionCmd { get; set; }
    private ReactiveCommand<Unit, ToolsReadyRecord> ToolsReadyCommand { get; }
    public ReactiveCommand<Unit, Unit> FirstInstallInitializationCmd { get; }
    public ReactiveCommand<Unit, Unit> InstallUpdateGammaCmd { get; }
    public ReactiveCommand<Unit, Unit> PlayCmd { get; }
    public ReactiveCommand<string, Unit> OpenUrlCmd { get; }
    public ReactiveCommand<Unit, Unit> BackgroundCheckUpdatesCmd { get; }
    public ReactiveCommand<Unit, bool?> LongPathsStatusCmd { get; }
    public ReactiveCommand<Unit, bool?> IsMo2VersionDowngradedCmd { get; }
    public ReactiveCommand<Unit, bool> IsMo2InitializedCmd { get; }
    public ReactiveCommand<Unit, string?> LocalGammaVersionsCmd { get; }
    public ReactiveCommand<Unit, bool?> UserLtxSetToFullscreenWineCmd { get; }
    public ReactiveCommand<string, Unit> UserLtxReplaceFullscreenWithBorderlessFullscreen { get; }
    public ReactiveCommand<Unit, IList<ModListRecord>> GetModDownloadExtractProgressVmsCmd { get; }
    private ReactiveCommand<Unit, IList<ModListRecord>> GetLocalModsCmd { get; }
    public ViewModelActivator Activator { get; }
    public List<InstallType> InstallTypes { get; } = [InstallType.FullInstall, InstallType.Update];
    public InstallType SelectedInstallType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = InstallType.FullInstall;
    private ReactiveCommand<Unit, Unit> ShowSelectFolderCmd { get; }

    private int InitialFilteredListCount { get; set; }

    [GeneratedRegex(@".+(?<version>\d+\.\d+\.\d*.*)\.*")]
    private static partial Regex FileNameVersionRx();
}
