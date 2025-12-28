using System.Collections.Frozen;

namespace Stalker.Gamma.Utilities;

public static class ArchiveUtility
{
    public static async Task ExtractAsync(
        string archivePath,
        string destinationDir,
        Action<double> pct,
        CancellationToken ct
    )
    {
        if (OperatingSystem.IsWindows())
        {
            await SevenZipUtility.ExtractAsync(
                archivePath,
                destinationDir,
                pct,
                cancellationToken: ct
            );
        }
        else
        {
            await using var fs = File.OpenRead(archivePath);
            fs.Seek(0, SeekOrigin.Begin);
            if (ArchiveMappings.TryGetValue(fs.ReadByte(), out var extractFunc))
            {
                try
                {
                    await extractFunc.Invoke(archivePath, destinationDir, pct, ct);
                }
                finally
                {
                    // Permissions are a pain in my ass
                    DirUtils.NormalizePermissions(destinationDir);
                }
            }
            else
            {
                throw new ArchiveUtilityException(
                    $"""
                    Unsupported archive type
                    Archive: {archivePath}
                    """
                );
            }
        }
    }

    private static readonly FrozenDictionary<
        int,
        Func<string, string, Action<double>, CancellationToken, Task>
    > ArchiveMappings = new Dictionary<
        int,
        Func<string, string, Action<double>, CancellationToken, Task>
    >
    {
        {
            0x37,
            async (archivePath, destinationDir, pct, ct) =>
                await SevenZipUtility.ExtractAsync(
                    archivePath,
                    destinationDir,
                    pct,
                    cancellationToken: ct
                )
        },
        {
            0x50,
            async (archivePath, destinationDir, pct, ct) =>
            {
                if (OperatingSystem.IsLinux())
                {
                    await UnzipUtility.ExtractAsync(archivePath, destinationDir, pct, ct);
                }
                else
                {
                    await TarUtility.ExtractAsync(archivePath, destinationDir, pct, ct);
                }
            }
        },
        {
            0x52,
            async (archivePath, destinationDir, pct, ct) =>
                await SevenZipUtility.ExtractAsync(
                    archivePath,
                    destinationDir,
                    pct,
                    cancellationToken: ct
                )
        },
    }.ToFrozenDictionary();
}

public class ArchiveUtilityException(string message) : Exception(message);
