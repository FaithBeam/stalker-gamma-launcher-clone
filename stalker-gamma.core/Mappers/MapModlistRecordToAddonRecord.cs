using System.Text.RegularExpressions;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.Enums;

namespace stalker_gamma.core.Mappers;

public static partial class MapModlistRecordToAddonRecord
{
    public static AddonRecord Map(
        int idx,
        ModListRecord m,
        string? cacheDir,
        string gammaDir,
        AddonType type,
        Action<string> onStatus,
        Action<double> onMd5Progress,
        Action<double> onDlProgress,
        Action<double> onExtractProgress
    )
    {
        var instructions = m.Instructions is null or "0"
            ? []
            : m.Instructions?.Split(
                    ':',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries
                )
                .Select(x => x.Replace('\\', Path.DirectorySeparatorChar))
                .ToArray() ?? [];
        var githubNameMatch = GithubRx().Match(m.DlLink!);
        var githubName = $"{githubNameMatch.Groups["repo"]}-{githubNameMatch.Groups["archive"]}";
        var archivePath = Path.Join(
            cacheDir,
            type switch
            {
                AddonType.ModDb => m.ZipName,
                AddonType.GitHub => githubName,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            }
        );
        var extractDir = Path.Join(gammaDir, $"{idx}- {m.AddonName}{m.Patch}");
        var zipName = type switch
        {
            AddonType.ModDb => m.ZipName!,
            AddonType.GitHub => githubName,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        return new AddonRecord(
            idx,
            m.AddonName!,
            m.DlLink!,
            m.DlLink + "/all",
            m.ModDbUrl!,
            m.Md5ModDb,
            archivePath,
            zipName,
            extractDir,
            instructions.ToList(),
            type,
            onStatus,
            onMd5Progress,
            onDlProgress,
            onExtractProgress
        );
    }

    [GeneratedRegex(
        @"https://github.com/(?<profile>[\w\-_]*)/+?(?<repo>[\w\-_\.]*).*/(?<archive>.*)\.+?"
    )]
    private static partial Regex GithubRx();
}
