using System.Collections.Frozen;
using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public static class ExtractAddonGroup
{
    public static async Task ExtractAsync(
        string repoName,
        string repoPath,
        string gammaModsPath,
        IGrouping<string, ModListRecord> group,
        Action<double> pct,
        CancellationToken ct
    )
    {
        foreach (var addonRecord in group)
        {
            var destinationDir = Path.Join(
                gammaModsPath,
                $"{addonRecord.Counter}- {addonRecord.AddonName}{addonRecord.Patch}"
            );
            Directory.CreateDirectory(destinationDir);
            var archivePath = Path.Join(repoPath, repoName);

            await ExtractAsync(archivePath, destinationDir, pct, ct);

            ProcessInstructions.Process(destinationDir, addonRecord.Instructions);

            CleanExtractPath.Clean(destinationDir);

            WriteAddonMetaIni.Write(destinationDir, repoName, addonRecord.ModDbUrl!);
        }
    }

    private static async Task ExtractAsync(
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
                throw new ExtractAddonGroupException(
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

public class ExtractAddonGroupException(string msg) : Exception(msg);
