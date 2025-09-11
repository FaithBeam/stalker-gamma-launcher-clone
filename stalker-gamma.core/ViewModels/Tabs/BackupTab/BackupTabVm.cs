using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public class BackupTabVm : ViewModelBase, IActivatableViewModel
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private Compressor _selectedCompressor;
    private CompressionLevel _selectedCompressionLevel;
    private readonly ObservableAsPropertyHelper<string?> _estimates;
    private CancellationTokenSource _backupCancellationTokenSource = new();
    private CancellationToken BackupCancellationToken => _backupCancellationTokenSource.Token;
    private string _gammaBackupFolder = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GAMMA"
    );
    private readonly ObservableAsPropertyHelper<string> _modsBackupPath;
    private readonly ObservableAsPropertyHelper<string> _fullBackupPath;
    private ObservableAsPropertyHelper<DriveSpaceStats> _driveStats;
    private readonly ObservableAsPropertyHelper<string?> _driveSpaceStatsString;
    private readonly ObservableAsPropertyHelper<string?> _totalModsSpace;

    private readonly ReadOnlyObservableCollection<string> _modBackups;
    private string? _selectedModBackup;

    private readonly ObservableAsPropertyHelper<BackupType> _selectedBackup;
    private bool _modsIsChecked = true;
    private bool _fullIsChecked;

    public BackupTabVm(
        BackupService backupService,
        BackupTabProgressService backupTabProgressService,
        RestoreBackup.Handler restoreBackupHandler
    )
    {
        Activator = new ViewModelActivator();
        GammaFolderPath = Path.GetFullPath(Path.Combine(_dir, ".."));
        var backupsSrcList = new SourceList<string>();
        GetDriveSpaceStatsCmd = ReactiveCommand.Create<string, DriveSpaceStats>(gammaFolder =>
        {
            var pathRoot = Path.GetPathRoot(gammaFolder);
            DriveInfo drive = new(pathRoot!);
            var backupSize = Directory
                .GetFiles(gammaFolder, "*.7z", SearchOption.AllDirectories)
                .Sum(x => new FileInfo(x).Length);
            return new DriveSpaceStats(
                drive.TotalSize,
                drive.TotalSize - drive.TotalFreeSpace,
                backupSize
            );
        });
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
        {
            backupsSrcList.Edit(inner =>
            {
                inner.Clear();
                inner.AddRange(Directory.GetFiles(ModsBackupPath));
            });
        });

        CreateBackupFolders = ReactiveCommand.Create(() =>
        {
            if (!Directory.Exists(GammaBackupFolder))
            {
                Directory.CreateDirectory(GammaBackupFolder);
            }

            if (!Directory.Exists(ModsBackupPath))
            {
                Directory.CreateDirectory(ModsBackupPath);
            }

            if (!Directory.Exists(FullBackupPath))
            {
                Directory.CreateDirectory(FullBackupPath);
            }
        });

        ChangeGammaBackupDirectoryInteraction = new Interaction<Unit, string?>();
        ChangeGammaBackupDirectoryCmd = ReactiveCommand.CreateFromTask<string?>(async () =>
            await ChangeGammaBackupDirectoryInteraction.Handle(Unit.Default)
        );
        ChangeGammaBackupDirectoryCmd
            .WhereNotNull()
            .Subscribe(x =>
            {
                GammaBackupFolder = x;
            });

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
        _selectedCompressor = Compressors.First(x => x == Compressor.Lzma2);
        _selectedCompressionLevel = CompressionLevels.First(x => x == CompressionLevel.Fast);

        BackupCmd = ReactiveCommand.CreateFromTask(() =>
        {
            _backupCancellationTokenSource = new CancellationTokenSource();
            return backupService.Backup(
                new BackupSettings(
                    SelectedBackup == BackupType.Full
                        ? [GetAnomalyPath()!, GetGammaPath()!]
                        :
                        [
#if DEBUG
                            Path.Join("..", "net9.0", "version.txt"),
                            Path.Join("..", "net9.0", "mods.txt"),
#else
                            Path.Join("..", ".Grok's Modpack Installer", "version.txt"),
                            Path.Join("..", ".Grok's Modpack Installer", "mods.txt"),
#endif
                            Path.Join("..", "mods"),
                        ],
                    SelectedBackup == BackupType.Full
                        ? Path.Join(FullBackupPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))
                        : Path.Join(ModsBackupPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")),
                    SelectedCompressionLevel,
                    SelectedCompressor,
                    BackupCancellationToken
                )
            );
        });
        BackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.Message)
        );
        BackupCmd.Subscribe(_ => CheckModsList.Execute().Subscribe());

        var canCancel = BackupCmd.IsExecuting;
        CancelBackupCmd = ReactiveCommand.Create(
            () => _backupCancellationTokenSource.Cancel(),
            canCancel
        );

        var canRestore = this.WhenAnyValue(
            x => x.GammaBackupFolder,
            x => x.SelectedModBackup,
            selector: (folder, backup) =>
                !string.IsNullOrWhiteSpace(folder) && !string.IsNullOrWhiteSpace(backup)
        );
        RestoreBackupCmd = ReactiveCommand.CreateFromTask<RestoreBackup.Command>(
            restoreBackupHandler.ExecuteAsync,
            canRestore
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
                    var joined = Path.Join(
                        pathToBackupToDelete.BackupModPath,
                        pathToBackupToDelete.BackupName
                    );
                    if (File.Exists(joined))
                    {
                        File.Delete(joined);
                    }
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
                    selBackup switch
                    {
                        BackupType.Mods => selComp switch
                        {
                            Compressor.Lzma2 => "",
                            Compressor.Zstd => selLevel switch
                            {
                                CompressionLevel.None => "",
                                CompressionLevel.Fast => "≈ 5 minute, 34gb 8c/16t CPU",
                                CompressionLevel.Max => "≈ 80 minutes, 20gb 8c/16t CPU",
                                _ => throw new ArgumentOutOfRangeException(
                                    nameof(selLevel),
                                    selLevel,
                                    null
                                ),
                            },
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(selComp),
                                selComp,
                                null
                            ),
                        },
                        BackupType.Full => selComp switch
                        {
                            Compressor.Lzma2 => selLevel switch
                            {
                                CompressionLevel.None => "changeme",
                                CompressionLevel.Fast => "≈ 15 minutes, 54gb 8c/16t CPU",
                                CompressionLevel.Max => "≈ 50 minutes, 50gb 8c/16t CPU",
                                _ => throw new ArgumentOutOfRangeException(
                                    nameof(selLevel),
                                    selLevel,
                                    null
                                ),
                            },
                            Compressor.Zstd => selLevel switch
                            {
                                CompressionLevel.None => "changeme",
                                CompressionLevel.Fast => "≈ 10 minutes, 65gb 8c/16t CPU",
                                CompressionLevel.Max => "≈ 135 minutes, 42gb 8c/16t CPU",
                                _ => throw new ArgumentOutOfRangeException(
                                    nameof(selLevel),
                                    selLevel,
                                    null
                                ),
                            },
                            _ => throw new ArgumentOutOfRangeException(
                                nameof(selComp),
                                selComp,
                                null
                            ),
                        },
                        _ => throw new ArgumentOutOfRangeException(
                            nameof(selBackup),
                            selBackup,
                            null
                        ),
                    }
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
    public ReactiveCommand<Unit, Unit> BackupCmd { get; }
    public ReactiveCommand<Unit, Unit> CancelBackupCmd { get; }
    public ReactiveCommand<(string BackupModPath, string BackupName), Unit> DeleteBackupCmd { get; }

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

    public IReadOnlyList<CompressionLevel> CompressionLevels { get; } =
        [CompressionLevel.None, CompressionLevel.Fast, CompressionLevel.Max];

    public CompressionLevel SelectedCompressionLevel
    {
        get => _selectedCompressionLevel;
        set => this.RaiseAndSetIfChanged(ref _selectedCompressionLevel, value);
    }

    public string? Estimates => _estimates.Value;

    public ReactiveCommand<Unit, Unit> CheckModsList { get; }

    public ReactiveCommand<Unit, string?> ChangeGammaBackupDirectoryCmd { get; }

    public string? DriveStats => _driveSpaceStatsString.Value;

    private string? GetAnomalyPath()
    {
        var modOrganizerIniPath = Path.Join(_dir, "..", "ModOrganizer.ini");
        if (!File.Exists(modOrganizerIniPath))
        {
            return null;
        }

        var modOrganizerIniTxt = File.ReadAllText(modOrganizerIniPath);
        return Regex
            .Match(
                modOrganizerIniTxt,
                @"\r?\ngamePath=@ByteArray\((.*)\)\r?\n",
                RegexOptions.IgnoreCase
            )
            .Groups[1]
            .Value;
    }

    private string? GetGammaPath()
    {
        var gammaPath = Path.GetFullPath(Path.Join(_dir, ".."));
        return gammaPath;
    }

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
    public string GammaBackupFolder
    {
        get => _gammaBackupFolder;
        set => this.RaiseAndSetIfChanged(ref _gammaBackupFolder, value);
    }
    private DriveSpaceStats DriveSpaceStats => _driveStats.Value;

    private ReactiveCommand<string, DriveSpaceStats> GetDriveSpaceStatsCmd { get; }
    public ReactiveCommand<RestoreBackup.Command, Unit> RestoreBackupCmd { get; }

    public ViewModelActivator Activator { get; }
}

public record DriveSpaceStats(long TotalSpace, long UsedSpace, long ModsSize);

public enum BackupType
{
    Mods,
    Full,
}
