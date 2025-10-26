using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Enums;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Models;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Services;
using stalker_gamma.core.ViewModels.Tabs.Queries;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public interface IBackupTabVm
{
    double BackupsListColWidth { get; set; }
    string ToggleBackupsListBtnTxt { get; }
    IIsBusyService IsBusyService { get; }
    Interaction<string, Unit> AppendLineInteraction { get; }
    ReactiveCommand<Unit, Unit> OpenBackupFolderCommand { get; }
    Interaction<Unit, string?> ChangeGammaBackupDirectoryInteraction { get; }
    ReactiveCommand<Unit, Task<string?>> BackupCmd { get; }
    ReactiveCommand<Unit, Unit> CancelBackupCmd { get; }
    ReactiveCommand<(string BackupModPath, string BackupName), Unit> DeleteBackupCmd { get; }
    ReactiveCommand<Unit, double> ToggleShowBackupsListCmd { get; }
    IReadOnlyList<Compressor> Compressors { get; }
    ReadOnlyObservableCollection<ModBackupVm> ModBackups { get; }
    string? TotalModsSpace { get; }
    ModBackupVm? SelectedModBackup { get; set; }
    Compressor SelectedCompressor { get; set; }
    ReadOnlyObservableCollection<CompressionLevel> CompressionLevels { get; }
    CompressionLevel SelectedCompressionLevel { get; set; }
    string? Estimates { get; }
    ReactiveCommand<Unit, Unit> CheckModsList { get; }
    ReactiveCommand<Unit, string?> ChangeGammaBackupDirectoryCmd { get; }
    string? DriveStats { get; }
    BackupType SelectedBackup { get; }
    string PartialBackupPath { get; }
    bool PartialIsChecked { get; set; }
    bool FullIsChecked { get; set; }
    string GammaBackupFolder { get; }
    ReactiveCommand<Unit, Unit> RestoreBackupCmd { get; }
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

public partial class BackupTabVm : ViewModelBase, IActivatableViewModel, IBackupTabVm
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    private Compressor _selectedCompressor;
    private CompressionLevel _selectedCompressionLevel;
    private readonly ObservableAsPropertyHelper<string?> _estimates;
    private CancellationTokenSource _backupCancellationTokenSource = new();
    private CancellationToken BackupCancellationToken => _backupCancellationTokenSource.Token;
    private readonly ObservableAsPropertyHelper<string> _gammaBackupFolder;
    private readonly ObservableAsPropertyHelper<string> _partialBackupPath;
    private readonly ObservableAsPropertyHelper<string> _fullBackupPath;
    private readonly ObservableAsPropertyHelper<DriveSpaceStats> _driveStats;
    private readonly ObservableAsPropertyHelper<string?> _driveSpaceStatsString;
    private readonly ObservableAsPropertyHelper<string?> _totalModsSpace;
    private readonly ReadOnlyObservableCollection<CompressionLevel> _compressionLevels;
    private readonly ReadOnlyObservableCollection<ModBackupVm> _modBackups;
    private ModBackupVm? _selectedModBackup;
    private double _backupsListColWidth;
    private readonly ObservableAsPropertyHelper<string> _toggleBackupsListBtnTxt;

    private readonly ObservableAsPropertyHelper<BackupType> _selectedBackup;
    private bool _partialIsChecked = true;
    private bool _fullIsChecked;

    public BackupTabVm(
        GlobalSettings globalSettings,
        IIsBusyService isBusyService,
        BackupTabProgressService backupTabProgressService,
        Queries.GetEstimate.Handler getEstimateHandler,
        GetAnomalyPath.Handler getAnomalyPathHandler,
        GetGammaPath.Handler getGammaPathHandler,
        Queries.GetDriveSpaceStats.Handler getDriveSpaceStatsHandler,
        Queries.CheckModsList.Handler checkModsListHandler,
        GetGammaBackupFolder.Handler getGammaBackupFolderHandler,
        Queries.OpenBackupFolder.Handler openBackupFolderHandler,
        Commands.UpdateGammaBackupPathInAppSettings.Handler updateGammaBackupPathInAppSettingsHandler,
        Commands.RestoreBackup.Handler restoreBackupHandler,
        Commands.DeleteBackup.Handler deleteBackupHandler,
        Commands.CreateBackupFolders.Handler createBackupFolderHandler,
        Commands.CreateBackup.Handler createBackupHandler,
        Tabs.Queries.GetStalkerGammaLastCommit.Handler getStalkerGammaLastCommitHandler
    )
    {
        IsBusyService = isBusyService;
        Activator = new ViewModelActivator();

        ToggleShowBackupsListCmd = ReactiveCommand.Create(() =>
        {
            BackupsListColWidth = BackupsListColWidth == 0 ? 400 : 0;
            return BackupsListColWidth;
        });
        _toggleBackupsListBtnTxt = ToggleShowBackupsListCmd
            .Select(width => width == 0 ? "<" : ">")
            .ToProperty(this, x => x.ToggleBackupsListBtnTxt, initialValue: "<");

        OpenBackupFolderCommand = ReactiveCommand.Create(openBackupFolderHandler.Execute);

        var backupsSrcList = new SourceList<string>();
        GetDriveSpaceStatsCmd = ReactiveCommand.CreateFromTask<string, DriveSpaceStats>(
            gammaFolder =>
                Task.Run(() =>
                    getDriveSpaceStatsHandler.Execute(
                        new Queries.GetDriveSpaceStats.Query(gammaFolder)
                    )
                )
        );
        GetDriveSpaceStatsCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );
        _driveStats = GetDriveSpaceStatsCmd.ToProperty(this, x => x.DriveSpaceStats);
        _driveSpaceStatsString = this.WhenAnyValue(
                x => x.DriveSpaceStats,
                selector: dst =>
                    $"{dst?.TotalSpace / 1024 / 1024 / 1024 - dst?.UsedSpace / 1024 / 1024 / 1024} GB free of {dst?.TotalSpace / 1024 / 1024 / 1024} GB"
            )
            .ToProperty(this, x => x.DriveStats);
        _totalModsSpace = this.WhenAnyValue(
                x => x.DriveSpaceStats,
                selector: dst => $"Total backups size {dst?.ModsSize / 1024 / 1024 / 1024} GB"
            )
            .ToProperty(this, x => x.TotalModsSpace);
        backupsSrcList
            .Connect()
            .Filter(path => MyRegex().Match(Path.GetFileName(path)).Success)
            .Transform(path =>
            {
                var fileName = Path.GetFileName(path);
                var fileSize = new FileInfo(path).Length;
                var match = MyRegex().Match(fileName);
                var date = match.Success
                    ? new DateTime(
                        int.Parse(match.Groups["year"].Value),
                        int.Parse(match.Groups["month"].Value),
                        int.Parse(match.Groups["day"].Value),
                        int.Parse(match.Groups["hour"].Value),
                        int.Parse(match.Groups["minute"].Value),
                        int.Parse(match.Groups["second"].Value)
                    ).ToString(CultureInfo.CurrentCulture)
                    : "N/A";
                var compressionMethod = match.Groups["compression"].Value;

                return new ModBackupVm(
                    fileName,
                    fileSize,
                    path,
                    int.Parse(match.Groups["gammaVersion"].Value),
                    match.Groups["gammaHash"].Value,
                    date,
                    compressionMethod
                );
            })
            .Sort(SortExpressionComparer<ModBackupVm>.Descending(x => x.FileName))
            .Bind(out _modBackups)
            .Subscribe();

        CheckModsList = ReactiveCommand.Create(() =>
            checkModsListHandler.Execute(
                new Queries.CheckModsList.Query(backupsSrcList, PartialBackupPath)
            )
        );
        CheckModsList.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );

        CreateBackupFolders = ReactiveCommand.Create(() =>
            createBackupFolderHandler.Execute(
                new Commands.CreateBackupFolders.Command(
                    GammaBackupFolder,
                    PartialBackupPath,
                    FullBackupPath
                )
            )
        );
        CreateBackupFolders.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );

        ChangeGammaBackupDirectoryInteraction = new Interaction<Unit, string?>();
        var canChangeBackupDirectory = this.WhenAnyValue(
            x => x.IsBusyService.IsBusy,
            selector: isBusy => !isBusy
        );
        ChangeGammaBackupDirectoryCmd = ReactiveCommand.CreateFromTask<string?>(
            async () => await ChangeGammaBackupDirectoryInteraction.Handle(Unit.Default),
            canChangeBackupDirectory
        );
        ChangeGammaBackupDirectoryCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );
        ChangeGammaBackupDirectoryCmd
            .WhereNotNull()
            .Subscribe(async newPath =>
                await updateGammaBackupPathInAppSettingsHandler.ExecuteAsync(
                    new Commands.UpdateGammaBackupPathInAppSettings.Command(newPath)
                )
            );
        _gammaBackupFolder = ChangeGammaBackupDirectoryCmd
            .WhereNotNull()
            .ToProperty(
                this,
                x => x.GammaBackupFolder,
                initialValue: getGammaBackupFolderHandler.Execute()
            );

        _partialBackupPath = this.WhenAnyValue(
                x => x.GammaBackupFolder,
                selector: folder => Path.Join(folder, "Partial")
            )
            .ToProperty(this, x => x.PartialBackupPath);
        _fullBackupPath = this.WhenAnyValue(
                x => x.GammaBackupFolder,
                selector: folder => Path.Join(folder, "Full")
            )
            .ToProperty(this, x => x.FullBackupPath);

        CreateBackupFolders.Subscribe(_ => CheckModsList.Execute().Subscribe());
        CheckModsList.Subscribe(_ => GetDriveSpaceStatsCmd.Execute(GammaBackupFolder).Subscribe());

        this.WhenAnyValue(x => x.GammaBackupFolder, x => x.PartialBackupPath, x => x.FullBackupPath)
            .Subscribe(_ => CreateBackupFolders.Execute().Subscribe());

        _selectedBackup = this.WhenAnyValue(
                x => x.PartialIsChecked,
                x => x.FullIsChecked,
                selector: (mods, full) =>
                    mods ? BackupType.Partial
                    : full ? BackupType.Full
                    : BackupType.Partial
            )
            .ToProperty(this, x => x.SelectedBackup);
        var compressionLvlSrcList = new SourceList<CompressionLevel>();
        compressionLvlSrcList.AddRange(
            [CompressionLevel.None, CompressionLevel.Fast, CompressionLevel.Ultra]
        );

        compressionLvlSrcList
            .Connect()
            .Sort(SortExpressionComparer<CompressionLevel>.Ascending(x => x))
            .Bind(out _compressionLevels)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedCompressor, selector: selComp => selComp)
            .WhereNotNull()
            .Subscribe(selComp =>
            {
                compressionLvlSrcList.Edit(inner =>
                {
                    switch (selComp)
                    {
                        case Compressor.Lzma2:
                        {
                            if (!inner.Contains(CompressionLevel.None))
                            {
                                inner.Add(CompressionLevel.None);
                            }
                            if (!inner.Contains(CompressionLevel.Fast))
                            {
                                inner.Add(CompressionLevel.Fast);
                            }
                            if (!inner.Contains(CompressionLevel.Ultra))
                            {
                                inner.Add(CompressionLevel.Ultra);
                            }

                            break;
                        }
                        case Compressor.Zstd:
                        {
                            SelectedCompressionLevel = inner.First(x => x == CompressionLevel.Fast);
                            inner.Remove(CompressionLevel.None);
                            inner.Remove(CompressionLevel.Ultra);

                            if (!inner.Contains(CompressionLevel.Fast))
                            {
                                inner.Add(CompressionLevel.Fast);
                            }

                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException(nameof(selComp), selComp, null);
                    }
                });
            });

        _selectedCompressor = Compressors.First(x => x == Compressor.Lzma2);
        this.WhenAnyValue(x => x.CompressionLevels)
            .Subscribe(lvls =>
                SelectedCompressionLevel = lvls.First(x => x == CompressionLevel.None)
            );
        var canBackup = this.WhenAnyValue(x => x.IsBusyService.IsBusy, selector: isBusy => !isBusy);
        BackupCmd = ReactiveCommand.CreateFromTask(
            () =>
                Task.Run(async () =>
                {
                    var now = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss");
                    var gammaVersion = Path.Exists(Path.Join(_dir, "version.txt"))
                        ? File.ReadAllText(Path.Join(_dir, "version.txt")).Trim()
                        : "NA";
                    var gammaHash = (
                        await getStalkerGammaLastCommitHandler.ExecuteAsync(
                            new GetStalkerGammaLastCommit.Query(
                                Path.Join(_dir, "resources", "Stalker_GAMMA")
                            )
                        )
                    )[..9];
                    now = $"{now}+{gammaVersion}.{gammaHash}";
                    var dstArchive =
                        SelectedBackup switch
                        {
                            BackupType.Partial => Path.Join(PartialBackupPath, now),
                            BackupType.Full => Path.Join(FullBackupPath, now),
                            _ => throw new ArgumentOutOfRangeException(),
                        }
                        + SelectedCompressor switch
                        {
                            Compressor.Lzma2 => ".lzma2",
                            Compressor.Zstd => ".zstd",
                            _ => throw new ArgumentOutOfRangeException(),
                        }
                        + ".7z";
                    _backupCancellationTokenSource = new CancellationTokenSource();
                    var anomalyPath = getAnomalyPathHandler.Execute()!.Replace(@"\\", "\\");
                    var gammaPath = getGammaPathHandler.Execute();
                    var commonDir = PathUtils.GetCommonDirectory(anomalyPath, gammaPath) ?? "";
                    anomalyPath = anomalyPath.Replace(commonDir, "").TrimStart('\\');
                    gammaPath = gammaPath.Replace(commonDir, "").TrimStart('\\');
                    try
                    {
                        await createBackupHandler.ExecuteAsync(
                            new Commands.CreateBackup.Command(
                                SelectedBackup == BackupType.Full
                                    ? [anomalyPath, gammaPath]
                                    :
                                    [
                                        Path.Join(anomalyPath, "bin"),
#if DEBUG
                                        Path.Join(gammaPath, "net9.0", "*.txt"),
#else
                                        Path.Join(gammaPath, ".Grok's Modpack Installer", "*.txt"),
#endif
                                        Path.Join(gammaPath, "mods"),
                                        Path.Join(gammaPath, "profiles"),
                                    ],
                                dstArchive,
                                SelectedCompressionLevel,
                                SelectedCompressor,
                                BackupCancellationToken,
                                WorkingDirectory: Path.GetFullPath(commonDir)
                            )
                        );
                    }
                    catch (OperationCanceledException)
                    {
                        backupTabProgressService.UpdateProgress("Backup Cancelled");
                        // delete archive if the backup was cancelled
                        if (File.Exists(dstArchive))
                        {
                            File.Delete(dstArchive);
                            dstArchive = null;
                        }
                    }

                    return Task.FromResult(dstArchive);
                }),
            canBackup
        );
        BackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.Message)
        );
        BackupCmd.WhereNotNull().Subscribe(_ => CheckModsList.Execute().Subscribe());
        BackupCmd.IsExecuting.Subscribe(isExecuting => IsBusyService.IsBusy = isExecuting);

        var canCancel = BackupCmd.IsExecuting;
        CancelBackupCmd = ReactiveCommand.CreateFromTask(
            () => Task.Run(() => _backupCancellationTokenSource.Cancel()),
            canCancel
        );
        CancelBackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );

        var canRestore = this.WhenAnyValue(
            x => x.GammaBackupFolder,
            x => x.SelectedModBackup,
            x => x.IsBusyService.IsBusy,
            selector: (folder, backup, isBusy) =>
                !string.IsNullOrWhiteSpace(folder)
                && backup is not null
                && !string.IsNullOrWhiteSpace(backup.FileName)
                && !isBusy
        );
        RestoreBackupCmd = ReactiveCommand.CreateFromTask(
            c =>
                Task.Run(
                    async () =>
                    {
                        backupTabProgressService.UpdateProgress(
                            """

                            ====== Begin restore ======

                            This may take a while, potentially 20 minutes depending on settings, and there will not be restore progress reported by 7zip.
                            Be patient.
                            """
                        );
                        var gammaPath = getGammaPathHandler.Execute().TrimStart('\\');
                        var anomalyPath =
                            getAnomalyPathHandler.Execute()?.Replace(@"\\", "\\").TrimStart('\\')
                            ?? "";
                        var archivePath = Path.Join(PartialBackupPath, SelectedModBackup!.FileName);
                        var workDir = PathUtils.GetCommonDirectory(anomalyPath, gammaPath)!;
                        var anomalyBinFolder = Path.Join(anomalyPath, "bin");
                        var gammaModsFolder = Path.Join(gammaPath, "mods");
                        await restoreBackupHandler.ExecuteAsync(
                            new Commands.RestoreBackup.Command(
                                archivePath,
                                ".",
                                workDir,
                                DirsToClean: [anomalyBinFolder, gammaModsFolder]
                            )
                        );
                        backupTabProgressService.UpdateProgress(
                            """

                            ====== Restore complete ======

                            """
                        );
                    },
                    c
                ),
            canRestore
        );
        RestoreBackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );
        RestoreBackupCmd.Subscribe(_ =>
            GetDriveSpaceStatsCmd.Execute(GammaBackupFolder).Subscribe()
        );
        RestoreBackupCmd.IsExecuting.Subscribe(isExecuting => IsBusyService.IsBusy = isExecuting);

        var canDelete = this.WhenAnyValue(
            x => x.PartialBackupPath,
            x => x.SelectedModBackup,
            x => x.IsBusyService.IsBusy,
            selector: (folder, backup, isBusy) =>
                backup is not null && File.Exists(Path.Join(folder, backup.FileName)) && !isBusy
        );
        DeleteBackupCmd = ReactiveCommand.CreateFromTask<(string BackupModPath, string BackupName)>(
            pathToBackupToDelete =>
                Task.Run(() =>
                {
                    deleteBackupHandler.Execute(
                        new Commands.DeleteBackup.Command(
                            pathToBackupToDelete.BackupModPath,
                            pathToBackupToDelete.BackupName
                        )
                    );
                }),
            canDelete
        );
        DeleteBackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.ToString())
        );
        DeleteBackupCmd.Subscribe(_ => CheckModsList.Execute().Subscribe());
        DeleteBackupCmd.IsExecuting.Subscribe(isExecuting => IsBusyService.IsBusy = isExecuting);

        AppendLineInteraction = new Interaction<string, Unit>();
        backupTabProgressService
            .BackupProgressObservable.ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x.Message)
            .WhereNotNull()
            .Subscribe(async x => await AppendLineInteraction.Handle(x));

        _estimates = this.WhenAnyValue(
                x => x.SelectedCompressor,
                x => x.SelectedCompressionLevel,
                x => x.SelectedBackup,
                selector: (selComp, selLevel, selBackup) =>
                    getEstimateHandler.Execute(
                        new Queries.GetEstimate.Query(selComp, selLevel, selBackup)
                    )
            )
            .ToProperty(this, x => x.Estimates);

        this.WhenActivated(
            (CompositeDisposable d) =>
            {
                CreateBackupFolders.Execute().Subscribe();
                CheckModsList.Execute().Subscribe();
                GetDriveSpaceStatsCmd.Execute(GammaBackupFolder).Subscribe();
            }
        );
    }

    public double BackupsListColWidth
    {
        get => _backupsListColWidth;
        set => this.RaiseAndSetIfChanged(ref _backupsListColWidth, value);
    }

    public string ToggleBackupsListBtnTxt => _toggleBackupsListBtnTxt.Value;

    public IIsBusyService IsBusyService { get; }
    public Interaction<string, Unit> AppendLineInteraction { get; }
    public ReactiveCommand<Unit, Unit> OpenBackupFolderCommand { get; }
    public Interaction<Unit, string?> ChangeGammaBackupDirectoryInteraction { get; }
    public ReactiveCommand<Unit, Task<string?>> BackupCmd { get; }
    public ReactiveCommand<Unit, Unit> CancelBackupCmd { get; }
    public ReactiveCommand<(string BackupModPath, string BackupName), Unit> DeleteBackupCmd { get; }
    public ReactiveCommand<Unit, double> ToggleShowBackupsListCmd { get; }

    public IReadOnlyList<Compressor> Compressors { get; } = [Compressor.Lzma2, Compressor.Zstd];

    public ReadOnlyObservableCollection<ModBackupVm> ModBackups => _modBackups;
    public string? TotalModsSpace => _totalModsSpace.Value;

    public ModBackupVm? SelectedModBackup
    {
        get => _selectedModBackup;
        set => this.RaiseAndSetIfChanged(ref _selectedModBackup, value);
    }

    public Compressor SelectedCompressor
    {
        get => _selectedCompressor;
        set => this.RaiseAndSetIfChanged(ref _selectedCompressor, value);
    }

    public ReadOnlyObservableCollection<CompressionLevel> CompressionLevels => _compressionLevels;

    public CompressionLevel SelectedCompressionLevel
    {
        get => _selectedCompressionLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedCompressionLevel, value);
    }

    public string? Estimates => _estimates.Value;

    public ReactiveCommand<Unit, Unit> CheckModsList { get; }

    public ReactiveCommand<Unit, string?> ChangeGammaBackupDirectoryCmd { get; }

    public string? DriveStats => _driveSpaceStatsString.Value;

    public BackupType SelectedBackup => _selectedBackup.Value;

    public string PartialBackupPath => _partialBackupPath.Value;
    private string FullBackupPath => _fullBackupPath.Value;

    private ReactiveCommand<Unit, Unit> CreateBackupFolders { get; }

    public bool PartialIsChecked
    {
        get => _partialIsChecked;
        set => this.RaiseAndSetIfChanged(ref _partialIsChecked, value);
    }

    public bool FullIsChecked
    {
        get => _fullIsChecked;
        set => this.RaiseAndSetIfChanged(ref _fullIsChecked, value);
    }

    public string GammaBackupFolder => _gammaBackupFolder.Value;
    private DriveSpaceStats DriveSpaceStats => _driveStats.Value;

    private ReactiveCommand<string, DriveSpaceStats> GetDriveSpaceStatsCmd { get; }
    public ReactiveCommand<Unit, Unit> RestoreBackupCmd { get; }

    public ViewModelActivator Activator { get; }

    [GeneratedRegex(
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})-(?<hour>\d{2})-(?<minute>\d{2})-(?<second>\d{2})\+(?<gammaVersion>\d+)\.(?<gammaHash>[\d\w]+)\.(?<compression>(lzma2|zstd)).(7z)$"
    )]
    private static partial Regex MyRegex();
}
