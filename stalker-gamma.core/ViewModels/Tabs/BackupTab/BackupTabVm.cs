using System.Reactive;
using System.Reactive.Linq;
using ReactiveUI;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public class BackupTabVm : ViewModelBase
{
    private readonly BackupService _backupService;
    private readonly BackupTabProgressService _backupTabProgressService;
    private Compressor _selectedCompressor;
    private CompressionLevel _selectedCompressionLevel;
    private readonly ObservableAsPropertyHelper<string?> _estimates;

    public BackupTabVm(BackupService backupService, BackupTabProgressService backupTabProgressService)
    {
        _backupService = backupService;
        _backupTabProgressService = backupTabProgressService;
        _selectedCompressor = Compressors.First(x => x == Compressor.Lzma2);
        _selectedCompressionLevel = CompressionLevels.First(x => x == CompressionLevel.Fast);
        BackupCmd = ReactiveCommand.CreateFromTask(() =>
            _backupService.Backup(new BackupSettings(SelectedCompressionLevel, SelectedCompressor)));

        AppendLineInteraction = new Interaction<string, Unit>();
        backupTabProgressService.BackupProgressObservable.ObserveOn(RxApp.MainThreadScheduler).Select(x => x.Message)
            .WhereNotNull().Subscribe(async x => await AppendLineInteraction.Handle(x));

        _estimates = this
            .WhenAnyValue(x => x.SelectedCompressor, x => x.SelectedCompressionLevel,
                selector: (selComp, selLevel) => selComp switch
                {
                    Compressor.Lzma2 => selLevel switch
                    {
                        CompressionLevel.None => "changeme",
                        CompressionLevel.Fast => "changeme",
                        CompressionLevel.Max => "changeme",
                        _ => throw new ArgumentOutOfRangeException(nameof(selLevel), selLevel, null)
                    },
                    Compressor.Zstd => selLevel switch
                    {
                        CompressionLevel.None => "changeme",
                        CompressionLevel.Fast => "≈ 10 minutes, 65gb 7800X3D CPU",
                        CompressionLevel.Max => "changeme",
                        _ => throw new ArgumentOutOfRangeException(nameof(selLevel), selLevel, null)
                    },
                    _ => throw new ArgumentOutOfRangeException(nameof(selComp), selComp, null)
                }).ToProperty(this, x => x.Estimates);
    }

    public Interaction<string, Unit> AppendLineInteraction { get; }
    public ReactiveCommand<Unit, Unit> BackupCmd { get; }
    public IReadOnlyList<Compressor> Compressors { get; } = [Compressor.Lzma2, Compressor.Zstd];

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
}