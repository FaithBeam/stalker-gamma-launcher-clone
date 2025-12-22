using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.ModOrganizer;
using stalker_gamma.core.Services.ModOrganizer.DowngradeModOrganizer;
using AnomalyInstaller = stalker_gamma.cli.Services.AnomalyInstaller;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class FullInstallCmd(
    GlobalSettings globalSettings,
    AnomalyInstaller anomalyInstaller,
    CustomGammaInstaller gammaInstaller,
    InstallModOrganizerGammaProfile installModOrganizerGammaProfile,
    DowngradeModOrganizer downgradeModOrganizer,
    WriteModOrganizerIni writeModOrganizerIni,
    DisableNexusModHandlerLink disableNexusModHandlerLink,
    ILogger logger
)
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// This will install/update all mods based on Stalker_GAMMA
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cache">cache directory</param>
    /// <param name="anomalyArchiveName">Optionally change the name of the downloaded anomaly archive</param>
    /// <param name="downloadThreads">Number of parallel downloads that can occur</param>
    /// <param name="extractThreads">Number of parallel extracts that can occur</param>
    /// <param name="stalkerAddonApiUrl">Escape hatch for stalker gamma api</param>
    /// <param name="gammaSetupRepoUrl">Escape hatch for git repo gamma_setup</param>
    /// <param name="stalkerGammaRepoUrl">Escape hatch for git repo Stalker_GAMMA</param>
    /// <param name="gammaLargeFilesRepoUrl">Escape hatch for git repo gamma_large_files_v2</param>
    /// <param name="teivazAnomalyGunslingerRepoUrl">Escape hatch for git repo teivaz_anomaly_gunslinger</param>
    /// <param name="stalkerAnomalyModdbUrl">Escape hatch for Stalker Anomaly</param>
    /// <param name="stalkerAnomalyArchiveMd5">The hash of the archive downloaded from --stalker-anomaly-moddb-url</param>
    public async Task FullInstall(
        string anomaly = "anomaly",
        string gamma = "gamma",
        string cache = "cache",
        string anomalyArchiveName = "anomaly.7z",
        int downloadThreads = 2,
        int extractThreads = 2,
        string stalkerAddonApiUrl = "https://stalker-gamma.com/api/list",
        string gammaSetupRepoUrl = "https://github.com/Grokitach/gamma_setup",
        string stalkerGammaRepoUrl = "https://github.com/Grokitach/Stalker_GAMMA",
        string gammaLargeFilesRepoUrl = "https://github.com/Grokitach/gamma_large_files_v2",
        string teivazAnomalyGunslingerRepoUrl =
            "https://github.com/Grokitach/teivaz_anomaly_gunslinger",
        string stalkerAnomalyModdbUrl = "https://www.moddb.com/downloads/start/277404",
        string stalkerAnomalyArchiveMd5 = "d6bce51a4e6d98f9610ef0aa967ba964"
    )
    {
        globalSettings.DownloadThreads = downloadThreads;
        globalSettings.ExtractThreads = extractThreads;
        globalSettings.StalkerAddonApiUrl = stalkerAddonApiUrl;
        globalSettings.GammaSetupRepo = gammaSetupRepoUrl;
        globalSettings.StalkerGammaRepo = stalkerGammaRepoUrl;
        globalSettings.GammaLargeFilesRepo = gammaLargeFilesRepoUrl;
        globalSettings.TeivazAnomalyGunslingerRepo = teivazAnomalyGunslingerRepoUrl;
        globalSettings.StalkerAnomalyModDbUrl = stalkerAnomalyModdbUrl;
        globalSettings.StalkerAnomalyArchiveMd5 = stalkerAnomalyArchiveMd5;

        var anomalyCacheArchivePath = Path.Join(cache, anomalyArchiveName);

        var anomalyTask = Task.Run(async () =>
            await anomalyInstaller.DownloadAndExtractAsync(anomalyCacheArchivePath, anomaly)
        );

        var gammaTask = Task.Run(async () =>
            await gammaInstaller.InstallAsync(anomaly, anomalyTask, gamma, cache)
        );

        var downgradeModOrganizerTask = Task.Run(async () =>
            await downgradeModOrganizer.DowngradeAsync(cachePath: cache, extractPath: gamma)
        );

        await Task.WhenAll(anomalyTask, gammaTask, downgradeModOrganizerTask);

        await installModOrganizerGammaProfile.InstallAsync(
            Path.Join(gamma, "downloads", "Stalker_GAMMA"),
            gamma
        );
        await writeModOrganizerIni.WriteAsync(gamma, anomaly);
        await disableNexusModHandlerLink.DisableAsync(gamma);

        _logger.Information("[+] Install finished");
    }
}
