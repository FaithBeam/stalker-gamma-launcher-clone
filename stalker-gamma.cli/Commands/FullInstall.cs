using System.Reactive;
using System.Reactive.Linq;
using ConsoleAppFramework;
using Serilog;
using Stalker.Gamma.GammaInstallerServices;
using Stalker.Gamma.Models;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class FullInstallCmd(
    ILogger logger,
    StalkerGammaSettings stalkerGammaSettings,
    GammaInstaller gammaInstaller
// AddFoldersToWinDefenderExclusionService addFoldersToWinDefenderExclusionService,
// EnableLongPathsOnWindowsService enableLongPathsOnWindowsService,
// ProgressService progress
)
{
    /// <summary>
    /// This will install/update Anomaly and all GAMMA addons.
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cache">Cache directory</param>
    /// <param name="downloadThreads">Number of parallel downloads that can occur</param>
    /// <param name="addFoldersToWinDefenderExclusion">(Windows) Add the anomaly, gamma, and cache folders to the Windows Defender Exclusion list</param>
    /// <param name="enableLongPaths">(Windows) Enable long paths</param>
    /// <param name="mo2Version">The version of Mod Organizer 2 to download</param>
    /// <param name="progressUpdateIntervalMs">How frequently to write progress to the console in milliseconds</param>
    /// <param name="stalkerAddonApiUrl">Escape hatch for stalker gamma api</param>
    /// <param name="customModListUrl">Download a custom MO2 GAMMA profile modlist.txt. Use in conjunction with --stalker-addon-api-url "".</param>
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
        [Hidden] string? mo2Version = null,
        [Hidden] long progressUpdateIntervalMs = 250,
        [Hidden] string stalkerAddonApiUrl = "https://stalker-gamma.com/api/list",
        [Hidden] string? customModListUrl = null,
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
        stalkerGammaSettings.DownloadThreads = downloadThreads;
        stalkerGammaSettings.ListStalkerModsUrl = stalkerAddonApiUrl;
        stalkerGammaSettings.GammaSetupRepo = gammaSetupRepoUrl;
        stalkerGammaSettings.StalkerGammaRepo = stalkerGammaRepoUrl;
        stalkerGammaSettings.GammaLargeFilesRepo = gammaLargeFilesRepoUrl;
        stalkerGammaSettings.TeivazAnomalyGunslingerRepo = teivazAnomalyGunslingerRepoUrl;
        var resourcesPath = Path.GetFullPath("resources");
        stalkerGammaSettings.PathToCurl = Path.Join(
            resourcesPath,
            OperatingSystem.IsWindows() ? "curl.exe" : "curl-impersonate"
        );
        stalkerGammaSettings.PathTo7Z = Path.Join(
            resourcesPath,
            OperatingSystem.IsWindows() ? "7zz.exe" : "7zz"
        );
        stalkerGammaSettings.PathToGit = OperatingSystem.IsWindows()
            ? Path.Join(resourcesPath, "git", "bin", "git.exe")
            : "git";
        var gammaProgressObservable = Observable
            .FromEventPattern<GammaProgress.GammaInstallProgressEventArgs>(
                handler => gammaInstaller.Progress.ProgressChanged += handler,
                handler => gammaInstaller.Progress.ProgressChanged -= handler
            )
            .Select(x => x.EventArgs);
        var gammaProgressDisposable = gammaProgressObservable
            .Sample(TimeSpan.FromMilliseconds(progressUpdateIntervalMs))
            .Subscribe(OnProgressChanged);
        try
        {
            await gammaInstaller.FullInstallAsync(
                anomaly,
                gamma,
                cache,
                mo2Version,
                cancellationToken
            );
            _logger.Information("Install finished");
        }
        finally
        {
            gammaProgressDisposable.Dispose();
        }
    }

    private void OnProgressChanged(GammaProgress.GammaInstallProgressEventArgs e) =>
        _logger.Information(
            StructuredLog,
            e.Name[..Math.Min(e.Name.Length, 35)].PadRight(40),
            e.ProgressType.PadRight(10),
            $"{e.Progress:P2}".PadRight(8),
            e.TotalProgress
        );

    private readonly ILogger _logger = logger;
    private const string StructuredLog =
        "{AddonName} | {Operation} | {Percent} | {TotalProgress:P2}";
}
