using System.Collections.Frozen;
using System.Text.Json;
using System.Threading.Channels;
using LibGit2Sharp;
using stalker_gamma.cli.Services.Enums;
using stalker_gamma.cli.Services.Models;
using stalker_gamma.core.Services;

namespace stalker_gamma.cli.Services;

public class CustomGammaInstaller(ICurlService curlService, IHttpClientFactory hcf)
{
    public async Task InstallAsync(string gammaPath, string? cachePath = null)
    {
        const string githubUrl = "https://github.com/{0}/{1}";

        #region Download Mods from ModDb and GitHub

        var gammaApiResponse = await JsonSerializer.DeserializeAsync(
            await _hc.GetStreamAsync("https://stalker-gamma.com/web/api/v1/mods/list"),
            jsonTypeInfo: StalkerGammaApiCtx.Default.StalkerGammaApiResponse
        );

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
                    Console.WriteLine
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
                        kvp.Value.MirrorsUrl,
                        kvp.Value.Md5Hash,
                        groksMainMenuThemeArchivePath,
                        Path.Join(gammaPath, $"{kvp.Key}-{kvp.Value.Title} - {kvp.Value.Uploader}"),
                        [],
                        AddonType.ModDb,
                        Console.WriteLine
                    ))
            )
            // github
            .Concat(
                indexedResponse
                    .Where(kvp =>
                        kvp.Value.AddonId is null
                        && !string.IsNullOrWhiteSpace(kvp.Value.Url)
                        && kvp.Value.Url.Contains("github")
                    )
                    .Select(kvp =>
                        MainToAddonRecord(
                            kvp.Key,
                            kvp.Value,
                            cachePath,
                            gammaPath,
                            AddonType.GitHub,
                            Console.WriteLine
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

        // download
        var dlChannel = Channel.CreateUnbounded<IList<AddonRecord>>();
        foreach (var group in addons)
        {
            var t1 = Task.Run(async () => { });
        }
        #endregion


        #region Download Base Git Repos

        var stalkerGammaRepoPath = Path.Join(cachePath, StalkerGammaRepo);
        var gammaLargeFilesRepoPath = Path.Join(cachePath, GammaLargeFilesRepo);
        var teivazAnomalyGunslingerRepoPath = Path.Join(cachePath, TeivazAnomalyGunslingerRepo);
        var gammaSetupRepoPath = Path.Join(cachePath, GammaSetupRepo);

        var t1 = Task.Run(() =>
        {
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
        });

        var t4 = Task.Run(() =>
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
        });

        await Task.WhenAll(t1, t2, t3, t4);

        #endregion

        // copy version from stalker gamma repo to cache path
        File.Copy(
            Path.Join(stalkerGammaRepoPath, "G.A.M.M.A_definition_version.txt"),
            Path.Join(cachePath, "version.txt")
        );
    }

    private string MainToSeparator(KeyValuePair<int, Main> kvp, string gammaDir) =>
        Path.Join(gammaDir, $"{kvp.Key}-{kvp.Value.Title}_separator");

    private AddonRecord MainToAddonRecord(
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
        var extractDir = Path.Join(gammaDir, $"{idx}-{m.Title} - {m.Uploader}");
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
    private readonly HttpClient _hc = hcf.CreateClient();

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
