using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public static class ExtractAddonGroup
{
    public static async Task Extract(
        string repoName,
        string repoPath,
        string gammaModsPath,
        IGrouping<string, ModListRecord> group,
        Action<double> pct
    )
    {
        foreach (var addonRecord in group)
        {
            var destinationDir = Path.Join(
                gammaModsPath,
                $"{addonRecord.Counter}- {addonRecord.AddonName}{addonRecord.Patch}"
            );
            Directory.CreateDirectory(destinationDir);
            if (addonRecord is ModDbRecord)
            {
                await ArchiveUtility.ExtractAsync(
                    Path.Join(repoPath, repoName),
                    destinationDir,
                    pct
                );
            }
            else
            {
                DirUtils.CopyDirectory(repoPath, destinationDir, true, onProgress: pct);
            }
            var instructions = addonRecord.Instructions is null or "0"
                ? []
                : addonRecord
                    .Instructions?.Split(
                        ':',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                    )
                    .Select(y => y.Replace('\\', Path.DirectorySeparatorChar))
                    .ToList() ?? [];

            // fix instructions because I broke github downloads by downloading them with git instead of archives
            var extractDirs = Directory.GetDirectories(destinationDir);
            if (
                instructions.Count == 0
                && extractDirs.Length == 1
                && !extractDirs[0].EndsWith("gamedata")
                && addonRecord is GithubRecord
            )
            {
                var innerDir = Directory.GetDirectories(extractDirs[0])[0];
                if (innerDir.EndsWith("gamedata"))
                {
                    instructions.Add(
                        OperatingSystem.IsWindows()
                            ? extractDirs[0].Split('\\')[^1]
                            : extractDirs[0].Split('/')[^1]
                    );
                }
            }

            ProcessInstructions.Process(destinationDir, instructions);

            CleanExtractPath.Clean(destinationDir);

            DirUtils.NormalizePermissions(destinationDir);

            WriteAddonMetaIni.Write(destinationDir, repoName, addonRecord.ModDbUrl!);
        }
    }
}
