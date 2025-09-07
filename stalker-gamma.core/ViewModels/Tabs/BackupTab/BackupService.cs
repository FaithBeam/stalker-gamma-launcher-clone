using System.Reactive.Linq;
using CliWrap.EventStream;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab;

public enum Compressor
{
    Lzma2,
    Zstd,
}

public enum CompressionLevel
{
    None,
    Fast,
    Max,
}

public record BackupSettings(
    string[] BackupPaths,
    string Destination,
    CompressionLevel CompressionLevel,
    Compressor Compressor,
    CancellationToken CancellationToken
);

public class BackupService
{
    private readonly BackupTabProgressService _progress;

    private readonly string _backupPath = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "GAMMA",
        "Backups"
    );

    public BackupService(BackupTabProgressService progress)
    {
        _progress = progress;
        if (!Directory.Exists(_backupPath))
        {
            Directory.CreateDirectory(_backupPath);
        }
    }

    public List<string> GetBackups() =>
        Directory.Exists(_backupPath) ? Directory.GetFiles(_backupPath).ToList() : [];

    public async Task Backup(BackupSettings backupSettings)
    {
        await ArchiveUtility
            .Archive(
                backupSettings.BackupPaths,
                backupSettings.Destination,
                backupSettings.Compressor.ToString().ToLower(),
                GetCompressionLevel(backupSettings.Compressor, backupSettings.CompressionLevel),
                ["downloads"],
                backupSettings.CancellationToken
            )
            .ForEachAsync(cmdEvent =>
            {
                switch (cmdEvent)
                {
                    case StandardOutputCommandEvent standardOutput:
                        _progress.UpdateProgress(standardOutput.Text);
                        break;
                    case StandardErrorCommandEvent standardError:
                        _progress.UpdateProgress(standardError.Text);
                        break;
                }
            });
    }

    private static string GetCompressionLevel(
        Compressor compressor,
        CompressionLevel compressionLevel
    ) =>
        compressor switch
        {
            Compressor.Lzma2 => compressionLevel switch
            {
                CompressionLevel.None => "0",
                CompressionLevel.Fast => "1",
                CompressionLevel.Max => "9",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(compressionLevel),
                    compressionLevel,
                    null
                ),
            },
            Compressor.Zstd => compressionLevel switch
            {
                CompressionLevel.None => "0",
                CompressionLevel.Fast => "1",
                CompressionLevel.Max => "22",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(compressionLevel),
                    compressionLevel,
                    null
                ),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(compressor), compressor, null),
        };
}
