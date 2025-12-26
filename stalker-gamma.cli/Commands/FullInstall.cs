using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;
using stalker_gamma.core.Services.GammaInstallerServices.SpecialRepos;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DownloadModOrganizer;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public partial class FullInstallCmd(
    GlobalSettings globalSettings,
    EnrichAnomalyInstaller anomalyInstaller,
    EnrichDownloadAndExtractGitRepoFactory downloadAndExtractGitRepoFactory,
    InstallModOrganizerGammaProfile installModOrganizerGammaProfile,
    DownloadModOrganizerService downloadModOrganizerService,
    WriteModOrganizerIniService writeModOrganizerIniService,
    DisableNexusModHandlerLink disableNexusModHandlerLink,
    ILogger logger,
    AddFoldersToWinDefenderExclusionService addFoldersToWinDefenderExclusionService,
    EnableLongPathsOnWindowsService enableLongPathsOnWindowsService,
    GetAddonsFromApiService getAddonsFromApiService,
    WriteSeparatorsService writeSeparatorsService,
    DownloadAddon downloadAddon,
    ProgressService progress
)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// This will install/update Anomaly and all GAMMA addons.
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cache">Cache directory</param>
    /// <param name="anomalyArchiveName">Optionally change the name of the downloaded anomaly archive</param>
    /// <param name="downloadThreads">Number of parallel downloads that can occur</param>
    /// <param name="addFoldersToWinDefenderExclusion">(Windows) Add the anomaly, gamma, and cache folders to the Windows Defender Exclusion list</param>
    /// <param name="enableLongPaths">(Windows) Enable long paths</param>
    /// <param name="mo2Version">The version of Mod Organizer 2 to download</param>
    /// <param name="progressUpdateIntervalMs">How frequently to write progress to the console in milliseconds</param>
    /// <param name="stalkerAddonApiUrl">Escape hatch for stalker gamma api</param>
    /// <param name="gammaSetupRepoUrl">Escape hatch for git repo gamma_setup</param>
    /// <param name="stalkerGammaRepoUrl">Escape hatch for git repo Stalker_GAMMA</param>
    /// <param name="gammaLargeFilesRepoUrl">Escape hatch for git repo gamma_large_files_v2</param>
    /// <param name="teivazAnomalyGunslingerRepoUrl">Escape hatch for git repo teivaz_anomaly_gunslinger</param>
    /// <param name="stalkerAnomalyModdbUrl">Escape hatch for Stalker Anomaly</param>
    /// <param name="stalkerAnomalyArchiveMd5">The hash of the archive downloaded from --stalker-anomaly-moddb-url</param>
    public async Task FullInstall(
        // ReSharper disable once InvalidXmlDocComment
        CancellationToken cancellationToken,
        string anomaly,
        string gamma,
        string cache = "cache",
        int downloadThreads = 1,
        bool addFoldersToWinDefenderExclusion = false,
        bool enableLongPaths = false,
        [Hidden] string mo2Version = "v2.5.2",
        [Hidden] long progressUpdateIntervalMs = 1000,
        [Hidden] string anomalyArchiveName = "anomaly.7z",
        [Hidden] string stalkerAddonApiUrl = "https://stalker-gamma.com/api/list",
        [Hidden] string gammaSetupRepoUrl = "https://github.com/Grokitach/gamma_setup",
        [Hidden] string stalkerGammaRepoUrl = "https://github.com/Grokitach/Stalker_GAMMA",
        [Hidden]
            string gammaLargeFilesRepoUrl = "https://github.com/Grokitach/gamma_large_files_v2",
        [Hidden]
            string teivazAnomalyGunslingerRepoUrl =
            "https://github.com/Grokitach/teivaz_anomaly_gunslinger",
        [Hidden] string stalkerAnomalyModdbUrl = "https://www.moddb.com/downloads/start/277404",
        [Hidden] string stalkerAnomalyArchiveMd5 = "d6bce51a4e6d98f9610ef0aa967ba964"
    )
    {
        globalSettings.DownloadThreads = downloadThreads;
        globalSettings.StalkerAddonApiUrl = stalkerAddonApiUrl;
        globalSettings.GammaSetupRepo = gammaSetupRepoUrl;
        globalSettings.StalkerGammaRepo = stalkerGammaRepoUrl;
        globalSettings.GammaLargeFilesRepo = gammaLargeFilesRepoUrl;
        globalSettings.TeivazAnomalyGunslingerRepo = teivazAnomalyGunslingerRepoUrl;
        globalSettings.StalkerAnomalyModDbUrl = stalkerAnomalyModdbUrl;
        globalSettings.StalkerAnomalyArchiveMd5 = stalkerAnomalyArchiveMd5;
        globalSettings.ProgressUpdateIntervalMs = progressUpdateIntervalMs;

        Directory.CreateDirectory(anomaly);
        Directory.CreateDirectory(gamma);
        Directory.CreateDirectory(cache);
        var anomalyCacheArchivePath = Path.Join(cache, anomalyArchiveName);
        var gammaDownloadsDir = Path.GetFullPath(Path.Join(gamma, "downloads"));
        CreateSymbolicLinkUtility.Create(gammaDownloadsDir, Path.GetFullPath(cache));

        if (OperatingSystem.IsWindows())
        {
            await GitUtility.EnableLongPathsAsync();
            if (enableLongPaths)
            {
                enableLongPathsOnWindowsService.Execute();
            }
            if (addFoldersToWinDefenderExclusion)
            {
                addFoldersToWinDefenderExclusionService.Execute(anomaly, gamma, cache);
            }
        }
        else
        {
            mo2Version = "v2.4.4";
        }

        var anomalyTask = Task.Run(
            async () =>
                await anomalyInstaller.DownloadAndExtractAsync(
                    anomalyCacheArchivePath,
                    anomaly,
                    cancellationToken
                ),
            cancellationToken
        );

        var addons = await getAddonsFromApiService.GetAddonsAsync(cancellationToken);

        var gammaModsPath = Path.Join(gamma, "mods");
        var separators = addons
            .Where(kvp => kvp.Value is Separator)
            .Select(kvp => kvp.Value)
            .Cast<Separator>()
            .Select(kvp => Path.Join(gammaModsPath, kvp.FolderName));
        await writeSeparatorsService.WriteAsync(separators);

        var anomalyBinPath = Path.Join(anomaly, "bin");

        var modDbAddons = new List<DownloadAndExtractRecordService> { };
        modDbAddons.AddRange(
            addons
                .Values.Where(x => x is ModDbRecord)
                .GroupBy(x => Path.Join(cache, x.ZipName))
                .Select(group =>
                {
                    var first = group.First();
                    return downloadAndExtractGitRepoFactory.Create(
                        first.AddonName![..Math.Min(first.AddonName!.Length, 35)].PadRight(40),
                        Path.Join(cache, first.ZipName),
                        "",
                        "",
                        dlFunc: (checkMd5Pct, dlPct, _) =>
                            downloadAddon.DownloadAsync(
                                first,
                                Path.Join(cache, first.ZipName),
                                checkMd5Pct,
                                dlPct
                            ),
                        extractFunc: pct =>
                            ExtractAddonGroup.Extract(
                                first.ZipName!,
                                cache,
                                gammaModsPath,
                                group,
                                pct
                            )
                    );
                })
        );
        progress.TotalAddons += modDbAddons.Count;

        ConcurrentBag<DownloadAndExtractRecordService> brokenAddons = [];
        var gitRepos = new List<DownloadAndExtractRecordService> { };
        gitRepos.AddRange(
            addons
                .Values.Where(x => x is GithubRecord)
                .GroupBy(x =>
                {
                    var githubNameMatch = GithubRx().Match(x.DlLink!);
                    return $"{githubNameMatch.Groups["repo"]}-{githubNameMatch.Groups["archive"]}";
                })
                .Select(group =>
                {
                    var first = group.First();
                    var githubNameMatch = GithubRx().Match(first.DlLink!);
                    var repoName =
                        $"{githubNameMatch.Groups["repo"]}-{githubNameMatch.Groups["archive"]}";
                    var repoPath = Path.Join(cache, repoName);

                    return downloadAndExtractGitRepoFactory.Create(
                        first.AddonName![..Math.Min(first.AddonName!.Length, 35)].PadRight(40),
                        repoPath,
                        "",
                        "",
                        dlFunc: (_, pct, _) =>
                            downloadAddon.DownloadAsync(first, repoPath, _ => { }, pct),
                        extractFunc: pct =>
                            ExtractAddonGroup.Extract(repoName, repoPath, gammaModsPath, group, pct)
                    );
                })
        );
        progress.TotalAddons += gitRepos.Count;
        modDbAddons.AddRange(gitRepos);
        await Parallel.ForEachAsync(
            modDbAddons,
            new ParallelOptions { MaxDegreeOfParallelism = downloadThreads },
            async (grs, _) =>
            {
                try
                {
                    await grs.DownloadAndExtractAsync();
                }
                catch (Exception e)
                {
                    _logger.Warning(
                        """
                        Error processing addon: {Addon}
                        {Exception}
                        """,
                        grs.RepoUrl,
                        e
                    );
                    brokenAddons.Add(grs);
                }
            }
        );

        foreach (var brokenAddon in brokenAddons)
        {
            try
            {
                await brokenAddon.DownloadAndExtractAsync(invalidateMirror: true);
            }
            catch (Exception e)
            {
                _logger.Error(
                    """
                    Error processing addon: {Addon}
                    {Exception}
                    """,
                    brokenAddon.RepoUrl,
                    e
                );
            }
        }

        brokenAddons.Clear();

        var specialGitRepos = new List<DownloadAndExtractRecordService>
        {
            downloadAndExtractGitRepoFactory.Create(
                GammaSetupRepo.Name,
                Path.Join(cache, GammaSetupRepo.Name),
                gammaModsPath,
                gammaSetupRepoUrl,
                dlFunc: (_, pct, _) => GammaSetupRepo.DownloadAsync(cache, gammaSetupRepoUrl, pct),
                extractFunc: pct =>
                    GammaSetupRepo.ExtractAsync(cache, gammaModsPath, anomalyBinPath, pct),
                extractPrereqTask: anomalyTask,
                cancellationToken: cancellationToken
            ),
            downloadAndExtractGitRepoFactory.Create(
                StalkerGammaRepo.Name,
                Path.Join(cache, StalkerGammaRepo.Name),
                gammaModsPath,
                stalkerGammaRepoUrl,
                dlFunc: (_, pct, _) =>
                    StalkerGammaRepo.DownloadAsync(cache, stalkerGammaRepoUrl, pct),
                extractFunc: pct => StalkerGammaRepo.Extract(cache, gammaModsPath, anomaly, pct),
                extractPrereqTask: anomalyTask,
                cancellationToken: cancellationToken
            ),
            downloadAndExtractGitRepoFactory.Create(
                GammaLargeFiles.Name,
                Path.Join(cache, GammaLargeFiles.Name),
                gammaModsPath,
                gammaLargeFilesRepoUrl,
                dlFunc: (_, pct, _) =>
                    GammaLargeFiles.DownloadAsync(cache, gammaLargeFilesRepoUrl, pct),
                extractFunc: pct => GammaLargeFiles.Extract(cache, gammaModsPath, pct),
                cancellationToken: cancellationToken
            ),
            downloadAndExtractGitRepoFactory.Create(
                TeivazAnomalyGunslingerRepo.Name,
                Path.Join(cache, TeivazAnomalyGunslingerRepo.Name),
                gammaModsPath,
                teivazAnomalyGunslingerRepoUrl,
                dlFunc: (_, pct, _) =>
                    TeivazAnomalyGunslingerRepo.DownloadAsync(
                        cache,
                        teivazAnomalyGunslingerRepoUrl,
                        pct
                    ),
                extractFunc: pct => TeivazAnomalyGunslingerRepo.Extract(cache, gammaModsPath, pct),
                cancellationToken: cancellationToken
            ),
        };
        progress.TotalAddons += specialGitRepos.Count;

        await Parallel.ForEachAsync(
            specialGitRepos,
            new ParallelOptions { MaxDegreeOfParallelism = downloadThreads },
            async (grs, _) =>
            {
                try
                {
                    await grs.DownloadAndExtractAsync();
                }
                catch (Exception e)
                {
                    _logger.Warning(
                        """
                        Error processing addon: {Addon}
                        {Exception}
                        """,
                        grs.RepoUrl,
                        e
                    );
                    brokenAddons.Add(grs);
                }
            }
        );

        foreach (var brokenAddon in brokenAddons)
        {
            try
            {
                await brokenAddon.DownloadAndExtractAsync();
            }
            catch (Exception e)
            {
                _logger.Error(
                    """
                    Error processing addon: {Addon}
                    {Exception}
                    """,
                    brokenAddon.RepoUrl,
                    e
                );
            }
        }

        await downloadModOrganizerService.DownloadAsync(
            cachePath: cache,
            extractPath: gamma,
            version: mo2Version,
            cancellationToken: cancellationToken
        );

        await installModOrganizerGammaProfile.InstallAsync(
            Path.Join(gamma, "downloads", StalkerGammaRepo.Name),
            gamma
        );

        await writeModOrganizerIniService.WriteAsync(gamma, anomaly, mo2Version);

        await disableNexusModHandlerLink.DisableAsync(gamma);

        _logger.Information("Install finished");
    }

    [GeneratedRegex(
        @"https://github.com/(?<profile>[\w\-_]*)/+?(?<repo>[\w\-_\.]*).*/(?<archive>.*)\.+?"
    )]
    private static partial Regex GithubRx();
}
