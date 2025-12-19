using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using stalker_gamma.cli.Services.Enums;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;
using GlobalSettings = stalker_gamma.core.Models.GlobalSettings;

namespace stalker_gamma.cli.Services;

public partial class CustomGammaInstaller(
    GlobalSettings globalSettings,
    IHttpClientFactory hcf,
    ModDb modDb,
    ModListRecordFactory modListRecordFactory,
    GitUtility gu
)
{
    public async Task InstallAsync(
        string anomalyPath,
        Task anomalyTask,
        string gammaPath,
        string cachePath
    )
    {
        var gammaModsPath = Path.Join(gammaPath, "mods");
        var gammaDownloadsPath = Path.Join(gammaPath, "downloads");

        #region Download Mods from ModDb and GitHub


        var gammaApiResponse = (await _hc.GetStringAsync("https://stalker-gamma.com/api/list"))
            .Split("\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select((x, idx) => _modListRecordFactory.Create(x, idx))
            .Cast<ModListRecord>();

        Directory.CreateDirectory(cachePath);
        Directory.CreateDirectory(gammaModsPath);

        var fullCachePath = Path.GetFullPath(cachePath!);
        if (!Directory.Exists(gammaDownloadsPath))
        {
            Directory.CreateSymbolicLink(gammaDownloadsPath, fullCachePath);
        }

        var indexedResponse = gammaApiResponse
            .Select((x, i) => (x, i))
            .ToFrozenDictionary(x => x.i + 1, x => x.x);

        var longestLengthTitle =
            indexedResponse.MaxBy(x => x.Value.AddonName?.Length).Value.AddonName!.Length + 8;

        var addons = indexedResponse
            // moddb
            .Where(kvp => kvp.Value is ModDbRecord)
            .Select(kvp =>
                MainToAddonRecord(
                    kvp.Key,
                    kvp.Value,
                    cachePath,
                    gammaModsPath,
                    AddonType.ModDb,
                    pct =>
                        Console.WriteLine(
                            $"{kvp.Value.AddonName?.PadRight(longestLengthTitle)} - {pct:F2}"
                        )
                )
            )
            // github
            .Concat(
                indexedResponse
                    .Where(kvp => kvp.Value is GithubRecord)
                    .Select(kvp =>
                        MainToAddonRecord(
                            kvp.Key,
                            kvp.Value,
                            cachePath,
                            gammaModsPath,
                            AddonType.GitHub,
                            pct =>
                                Console.WriteLine(
                                    $"{kvp.Value.AddonName?.PadRight(longestLengthTitle)} - {pct:F2}"
                                )
                        )
                    )
            )
            .GroupBy(x => x.ArchiveDlPath)
            .OrderBy(x => x.First().Index);

        // Write separators
        var separators = indexedResponse
            .Where(kvp => kvp.Value is Separator)
            .Select(kvp => kvp.Value)
            .Cast<Separator>()
            .Select(kvp => Path.Join(gammaModsPath, kvp.FolderName));
        foreach (var separator in separators)
        {
            Directory.CreateDirectory(separator);
            await File.WriteAllTextAsync(
                Path.Join(separator, "meta.ini"),
                SeparatorMetaIni.ReplaceLineEndings("\r\n")
            );
        }

        ConcurrentBag<IGrouping<string, AddonRecord>> brokenAddons = [];

        // download
        var dlChannel = Channel.CreateUnbounded<IGrouping<string, AddonRecord>>();
        var dlTask = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                addons,
                new ParallelOptions { MaxDegreeOfParallelism = globalSettings.DownloadThreads },
                async (group, t) =>
                {
                    try
                    {
                        await DownloadAndVerifyFile(group, invalidateMirror: true, t);
                        dlChannel.Writer.TryWrite(group);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"WARNING: Unable to download {group.Key}: {e}");
                        brokenAddons.Add(group);
                    }
                }
            );
            dlChannel.Writer.TryComplete();
        });

        var extractTask = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                dlChannel.Reader.ReadAllAsync(),
                new ParallelOptions { MaxDegreeOfParallelism = globalSettings.ExtractThreads },
                async (group, _) =>
                {
                    try
                    {
                        await ExtractAndProcessAddon(group);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"WARNING: Unable to extract {group.Key}: {e.Message}");
                        brokenAddons.Add(group);
                    }
                }
            );
        });

        await Task.WhenAll(dlTask, extractTask);

        foreach (var brokenAddon in brokenAddons)
        {
            try
            {
                await DownloadAndVerifyFile(
                    brokenAddon,
                    invalidateMirror: true,
                    CancellationToken.None
                );
                await ExtractAndProcessAddon(brokenAddon);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"""
                    ERROR Processing broken addon: {brokenAddon.Key}
                    {e}
                    """
                );
            }
        }

        #endregion

        #region Download Base Git Repos

        var stalkerGammaRepoPath = Path.Join(cachePath, StalkerGammaRepo);
        var gammaLargeFilesRepoPath = Path.Join(cachePath, GammaLargeFilesRepo);
        var teivazAnomalyGunslingerRepoPath = Path.Join(cachePath, TeivazAnomalyGunslingerRepo);
        var gammaSetupRepoPath = Path.Join(cachePath, GammaSetupRepo);

        var gammaSetupAndStalkerGammaTask = Task.Run(async () =>
        {
            if (Directory.Exists(gammaSetupRepoPath))
            {
                await _gu.PullGitRepo(gammaSetupRepoPath);
            }
            else
            {
                await _gu.CloneGitRepo(
                    gammaSetupRepoPath,
                    string.Format(GithubUrl, GitAuthor, GammaSetupRepo)
                );
            }
            DirUtils.CopyDirectory(
                Path.Join(gammaSetupRepoPath, "modpack_addons"),
                Path.Combine(gammaModsPath),
                onProgress: (copied, total) =>
                    Console.WriteLine($"Gamma Setup: {(double)copied / total * 100:F2}%")
            );

            if (Directory.Exists(stalkerGammaRepoPath))
            {
                await _gu.PullGitRepo(stalkerGammaRepoPath);
            }
            else
            {
                await _gu.CloneGitRepo(
                    stalkerGammaRepoPath,
                    string.Format(GithubUrl, GitAuthor, StalkerGammaRepo)
                );
            }
            DirUtils.CopyDirectory(
                Path.Combine(stalkerGammaRepoPath, "G.A.M.M.A", "modpack_addons"),
                Path.Combine(gammaModsPath),
                overwrite: true,
                onProgress: (copied, total) =>
                    Console.WriteLine($"Stalker GAMMA: {(double)copied / total * 100:F2}%")
            );
            File.Copy(
                Path.Combine(stalkerGammaRepoPath, "G.A.M.M.A_definition_version.txt"),
                Path.Combine(gammaModsPath, "version.txt"),
                true
            );
        });

        var gammaLargeFilesTask = Task.Run(async () =>
        {
            if (Directory.Exists(gammaLargeFilesRepoPath))
            {
                await _gu.PullGitRepo(gammaLargeFilesRepoPath);
            }
            else
            {
                await _gu.CloneGitRepo(
                    gammaLargeFilesRepoPath,
                    string.Format(GithubUrl, GitAuthor, GammaLargeFilesRepo)
                );
            }
            DirUtils.CopyDirectory(
                gammaLargeFilesRepoPath,
                gammaModsPath,
                overwrite: true,
                onProgress: (count, total) =>
                    Console.WriteLine($"GAMMA Large Files: {(double)count / total * 100:F2}%")
            );
        });

        var teivazTask = Task.Run(async () =>
        {
            if (Directory.Exists(teivazAnomalyGunslingerRepoPath))
            {
                await _gu.PullGitRepo(teivazAnomalyGunslingerRepoPath);
            }
            else
            {
                await _gu.CloneGitRepo(
                    teivazAnomalyGunslingerRepoPath,
                    string.Format(GithubUrl, GitAuthor, TeivazAnomalyGunslingerRepo)
                );
            }
            foreach (
                var gameDataDir in new DirectoryInfo(
                    teivazAnomalyGunslingerRepoPath
                ).EnumerateDirectories("gamedata", SearchOption.AllDirectories)
            )
            {
                DirUtils.CopyDirectory(
                    gameDataDir.FullName,
                    Path.Join(
                        gammaModsPath,
                        "312- Gunslinger Guns for Anomaly - Teivazcz & Gunslinger Team",
                        "gamedata"
                    ),
                    overwrite: true,
                    onProgress: (count, total) =>
                        Console.WriteLine(
                            $"Teivaz Anomaly Gunslinger: {(double)count / total * 100:F2}%"
                        )
                );
            }
        });

        var stalkerGammaModpackPatches = Task.Run(async () =>
        {
            // wait for gamma setup, stalker gamma, anomaly to finish downloading and extracting
            await Task.WhenAll(gammaSetupAndStalkerGammaTask, anomalyTask);

            var cacheModpackPatchPath = Path.Join(
                stalkerGammaRepoPath,
                "G.A.M.M.A",
                "modpack_patches"
            );
            DirUtils.CopyDirectory(cacheModpackPatchPath, anomalyPath);
        });

        await Task.WhenAll(
            gammaSetupAndStalkerGammaTask,
            gammaLargeFilesTask,
            teivazTask,
            stalkerGammaModpackPatches
        );

        #endregion

        // copy version from stalker gamma repo to cache path
        File.Copy(
            Path.Join(stalkerGammaRepoPath, "G.A.M.M.A_definition_version.txt"),
            Path.Join(cachePath, "version.txt"),
            true
        );
    }

    private static async Task ExtractAndProcessAddon(IGrouping<string, AddonRecord> group)
    {
        foreach (var addonRecord in group)
        {
            Console.WriteLine($"Extracting {addonRecord.Name}");

            Directory.CreateDirectory(addonRecord.ExtractDirectory);

            if (addonRecord.AddonType == AddonType.ModDb)
            {
                await ArchiveUtility.ExtractWithProgress(
                    addonRecord.ArchiveDlPath,
                    addonRecord.ExtractDirectory,
                    addonRecord.OnProgress
                );
            }
            else
            {
                DirUtils.CopyDirectory(
                    addonRecord.ArchiveDlPath,
                    addonRecord.ExtractDirectory,
                    true,
                    onProgress: (cur, total) => addonRecord.OnProgress((double)cur / total)
                );
            }

            ProcessInstructions(addonRecord.ExtractDirectory, addonRecord.Instructions);

            CleanExtractPath(addonRecord.ExtractDirectory);

            await WriteAddonMetaIniAsync(
                addonRecord.ExtractDirectory,
                addonRecord.ZipName,
                addonRecord.NiceUrl
            );
        }
    }

    private async Task DownloadAndVerifyFile(
        IGrouping<string, AddonRecord> group,
        bool invalidateMirror,
        CancellationToken t
    )
    {
        var first = group.First();

        // if file doesn't exist or md5 doesn't match, download it
        if (
            first.AddonType == AddonType.GitHub // always download github files because md5 is not available
            || Path.Exists(first.ArchiveDlPath)
                && !string.IsNullOrWhiteSpace(first.Md5)
                && await Md5Utility.CalculateFileMd5Async(first.ArchiveDlPath) != first.Md5
            || !Path.Exists(first.ArchiveDlPath)
        )
        {
            Console.WriteLine($"Downloading {first.Name}");
            switch (first.AddonType)
            {
                case AddonType.ModDb:
                {
                    var usedMirror = await _modDb.GetModDbLinkCurl(
                        first.MirrorUrl!.Replace("/all", ""),
                        first.ArchiveDlPath,
                        first.OnProgress,
                        invalidateMirror
                    );
                    break;
                }
                case AddonType.GitHub:
                {
                    var profileAndRepoMatch = GithubRx().Match(first.Url);
                    var (profile, repo) = (
                        profileAndRepoMatch.Groups["profile"].Value,
                        profileAndRepoMatch.Groups["repo"].Value
                    );
                    var matchedRef = Rxs.First(rx => rx(first.Url).Success)(first.Url)
                        .Groups["ref"]
                        .Value;

                    if (Directory.Exists(first.ArchiveDlPath))
                    {
                        var defaultBranch = await _gu.GetDefaultBranch(first.ArchiveDlPath);
                        await _gu.CheckoutBranch(first.ArchiveDlPath, defaultBranch);
                    }
                    else
                    {
                        await _gu.CloneGitRepo(
                            first.ArchiveDlPath,
                            string.Format(GithubUrl, profile, repo)
                        );
                    }

                    await _gu.PullGitRepo(first.ArchiveDlPath);

                    await _gu.CheckoutBranch(first.ArchiveDlPath, matchedRef);

                    break;
                }
            }
        }
    }

    private static AddonRecord MainToAddonRecord(
        int idx,
        ModListRecord m,
        string? cacheDir,
        string gammaDir,
        AddonType type,
        Action<double> onProgress
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
        var extractDir = Path.Join(gammaDir, $"{idx}-{m.AddonName}{m.Patch}");
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
            instructions,
            type,
            onProgress
        );
    }

    private static void ProcessInstructions(string extractPath, string[] instructions)
    {
        foreach (var i in instructions)
        {
            if (Path.Exists(Path.Join(extractPath, i, "gamedata")))
            {
                DirUtils.CopyDirectory(Path.Join(extractPath, i), extractPath);
            }
            else
            {
                Directory.CreateDirectory(Path.Join(extractPath, "gamedata"));
                if (Directory.Exists(Path.Join(extractPath, i)))
                {
                    DirUtils.CopyDirectory(
                        Path.Join(extractPath, i),
                        Path.Join(extractPath, "gamedata")
                    );
                }
            }
        }
    }

    private static void CleanExtractPath(string extractPath)
    {
        if (!Directory.Exists(extractPath))
        {
            return;
        }

        DirUtils.NormalizePermissions(extractPath);

        DirUtils.RecursivelyDeleteDirectory(extractPath, DoNotMatch);
    }

    private static readonly IReadOnlyList<string> DoNotMatch =
    [
        "gamedata",
        "appdata",
        "db",
        "fomod",
    ];

    private readonly string _dir = Path.GetDirectoryName(AppContext.BaseDirectory)!;

    private const string GitAuthor = "Grokitach";
    private const string GammaSetupRepo = "gamma_setup";
    private const string StalkerGammaRepo = "Stalker_GAMMA";
    private const string GammaLargeFilesRepo = "gamma_large_files_v2";
    private const string TeivazAnomalyGunslingerRepo = "teivaz_anomaly_gunslinger";

    private readonly ModDb _modDb = modDb;
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
    private readonly HttpClient _hc = hcf.CreateClient();
    private readonly HttpClient _githubDlArchiveClient = hcf.CreateClient("githubDlArchive");
    private readonly GitUtility _gu = gu;

    private const string SeparatorMetaIni = """
        [General]
        modid=0
        version=
        newestVersion=
        category=0
        installationFile=

        [installedFiles]
        size=0

        """;

    private static async Task WriteAddonMetaIniAsync(
        string extractPath,
        string archiveName,
        string niceUrl
    ) =>
        await File.WriteAllTextAsync(
            Path.Join(extractPath, "meta.ini"),
            $"""
            [General]
            gameName=stalkeranomaly
            modid=0
            ignoredversion={archiveName}
            version={archiveName}
            newestversion={archiveName}
            category="-1,"
            nexusFileStatus=1
            installationFile={archiveName}
            repository=
            comments=
            notes=
            nexusDescription=
            url={niceUrl}
            hasCustomURL=true
            lastNexusQuery=
            lastNexusUpdate=
            nexusLastModified=2021-11-09T18:10:18Z
            converted=false
            validated=false
            color=@Variant(\0\0\0\x43\0\xff\xff\0\0\0\0\0\0\0\0)
            tracked=0

            [installedFiles]
            1\modid=0
            1\fileid=0
            size=1

            """.ReplaceLineEndings("\r\n"),
            encoding: Encoding.UTF8
        );

    private static readonly ImmutableList<Func<string, Match>> Rxs =
    [
        txt => Rx1().Match(txt),
        txt => Rx2().Match(txt),
        txt => ShaRx().Match(txt),
    ];

    [GeneratedRegex(
        @"https://github.com/(?<profile>[\w\-_]*)/+?(?<repo>[\w\-_\.]*).*/(?<archive>.*)\.+?"
    )]
    public static partial Regex GithubRx();

    [GeneratedRegex("releases/download/(?<ref>.*)/.*?")]
    public static partial Regex Rx1();

    [GeneratedRegex("refs/(?<refType>tags|heads)/(?<ref>.+)\\..+")]
    public static partial Regex Rx2();

    [GeneratedRegex("(?<ref>[a-fA-F0-9]{40})")]
    public static partial Regex ShaRx();

    private const string GithubUrl = "https://github.com/{0}/{1}";
}

internal record AddonRecord(
    int Index,
    string Name,
    string Url,
    string? MirrorUrl,
    string NiceUrl,
    string? Md5,
    string ArchiveDlPath,
    string ZipName,
    string ExtractDirectory,
    string[] Instructions,
    AddonType AddonType,
    Action<double> OnProgress
);
