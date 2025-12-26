using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices.SpecialRepos;

public static class TeivazAnomalyGunslingerRepo
{
    public const string Name = "teivaz_anomaly_gunslinger";

    public static async Task DownloadAsync(string cache, string repoUrl, Action<double> pct) =>
        await DownloadSpecialGitRepo.DownloadAsync(Path.Join(cache, Name), repoUrl, pct);

    public static Task Extract(string cache, string gammaModsPath, Action<double> pct)
    {
        foreach (
            var gameDataDir in new DirectoryInfo(Path.Join(cache, Name)).EnumerateDirectories(
                "gamedata",
                SearchOption.AllDirectories
            )
        )
        {
            DirUtils.CopyDirectory(
                gameDataDir.FullName,
                Path.Join(
                    gammaModsPath,
                    "312- Gunslinger Guns for Anomaly - Teivazcz & Gunslinger Team",
                    "gamedata"
                ),
                overwrite: true,
                onProgress: pct
            );
        }
        return Task.CompletedTask;
    }
}
