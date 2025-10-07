using System.Reactive.Linq;
using CliWrap.EventStream;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Enums;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Services;

namespace stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

public static class CreateBackup
{
    public sealed record Command(
        string[] BackupPaths,
        string Destination,
        CompressionLevel CompressionLevel,
        Compressor Compressor,
        CancellationToken CancellationToken,
        string? WorkingDirectory
    );

    public sealed class Handler(BackupTabProgressService progress)
    {
        public async Task ExecuteAsync(Command c)
        {
            await ArchiveUtility
                .Archive(
                    c.BackupPaths,
                    c.Destination,
                    c.Compressor.ToString().ToLower(),
                    GetCompressionLevel(c.Compressor, c.CompressionLevel),
                    ["downloads"],
                    c.CancellationToken,
                    c.WorkingDirectory
                )
                .ForEachAsync(cmdEvent =>
                {
                    switch (cmdEvent)
                    {
                        case StandardOutputCommandEvent standardOutput:
                            progress.UpdateProgress(standardOutput.Text);
                            break;
                        case StandardErrorCommandEvent standardError:
                            progress.UpdateProgress(standardError.Text);
                            break;
                    }
                });
        }
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
                CompressionLevel.Ultra => "9",
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
                CompressionLevel.Ultra => "22",
                _ => throw new ArgumentOutOfRangeException(
                    nameof(compressionLevel),
                    compressionLevel,
                    null
                ),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(compressor), compressor, null),
        };
}
