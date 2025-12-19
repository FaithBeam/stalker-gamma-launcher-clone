using ConsoleAppFramework;
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
    InstallModOrganizerProfile installModOrganizerProfile,
    DowngradeModOrganizer downgradeModOrganizer,
    WriteModOrganizerIni writeModOrganizerIni,
    DisableNexusModHandlerLink disableNexusModHandlerLink
)
{
    /// <summary>
    /// This will install/update all mods based on Stalker_GAMMA
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cacheDirectory">cache directory</param>
    /// <param name="anomalyArchiveName">Optionally change the name of the downloaded anomaly archive</param>
    /// <param name="downloadThreads">Number of parallel downloads that can occur</param>
    /// <param name="extractThreads">Number of parallel extracts that can occur</param>
    public async Task FullInstall(
        string anomaly = "anomaly",
        string gamma = "gamma",
        string cacheDirectory = "cache",
        string anomalyArchiveName = "anomaly.7z",
        int downloadThreads = 2,
        int extractThreads = 2
    )
    {
        globalSettings.DownloadThreads = downloadThreads;
        globalSettings.ExtractThreads = extractThreads;
        var anomalyCacheArchivePath = Path.Join(cacheDirectory, anomalyArchiveName);

        var anomalyTask = Task.Run(async () =>
            await anomalyInstaller.DownloadAndExtractAsync(
                anomalyCacheArchivePath,
                anomaly,
                AnomalyProgress
            )
        );

        var gammaTask = Task.Run(async () =>
            await gammaInstaller.InstallAsync(anomaly, anomalyTask, gamma, cacheDirectory)
        );

        var downgradeModOrganizerTask = Task.Run(async () =>
            await downgradeModOrganizer.DowngradeAsync(
                cachePath: cacheDirectory,
                extractPath: gamma
            )
        );

        await Task.WhenAll(anomalyTask, gammaTask, downgradeModOrganizerTask);

        await installModOrganizerProfile.InstallAsync(
            Path.Join(gamma, "downloads", "Stalker_GAMMA"),
            gamma
        );
        await writeModOrganizerIni.WriteAsync(gamma, anomaly);
        await disableNexusModHandlerLink.DisableAsync(gamma);

        Console.WriteLine("[+] Setup ended... Enjoy your journey in the Zone o/");

        return;

        void AnomalyProgress(double pct) => Console.WriteLine($"[+] Anomaly: {pct}%");
    }
}
