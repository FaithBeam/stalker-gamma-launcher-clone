using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using LibGit2Sharp;
using stalker_gamma.cli.Services.Enums;
using stalker_gamma.cli.Services.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Services;

public class CustomGammaInstaller(ICurlService curlService, IHttpClientFactory hcf, ModDb modDb)
{
    public async Task InstallAsync(string gammaPath, string? cachePath = null)
    {
        const string githubUrl = "https://github.com/{0}/{1}";

        #region Download Mods from ModDb and GitHub

        var gammaApiResponse = await JsonSerializer.DeserializeAsync(
            await _hc.GetStreamAsync("https://stalker-gamma.com/web/api/v1/mods/list"),
            jsonTypeInfo: StalkerGammaApiCtx.Default.StalkerGammaApiResponse
        );

        Directory.CreateDirectory(cachePath!);
        Directory.CreateDirectory(gammaPath);
        var modsJsonPath = Path.Join(cachePath, "mods.json");
        await File.WriteAllTextAsync(
            modsJsonPath,
            JsonSerializer.Serialize(
                gammaApiResponse,
                StalkerGammaApiCtx.Default.StalkerGammaApiResponse
            )
        );

        var indexedResponse = gammaApiResponse!
            .Main.Select((x, i) => (x, i))
            .ToFrozenDictionary(x => x.i + 1, x => x.x);

        var groksMainMenuThemeArchivePath = Path.Join(cachePath, "Groks_main_menu_theme.zip");

        var addons = indexedResponse
            // moddb
            .Where(kvp =>
                kvp.Value.AddonId is not null
                && !string.IsNullOrWhiteSpace(kvp.Value.Url)
                && !PlaceHolderUrls.Contains(kvp.Value.Url)
            )
            .Select(kvp =>
                MainToAddonRecord(
                    kvp.Key,
                    kvp.Value,
                    cachePath,
                    gammaPath,
                    AddonType.ModDb,
                    pct => Console.WriteLine($"{kvp.Value.Title} - {pct:F2}")
                )
            )
            // placeholders
            .Concat(
                indexedResponse
                    .Where(kvp =>
                        !string.IsNullOrWhiteSpace(kvp.Value.Url)
                        && PlaceHolderUrls.Contains(kvp.Value.Url)
                    )
                    .Select(kvp => new AddonRecord(
                        kvp.Key,
                        kvp.Value.Title!,
                        kvp.Value.Url!,
                        "https://www.moddb.com/addons/start/222467",
                        kvp.Value.Md5Hash,
                        groksMainMenuThemeArchivePath,
                        Path.Join(gammaPath, $"{kvp.Key}-{kvp.Value.Title} - {kvp.Value.Uploader}"),
                        [],
                        AddonType.ModDb,
                        pct => Console.WriteLine($"{kvp.Value.Title} - {pct:F2}")
                    ))
            )
            // github
            .Concat(
                indexedResponse
                    .Where(kvp =>
                        kvp.Value.AddonId is null
                        && !string.IsNullOrWhiteSpace(kvp.Value.Url)
                        && kvp.Value.Url.Contains("github")
                        && !PlaceHolderUrls.Contains(kvp.Value.Url)
                    )
                    .Select(kvp =>
                        MainToAddonRecord(
                            kvp.Key,
                            kvp.Value,
                            cachePath,
                            gammaPath,
                            AddonType.GitHub,
                            pct => Console.WriteLine($"{kvp.Value.Title} - {pct:F2}")
                        )
                    )
            )
            .GroupBy(x => x.ArchiveDlPath)
            .OrderBy(x => x.First().Index);

        // Write separators
        var separators = indexedResponse
            .Where(kvp => kvp.Value.AddonId is null && string.IsNullOrWhiteSpace(kvp.Value.Url))
            .Select(kvp => MainToSeparator(kvp, gammaPath));
        foreach (var separator in separators)
        {
            Directory.CreateDirectory(separator);
            await File.WriteAllTextAsync(
                Path.Join(separator, "separator_meta.ini"),
                SeparatorMetaIni
            );
        }

        ConcurrentBag<IGrouping<string, AddonRecord>> brokenAddons = new();

        // download
        var dlChannel = Channel.CreateUnbounded<IGrouping<string, AddonRecord>>();
        var dlTask = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                addons,
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
                async (group, t) =>
                {
                    try
                    {
                        await DownloadAndVerifyFile(group, invalidateMirror: true, t);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"WARNING: Unable to download {group.Key}: {e.Message}");
                        brokenAddons.Add(group);
                    }

                    dlChannel.Writer.TryWrite(group);
                }
            );
            dlChannel.Writer.TryComplete();
        });

        var extractTask = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                dlChannel.Reader.ReadAllAsync(),
                new ParallelOptions { MaxDegreeOfParallelism = 4 },
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
                Console.WriteLine($"ERROR Processing broken addon: {brokenAddon.Key} {e.Message}");
            }
        }

        #endregion

        #region Download Base Git Repos

        var stalkerGammaRepoPath = Path.Join(cachePath, StalkerGammaRepo);
        var gammaLargeFilesRepoPath = Path.Join(cachePath, GammaLargeFilesRepo);
        var teivazAnomalyGunslingerRepoPath = Path.Join(cachePath, TeivazAnomalyGunslingerRepo);
        var gammaSetupRepoPath = Path.Join(cachePath, GammaSetupRepo);

        var t1 = Task.Run(() =>
        {
            if (Directory.Exists(gammaSetupRepoPath))
            {
                var gammaSetupRepo = new Repository(gammaSetupRepoPath);
                LibGit2Sharp.Commands.Pull(gammaSetupRepo, MySig, null);
            }
            else
            {
                LibGit2Sharp.Repository.Clone(
                    string.Format(githubUrl, GitAuthor, GammaSetupRepo),
                    gammaSetupRepoPath
                );
            }
            DirUtils.CopyDirectory(
                Path.Join(gammaSetupRepoPath, "modpack_addons"),
                Path.Combine(gammaPath, "G.A.M.M.A"),
                onProgress: (copied, total) =>
                    Console.WriteLine($"Gamma Setup: {(double)copied / total * 100:F2}%")
            );

            if (Directory.Exists(stalkerGammaRepoPath))
            {
                var stalkerGammaRepo = new Repository(stalkerGammaRepoPath);
                LibGit2Sharp.Commands.Pull(stalkerGammaRepo, MySig, null);
            }
            else
            {
                LibGit2Sharp.Repository.Clone(
                    string.Format(githubUrl, GitAuthor, StalkerGammaRepo),
                    stalkerGammaRepoPath
                );
            }
            DirUtils.CopyDirectory(
                Path.Combine(stalkerGammaRepoPath, "G.A.M.M.A", "modpack_addons"),
                Path.Combine(gammaPath, "G.A.M.M.A"),
                overwrite: true,
                onProgress: (copied, total) =>
                    Console.WriteLine($"Stalker GAMMA: {(double)copied / total * 100:F2}%")
            );
            File.Copy(
                Path.Combine(stalkerGammaRepoPath, "G.A.M.M.A_definition_version.txt"),
                Path.Combine(gammaPath, "version.txt"),
                true
            );
        });

        var t2 = Task.Run(() =>
        {
            if (Directory.Exists(gammaLargeFilesRepoPath))
            {
                var gammaLargeFilesRepo = new Repository(gammaLargeFilesRepoPath);
                LibGit2Sharp.Commands.Pull(gammaLargeFilesRepo, MySig, null);
            }
            else
            {
                LibGit2Sharp.Repository.Clone(
                    string.Format(githubUrl, GitAuthor, GammaLargeFilesRepo),
                    gammaLargeFilesRepoPath
                );
            }
            DirUtils.CopyDirectory(
                gammaLargeFilesRepoPath,
                gammaPath,
                overwrite: true,
                onProgress: (count, total) =>
                    Console.WriteLine($"GAMMA Large Files: {(double)count / total * 100:F2}%")
            );
        });

        var t3 = Task.Run(() =>
        {
            if (Directory.Exists(teivazAnomalyGunslingerRepoPath))
            {
                var teivazAnomalyGunslingerRepo = new Repository(teivazAnomalyGunslingerRepoPath);
                LibGit2Sharp.Commands.Pull(teivazAnomalyGunslingerRepo, MySig, null);
            }
            else
            {
                LibGit2Sharp.Repository.Clone(
                    string.Format(githubUrl, GitAuthor, TeivazAnomalyGunslingerRepo),
                    teivazAnomalyGunslingerRepoPath
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
                        gammaPath,
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

        await Task.WhenAll(t1, t2, t3);

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

            await ArchiveUtility.ExtractWithProgress(
                addonRecord.ArchiveDlPath,
                addonRecord.ExtractDirectory,
                addonRecord.OnProgress
            );

            ProcessInstructions(addonRecord.ExtractDirectory, addonRecord.Instructions);

            CleanExtractPath(addonRecord.ExtractDirectory);

            await WriteAddonMetaIniAsync(
                addonRecord.ExtractDirectory,
                addonRecord.Name,
                addonRecord.Url
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
                    const int bufferSize = 1024 * 1024;
                    using var response = await _hc.GetAsync(
                        first.Url,
                        HttpCompletionOption.ResponseHeadersRead,
                        t
                    );
                    response.EnsureSuccessStatusCode();

                    var totalBytes = response.Content.Headers.ContentLength;

                    await using var fs = new FileStream(
                        first.ArchiveDlPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: bufferSize
                    );
                    await using var contentStream = await response.Content.ReadAsStreamAsync(t);

                    var buffer = ArrayPool<byte>.Shared.Rent(81920);
                    try
                    {
                        long totalBytesRead = 0;
                        int bytesRead;

                        while (
                            (
                                bytesRead = await contentStream.ReadAsync(
                                    buffer.AsMemory(0, buffer.Length),
                                    t
                                )
                            ) > 0
                        )
                        {
                            await fs.WriteAsync(buffer.AsMemory(0, bytesRead), t);
                            totalBytesRead += bytesRead;

                            if (totalBytes.HasValue)
                            {
                                var progressPercentage =
                                    (double)totalBytesRead / totalBytes.Value * 100.0;
                                first.OnProgress(progressPercentage);
                            }
                        }

                        first.OnProgress(100);
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                    }

                    break;
                }
            }
        }
    }

    private static string MainToSeparator(KeyValuePair<int, Main> kvp, string gammaDir) =>
        Path.Join(gammaDir, $"{kvp.Key}-{kvp.Value.Title}_separator");

    private static AddonRecord MainToAddonRecord(
        int idx,
        Main m,
        string? cacheDir,
        string gammaDir,
        AddonType type,
        Action<double> onProgress
    )
    {
        var urlSplit = m.Url!.Split('/');
        var archivePath = Path.Join(
            cacheDir,
            type switch
            {
                AddonType.ModDb => m.Filename,
                AddonType.GitHub => $"{urlSplit[4]}.zip",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            }
        );
        var extractDir = Path.Join(gammaDir, $"{idx}- {m.Title} - {m.Uploader}");
        var url = type switch
        {
            AddonType.ModDb => m.MirrorsUrl?.Replace("/all", ""),
            AddonType.GitHub => m.Url,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
        return new AddonRecord(
            idx,
            m.Title!,
            url!,
            m.MirrorsUrl,
            m.Md5Hash,
            archivePath,
            extractDir,
            m.Instructions,
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

        new DirectoryInfo(extractPath)
            .GetDirectories("*", SearchOption.AllDirectories)
            .ToList()
            .ForEach(di =>
            {
                di.Attributes &= ~FileAttributes.ReadOnly;
                di.GetFiles("*", SearchOption.TopDirectoryOnly)
                    .ToList()
                    .ForEach(fi => fi.IsReadOnly = false);
            });

        var dirInfo = new DirectoryInfo(extractPath);
        foreach (
            var d in dirInfo
                .GetDirectories()
                .Where(x => !DoNotMatch.Contains(x.Name, StringComparer.OrdinalIgnoreCase))
        )
        {
            d.Delete(true);
        }
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
    private static readonly Signature MySig = new(
        "stalker-gamma-clone",
        "stalker-gamma-clone@github.com",
        DateTimeOffset.Now
    );

    private static readonly IReadOnlyList<string> PlaceHolderUrls =
    [
        "https://www.moddb.com/addons/start/222467",
        "https://www.moddb.com/addons/groks-main-menu-theme-deathcard-cabin",
        "https://www.moddb.com/mods/stalker-anomaly/addons/groks-main-menu-theme-deathcard-cabin",
        "https://github.com/Grokitach/gamma_large_files_v2",
        "https://github.com/Grokitach/teivaz_anomaly_gunslinger",
    ];

    private readonly ICurlService _curlService = curlService;
    private readonly IHttpClientFactory _hcf = hcf;
    private readonly ModDb _modDb = modDb;
    private readonly HttpClient _hc = hcf.CreateClient("githubDlArchive");

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
        string name,
        string modDbUrl
    ) =>
        await File.WriteAllTextAsync(
            Path.Join(extractPath, "meta.ini"),
            $"""
            [General]
            gameName=stalkeranomaly
            modid=0
            ignoredversion={name}
            version={name}
            newestversion={name}
            category="-1,"
            nexusFileStatus=1
            installationFile={name}
            repository=
            comments=
            notes=
            nexusDescription=
            url={modDbUrl}
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

            """,
            encoding: Encoding.UTF8
        );
}

internal record AddonRecord(
    int Index,
    string Name,
    string Url,
    string? MirrorUrl,
    string? Md5,
    string ArchiveDlPath,
    string ExtractDirectory,
    string[] Instructions,
    AddonType AddonType,
    Action<double> OnProgress
);
