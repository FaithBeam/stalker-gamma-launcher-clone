using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public class BackupTabVm : ViewModelBase
{
    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;
    private Compressor _selectedCompressor;
    private CompressionLevel _selectedCompressionLevel;
    private readonly ObservableAsPropertyHelper<string?> _estimates;
    private CancellationTokenSource _backupCancellationTokenSource = new();
    private CancellationToken BackupCancellationToken => _backupCancellationTokenSource.Token;
    private readonly string _modsBackupPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GAMMA",
        "Mods"
    );
    private readonly string _fullBackupPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GAMMA",
        "Full"
    );

    private List<string> _modBackups;
    private string? _selectedModBackup;

    private readonly ObservableAsPropertyHelper<BackupType> _selectedBackup;
    private bool _modsIsChecked = true;
    private bool _fullIsChecked;

    public BackupTabVm(
        BackupService backupService,
        BackupTabProgressService backupTabProgressService
    )
    {
        if (!Directory.Exists(_modsBackupPath))
        {
            Directory.CreateDirectory(_modsBackupPath);
        }
        if (!Directory.Exists(_fullBackupPath))
        {
            Directory.CreateDirectory(_fullBackupPath);
        }

        _modBackups = Directory
            .EnumerateFiles(_modsBackupPath)
            .Select(x => Path.GetFileName(x))
            .ToList();

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
        CancelBackupCmd = ReactiveCommand.Create(() => _backupCancellationTokenSource.Cancel());
        BackupCmd = ReactiveCommand.CreateFromTask(() =>
        {
            _backupCancellationTokenSource = new CancellationTokenSource();
            return backupService.Backup(
                new BackupSettings(
                    SelectedBackup == BackupType.Full
                        ? [GetAnomalyPath()!, GetGammaPath()!]
                        : [Path.Join("..", "mods")],
                    SelectedBackup == BackupType.Full
                        ? Path.Join(_fullBackupPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"))
                        : Path.Join(_modsBackupPath, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")),
                    SelectedCompressionLevel,
                    SelectedCompressor,
                    BackupCancellationToken
                )
            );
        });
        BackupCmd.ThrownExceptions.Subscribe(x =>
            backupTabProgressService.UpdateProgress(x.Message)
        );

        // RestoreCmd = ReactiveCommand.CreateFromTask();

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
                                CompressionLevel.Fast => "≈ 1 minute, 36gb 8c/16t CPU",
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
    }

    public Interaction<string, Unit> AppendLineInteraction { get; }
    public ReactiveCommand<Unit, Unit> BackupCmd { get; }
    public ReactiveCommand<Unit, Unit> CancelBackupCmd { get; }

    // public ReactiveCommand<Unit, Unit> RestoreCmd { get; }
    public IReadOnlyList<Compressor> Compressors { get; } = [Compressor.Lzma2, Compressor.Zstd];

    public List<string> ModBackups => _modBackups;

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
}

public enum BackupType
{
    Mods,
    Full,
}
