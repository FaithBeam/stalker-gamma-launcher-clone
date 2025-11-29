using ConsoleAppFramework;

namespace stalker_gamma.cli.Commands;

[RegisterCommands]
public class FullInstallCmd
{
    /// <summary>
    /// This will install/update all mods based on Stalker_GAMMA
    /// </summary>
    /// <param name="anomaly">Anomaly path</param>
    /// <param name="gamma">GAMMA path</param>
    /// <param name="cacheDirectory">cache directory</param>
    public void FullInstall(string anomaly, string gamma, string? cacheDirectory = null) =>
        Console.WriteLine("Full Install");
}
