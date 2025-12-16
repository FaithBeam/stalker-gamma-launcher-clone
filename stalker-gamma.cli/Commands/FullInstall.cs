using ConsoleAppFramework;
using stalker_gamma.cli.Services;
using stalker_gamma.core.Services.DowngradeModOrganizer;
using AnomalyInstaller = stalker_gamma.cli.Services.AnomalyInstaller;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class FullInstallCmd(
    AnomalyInstaller anomalyInstaller,
    // CustomGammaInstaller gammaInstaller,
    DowngradeModOrganizer downgradeModOrganizer
)
{
    /// <summary>
    /// This will install/update all mods based on Stalker_GAMMA
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cacheDirectory">cache directory</param>
    /// <param name="anomalyArchiveName">Optionally change the name of the downloaded anomaly archive</param>
    public async Task FullInstall(
        string anomaly,
        string gamma,
        string? cacheDirectory = null,
        string anomalyArchiveName = "anomaly.7z"
    )
    {
        var cacheProvided = !string.IsNullOrWhiteSpace(cacheDirectory);
        var anomalyCacheArchivePath = cacheProvided
            ? Path.Join(cacheDirectory, anomalyArchiveName)
            : anomalyArchiveName;

        // await _anomalyInstaller.DownloadAndExtractAsync(
        //     anomalyCacheArchivePath,
        //     anomaly,
        //     AnomalyProgress
        // );

        // await gammaInstaller.InstallAsync(gamma, cacheDirectory);

        await downgradeModOrganizer.DowngradeAsync();

        return;

        // void AnomalyProgress(double pct) => Console.WriteLine($"[+] Anomaly: {pct}%");
    }

    private readonly AnomalyInstaller _anomalyInstaller = anomalyInstaller;
}
