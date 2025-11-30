using ConsoleAppFramework;
using stalker_gamma.core.Services;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class FullInstallCmd(AnomalyInstaller anomalyInstaller)
{
    /// <summary>
    /// This will install/update all mods based on Stalker_GAMMA
    /// </summary>
    /// <param name="anomaly">Directory to extract Anomaly to</param>
    /// <param name="gamma">Directory to extract GAMMA to</param>
    /// <param name="cacheDirectory">cache directory</param>
    public async Task FullInstall(string anomaly, string gamma, string? cacheDirectory = "cache")
    {
        var anomalyCacheArchivePath = Path.Join(cacheDirectory, "anomaly.7z");
        await _anomalyInstaller.DownloadAndExtractAsync(
            anomalyCacheArchivePath,
            anomaly,
            AnomalyProgress
        );
        return;

        void AnomalyProgress(double pct) => Console.WriteLine($"[+] Anomaly: {pct}%");
    }

    private readonly AnomalyInstaller _anomalyInstaller = anomalyInstaller;
}
