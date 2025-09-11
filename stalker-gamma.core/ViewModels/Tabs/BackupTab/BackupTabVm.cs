using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Enums;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public class BackupTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private Compressor _selectedCompressor;
    private CompressionLevel _selectedCompressionLevel;
    private readonly ObservableAsPropertyHelper<string?> _estimates;
    private CancellationTokenSource _backupCancellationTokenSource = new();
    private CancellationToken BackupCancellationToken => _backupCancellationTokenSource.Token;
    private readonly ObservableAsPropertyHelper<string> _gammaBackupFolder;
    private readonly ObservableAsPropertyHelper<string> _modsBackupPath;
    private readonly ObservableAsPropertyHelper<string> _fullBackupPath;
    private readonly ObservableAsPropertyHelper<DriveSpaceStats> _driveStats;
    private readonly ObservableAsPropertyHelper<string?> _driveSpaceStatsString;
    private readonly ObservableAsPropertyHelper<string?> _totalModsSpace;
    private readonly ReadOnlyObservableCollection<CompressionLevel> _compressionLevels;
    private readonly ObservableAsPropertyHelper<string?> _compressorToolTip;
    private readonly ReadOnlyObservableCollection<string> _modBackups;
    private string? _selectedModBackup;

    private readonly ObservableAsPropertyHelper<BackupType> _selectedBackup;
    private bool _modsIsChecked = true;
    private bool _fullIsChecked;

    public BackupTabVm(
        BackupTabProgressService backupTabProgressService,
        Queries.GetEstimate.Handler getEstimateHandler,
        Queries.GetAnomalyPath.Handler getAnomalyPathHandler,
        Queries.GetGammaPath.Handler getGammaPathHandler,
        Queries.GetDriveSpaceStats.Handler getDriveSpaceStatsHandler,
        Queries.CheckModsList.Handler checkModsListHandler,
        Queries.GetGammaBackupFolder.Handler getGammaBackupFolderHandler,
        Commands.RestoreBackup.Handler restoreBackupHandler,
        Commands.DeleteBackup.Handler deleteBackupHandler,
        Commands.CreateBackupFolders.Handler createBackupFolderHandler,
        Commands.CreateBackup.Handler createBackupHandler
    )
    {
        Activator = new ViewModelActivator();
        GammaFolderPath = Path.GetFullPath(Path.Combine(_dir, ".."));
        var backupsSrcList = new SourceList<string>();
        GetDriveSpaceStatsCmd = ReactiveCommand.CreateFromTask<string, DriveSpaceStats>(
            gammaFolder =>
                Task.Run(() =>
                    getDriveSpaceStatsHandler.Execute(
                        new Queries.GetDriveSpaceStats.Query(gammaFolder)
                    )
                )
        );
        _driveStats = GetDriveSpaceStatsCmd.ToProperty(this, x => x.DriveSpaceStats);
        _driveSpaceStatsString = this.WhenAnyValue(
                x => x.DriveSpaceStats,
                selector: dst =>
                    $"{dst?.TotalSpace / 1024 / 1024 / 1024}/{dst?.UsedSpace / 1024 / 1024 / 1024} GB"
            )
            .ToProperty(this, x => x.DriveStats);
        _totalModsSpace = this.WhenAnyValue(
                x => x.DriveSpaceStats,
                selector: dst => $"{dst?.ModsSize / 1024 / 1024 / 1024} GB"
            )
            .ToProperty(this, x => x.TotalModsSpace);
        backupsSrcList
            .Connect()
            .Transform(x => Path.GetFileName(x))
            .Sort(SortExpressionComparer<string>.Descending(x => x))
            .Bind(out _modBackups)
            .Subscribe();

        CheckModsList = ReactiveCommand.Create(() =>
            checkModsListHandler.Execute(
                new Queries.CheckModsList.Query(backupsSrcList, ModsBackupPath)
            )
        );

        CreateBackupFolders = ReactiveCommand.Create(() =>
            createBackupFolderHandler.Execute(
                new Commands.CreateBackupFolders.Command(
                    GammaBackupFolder,
                    ModsBackupPath,
                    FullBackupPath
                )
            )
        );

        ChangeGammaBackupDirectoryInteraction = new Interaction<Unit, string?>();
        ChangeGammaBackupDirectoryCmd = ReactiveCommand.CreateFromTask<string?>(async () =>
            await ChangeGammaBackupDirectoryInteraction.Handle(Unit.Default)
        );
        _gammaBackupFolder = ChangeGammaBackupDirectoryCmd
            .WhereNotNull()
            .ToProperty(
                this,
                x => x.GammaBackupFolder,
                initialValue: getGammaBackupFolderHandler.Execute()
            );

        _modsBackupPath = this.WhenAnyValue(
                x => x.GammaBackupFolder,
                selector: folder => Path.Join(folder, "Mods")
            )
            .ToProperty(this, x => x.ModsBackupPath);
        _fullBackupPath = this.WhenAnyValue(
                x => x.GammaBackupFolder,
                selector: folder => Path.Join(folder, "Full")
            )
            .ToProperty(this, x => x.FullBackupPath);

        CreateBackupFolders.Subscribe(_ => CheckModsList.Execute().Subscribe());
        CheckModsList.Subscribe(_ => GetDriveSpaceStatsCmd.Execute(GammaBackupFolder).Subscribe());

        this.WhenAnyValue(x => x.GammaBackupFolder, x => x.ModsBackupPath, x => x.FullBackupPath)
            .Subscribe(_ => CreateBackupFolders.Execute().Subscribe());

        _selectedBackup = this.WhenAnyValue(
                x => x.ModsIsChecked,
                x => x.FullIsChecked,
                selector: (mods, full) =>
                    mods ? BackupType.Mods
                    : full ? BackupType.Full
                    : BackupType.Mods
            )
            .ToProperty(this, x => x.SelectedBackup);
        var compressionLvlSrcList = new SourceList<CompressionLevel>();
        compressionLvlSrcList.AddRange(
            [CompressionLevel.None, CompressionLevel.Fast, CompressionLevel.Max]
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
                            if (!inner.Contains(CompressionLevel.Max))
                            {
                                inner.Add(CompressionLevel.Max);
                            }

                            break;
                        }
                        case Compressor.Zstd:
                        {
                            SelectedCompressionLevel = inner.First(x => x == CompressionLevel.Fast);
                            inner.Remove(CompressionLevel.None);
                            inner.Remove(CompressionLevel.Max);

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

        _compressorToolTip = this.WhenAnyValue(
                x => x.SelectedCompressor,
                selector: selComp =>
                    selComp switch
                    {
                        Compressor.Lzma2 =>
                            "The default 7zip compression method. Maximum compatibility.",
                        Compressor.Zstd =>
                            "A very fast non-standard compression method. Minimum compatibility, install 7zip z-standard from github to view archives created with this.",
                        _ => throw new ArgumentOutOfRangeException(nameof(selComp), selComp, null),
                    }
            )
            .ToProperty(this, x => x.CompressorToolTip);
        _selectedCompressor = Compressors.First(x => x == Compressor.Lzma2);
        this.WhenAnyValue(x => x.CompressionLevels)
            .Subscribe(lvls =>
                SelectedCompressionLevel = lvls.First(x => x == CompressionLevel.Fast)
            );
        BackupCmd = ReactiveCommand.CreateFromTask(() =>
            Task.Run(() =>
            {
                var dstArchive =
                    SelectedBackup switch
                    {
                        BackupType.Mods => Path.Join(
                            ModsBackupPath,
                            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                        ),
                        BackupType.Full => Path.Join(
                            FullBackupPath,
                            DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")
                        ),
                        _ => throw new ArgumentOutOfRangeException(),
                    } + ".7z";
                _backupCancellationTokenSource = new CancellationTokenSource();
                var anomalyPath = getAnomalyPathHandler.Execute()!.Replace(@"\\", "\\");
                var gammaPath = getGammaPathHandler.Execute();
                var commonDir = PathUtils.GetCommonDirectory(anomalyPath, gammaPath) ?? "";
                anomalyPath = anomalyPath.Replace(commonDir, "");
                gammaPath = gammaPath.Replace(commonDir, "");
                createBackupHandler
                    .ExecuteAsync(
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
                    )
                    .GetAwaiter()
                    .GetResult();
                return Task.FromResult(dstArchive);
            })
        );
        BackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.Message)
        );
        BackupCmd.Subscribe(_ => CheckModsList.Execute().Subscribe());

        var canCancel = BackupCmd.IsExecuting;
        CancelBackupCmd = ReactiveCommand.CreateFromTask(
            () => Task.Run(() => _backupCancellationTokenSource.Cancel()),
            canCancel
        );
        CancelBackupCmd.Subscribe(_ => CheckModsList.Execute().Subscribe());

        var canRestore = this.WhenAnyValue(
            x => x.GammaBackupFolder,
            x => x.SelectedModBackup,
            selector: (folder, backup) =>
                !string.IsNullOrWhiteSpace(folder) && !string.IsNullOrWhiteSpace(backup)
        );
        RestoreBackupCmd = ReactiveCommand.CreateFromTask(
            c =>
                Task.Run(() =>
                {
                    var gammaPath = getGammaPathHandler.Execute();
                    var anomalyPath = getAnomalyPathHandler.Execute()?.Replace(@"\\", "\\") ?? "";
                    var archivePath = Path.Join(ModsBackupPath, SelectedModBackup);
                    var workDir = PathUtils.GetCommonDirectory(anomalyPath, gammaPath)!;
                    var anomalyBinFolder = Path.Join(anomalyPath, "bin");
                    var gammaModsFolder = Path.Join(gammaPath, "mods");
                    restoreBackupHandler
                        .ExecuteAsync(
                            new Commands.RestoreBackup.Command(
                                archivePath,
                                ".",
                                workDir,
                                DirsToClean: [anomalyBinFolder, gammaModsFolder]
                            )
                        )
                        .GetAwaiter()
                        .GetResult();
                }),
            canRestore
        );
        RestoreBackupCmd.Subscribe(_ =>
            GetDriveSpaceStatsCmd.Execute(GammaBackupFolder).Subscribe()
        );

        var canDelete = this.WhenAnyValue(
            x => x.ModsBackupPath,
            x => x.SelectedModBackup,
            selector: (folder, backup) => File.Exists(Path.Join(folder, backup))
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

    public Interaction<string, Unit> AppendLineInteraction { get; }
    public Interaction<Unit, string?> ChangeGammaBackupDirectoryInteraction { get; }
    public ReactiveCommand<Unit, string> BackupCmd { get; }
    public ReactiveCommand<Unit, Unit> CancelBackupCmd { get; }
    public ReactiveCommand<(string BackupModPath, string BackupName), Unit> DeleteBackupCmd { get; }
    public string? CompressorToolTip => _compressorToolTip.Value;

    public IReadOnlyList<Compressor> Compressors { get; } = [Compressor.Lzma2, Compressor.Zstd];

    public ReadOnlyObservableCollection<string> ModBackups => _modBackups;
    public string? TotalModsSpace => _totalModsSpace.Value;

    public string? SelectedModBackup
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

    public string ModsBackupPath => _modsBackupPath.Value;
    private string FullBackupPath => _fullBackupPath.Value;

    private ReactiveCommand<Unit, Unit> CreateBackupFolders { get; }

    public bool ModsIsChecked
    {
        get => _modsIsChecked;
        set => this.RaiseAndSetIfChanged(ref _modsIsChecked, value);
    }

    public bool FullIsChecked
    {
        get => _fullIsChecked;
        set => this.RaiseAndSetIfChanged(ref _fullIsChecked, value);
    }
    public string GammaFolderPath { get; }
    public string GammaBackupFolder => _gammaBackupFolder.Value;
    private DriveSpaceStats DriveSpaceStats => _driveStats.Value;

    private ReactiveCommand<string, DriveSpaceStats> GetDriveSpaceStatsCmd { get; }
    public ReactiveCommand<Unit, Unit> RestoreBackupCmd { get; }

    public ViewModelActivator Activator { get; }
}

public record DriveSpaceStats(long TotalSpace, long UsedSpace, long ModsSize);

public enum BackupType
{
    Mods,
    Full,
}
