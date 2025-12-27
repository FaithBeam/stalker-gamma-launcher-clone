using System;
using System.Threading;
using System.Threading.Tasks;
using stalker_gamma_gui.ViewModels.Tabs.BackupTab.Enums;
using stalker_gamma_gui.ViewModels.Tabs.BackupTab.Services;
using stalker_gamma.core.Utilities;

namespace stalker_gamma_gui.ViewModels.Tabs.BackupTab.Commands;

public static class CreateBackup
{
    public sealed record Command(
        string[] BackupPaths,
        string Destination,
        CompressionLevel CompressionLevel,
        Compressor Compressor,
        string? WorkingDirectory,
        CancellationToken CancellationToken
    );

    public sealed class Handler(BackupTabProgressService progress)
    {
        public async Task ExecuteAsync(Command c)
        {
            await SevenZipUtility.Archive(
                c.BackupPaths,
                c.Destination,
                c.Compressor.ToString().ToLower(),
                GetCompressionLevel(c.Compressor, c.CompressionLevel),
                ["downloads"],
                c.WorkingDirectory,
                txtProgress: progress.UpdateProgress,
                cancellationToken: c.CancellationToken
            );
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
