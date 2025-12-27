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

            if (
                (addonRecord.ZipName?.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) ?? false)
                && !OperatingSystem.IsWindows()
            )
            {
                if (OperatingSystem.IsMacOS())
                {
                    await TarUtility.ExtractAsync(archivePath, destinationDir, pct, ct);
                }
                else
                {
                    await UnzipUtility.ExtractAsync(archivePath, destinationDir, pct, ct);
                }
            }
            else
            {
                await SevenZipUtility.ExtractAsync(
                    archivePath,
                    destinationDir,
                    pct,
                    cancellationToken: ct
                );
            }

            var instructions = addonRecord.Instructions is null or "0"
                ? []
                : addonRecord
                    .Instructions?.Split(
                        ':',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(y => y.TrimStart('\\').Replace('\\', Path.DirectorySeparatorChar))
                    .ToList() ?? [];

            ProcessInstructions.Process(destinationDir, instructions);

            CleanExtractPath.Clean(destinationDir);

            WriteAddonMetaIni.Write(destinationDir, repoName, addonRecord.ModDbUrl!);
        }
    }
}
