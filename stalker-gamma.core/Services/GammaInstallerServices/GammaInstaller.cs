using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using Serilog;
using stalker_gamma.core.Factories;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.Enums;
using stalker_gamma.core.Utilities;
using GlobalSettings = stalker_gamma.core.Models.GlobalSettings;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public partial class GammaInstaller(
    GlobalSettings globalSettings,
    IHttpClientFactory hcf,
    ModDb modDb,
    ModListRecordFactory modListRecordFactory,
    ILogger logger,
    ProgressThrottleService progressThrottle,
    ProgressService progressService
)
{
    public async Task InstallAsync(
        IList<IGrouping<string, AddonRecord>> addonRecords,
        string anomalyPath,
        Task anomalyTask,
        string gammaPath,
        string cachePath,
        CancellationToken? cancellationToken = null
    )
    {
        cancellationToken ??= CancellationToken.None;
        var gammaModsPath = Path.Join(gammaPath, "mods");

        #region Download Mods from ModDb and GitHub

        Directory.CreateDirectory(cachePath);
        Directory.CreateDirectory(gammaModsPath);

        _progressService.TotalAddons = addonRecords.Count + 1 + 5;

        ConcurrentBag<IGrouping<string, AddonRecord>> brokenAddons = [];

        // download
        var dlChannel = Channel.CreateUnbounded<IGrouping<string, AddonRecord>>();
        var dlTask = Task.Run(
            async () =>
            {
                await Parallel.ForEachAsync(
                    addonRecords,
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
                            _logger.Warning(
                                "Unable to download {AddonName}: {Exception}",
                                group.Key,
                                e
                            );
                            brokenAddons.Add(group);
                        }
                    }
                );
                dlChannel.Writer.TryComplete();
            },
            (CancellationToken)cancellationToken
        );

        var extractTask = Task.Run(
            async () =>
            {
                await Parallel.ForEachAsync(
                    dlChannel.Reader.ReadAllAsync(),
                    new ParallelOptions { MaxDegreeOfParallelism = globalSettings.ExtractThreads },
                    async (group, ct) =>
                    {
                        try
                        {
                            await ExtractAndProcessAddon(group, ct);
                        }
                        catch (Exception e)
                        {
                            _logger.Warning(
                                "Unable to extract {AddonName}: {Exception}",
                                group.Key,
                                e
                            );
                            brokenAddons.Add(group);
                        }
                    }
                );
            },
            (CancellationToken)cancellationToken
        );

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
                await ExtractAndProcessAddon(brokenAddon, (CancellationToken)cancellationToken);
            }
            catch (Exception e)
            {
                _logger.Error(
                    """
                    ERROR Processing broken addon: {BrokenAddon}
                    {Exception}
                    """,
                    brokenAddon.Key,
                    e
                );
            }
        }

        #endregion

        #region Download Base Git Repos

        var stalkerGammaRepoPath = Path.Join(cachePath, StalkerGammaRepo);

        #endregion

        // copy version from stalker gamma repo to cache path
        File.Copy(
            Path.Join(stalkerGammaRepoPath, "G.A.M.M.A_definition_version.txt"),
            Path.Join(cachePath, "version.txt"),
            true
        );
    }

    private async Task ExtractAndProcessAddon(
        IGrouping<string, AddonRecord> group,
        CancellationToken ct
    )
    {
        foreach (var addonRecord in group)
        {
            Directory.CreateDirectory(addonRecord.ExtractDirectory);

            if (addonRecord.AddonType == AddonType.ModDb)
            {
                await ArchiveUtility.ExtractAsync(
                    addonRecord.ArchiveDlPath,
                    addonRecord.ExtractDirectory,
                    addonRecord.OnExtractProgress
                );
            }
            else
            {
                DirUtils.CopyDirectory(
                    addonRecord.ArchiveDlPath,
                    addonRecord.ExtractDirectory,
                    true,
                    onProgress: _progressThrottle.Throttle<double>(pct =>
                        addonRecord.OnExtractProgress(pct)
                    )
                );
            }

            // fix instructions because I broke github downloads by downloading them with git instead of archives
            var extractDirs = Directory.GetDirectories(addonRecord.ExtractDirectory);
            if (
                addonRecord.Instructions.Count == 0
                && extractDirs.Length == 1
                && !extractDirs[0].EndsWith("gamedata")
                && addonRecord.AddonType == AddonType.GitHub
            )
            {
                var innerDir = Directory.GetDirectories(extractDirs[0])[0];
                if (innerDir.EndsWith("gamedata"))
                {
                    addonRecord.Instructions.Add(
                        OperatingSystem.IsWindows()
                            ? extractDirs[0].Split('\\')[^1]
                            : extractDirs[0].Split('/')[^1]
                    );
                }
            }

            ProcessInstructions(addonRecord.ExtractDirectory, addonRecord.Instructions);

            CleanExtractPath(addonRecord.ExtractDirectory);

            DirUtils.NormalizePermissions(addonRecord.ExtractDirectory);

            await WriteAddonMetaIniAsync(
                addonRecord.ExtractDirectory,
                addonRecord.ZipName,
                addonRecord.NiceUrl
            );
        }

        _progressService.IncrementCompleted();
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
                && await Md5Utility.CalculateFileMd5Async(first.ArchiveDlPath, _ => { })
                    != first.Md5
            || !Path.Exists(first.ArchiveDlPath)
        )
        {
            switch (first.AddonType)
            {
                case AddonType.ModDb:
                {
                    var usedMirror = await _modDb.GetModDbLinkCurl(
                        first.MirrorUrl!.Replace("/all", ""),
                        first.ArchiveDlPath,
                        first.OnDlProgress,
                        invalidateMirrorCache: invalidateMirror
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
                        // delete git dir because attempting to update repos on different refs is a pita. Should figure this out later
                        DirUtils.NormalizePermissions(first.ArchiveDlPath);
                        Directory.Delete(first.ArchiveDlPath, true);
                    }

                    // await _gu.CloneGitRepo(
                    //     first.ArchiveDlPath,
                    //     string.Format(GithubUrl, profile, repo),
                    //     onProgress: _progressThrottle.Throttle<double>(pct =>
                    //         _logger.Information(
                    //             StructuredLog,
                    //             first.Name[..Math.Min(first.Name.Length, 35)].PadRight(40),
                    //             "Clone".PadRight(10),
                    //             $"{pct:P2}".PadRight(8),
                    //             _progressService.TotalProgress
                    //         )
                    //     )
                    // );

                    if (matchedRef != "latest")
                    {
                        await GitUtility.CheckoutBranch(first.ArchiveDlPath, matchedRef);
                    }

                    break;
                }
            }
        }
    }

    private static void ProcessInstructions(string extractPath, IList<string> instructions)
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

    private const string GammaSetupRepo = "gamma_setup";
    private const string StalkerGammaRepo = "Stalker_GAMMA";
    private const string GammaLargeFilesRepo = "gamma_large_files_v2";
    private const string TeivazAnomalyGunslingerRepo = "teivaz_anomaly_gunslinger";

    private readonly ModDb _modDb = modDb;
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
    private readonly HttpClient _hc = hcf.CreateClient();
    private readonly ILogger _logger = logger;
    private readonly ProgressThrottleService _progressThrottle = progressThrottle;
    private readonly ProgressService _progressService = progressService;

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
    private static partial Regex GithubRx();

    [GeneratedRegex("releases/download/(?<ref>.*)/.*?")]
    private static partial Regex Rx1();

    [GeneratedRegex("refs/(?<refType>tags|heads)/(?<ref>.+)\\..+")]
    private static partial Regex Rx2();

    [GeneratedRegex("(?<ref>[a-fA-F0-9]{40})")]
    private static partial Regex ShaRx();

    private const string GithubUrl = "https://github.com/{0}/{1}";
}
