using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices.SpecialRepos;

public static class StalkerGammaRepo
{
    public const string Name = "Stalker_GAMMA";

    public static async Task DownloadAsync(string cache, string repoUrl, Action<double> pct) =>
        await DownloadSpecialGitRepo.DownloadAsync(Path.Join(cache, Name), repoUrl, pct);

    public static Task Extract(
        string cache,
        string gammaModsPath,
        string anomaly,
        Action<double> pct
    )
    {
        DirUtils.CopyDirectory(
            Path.Join(cache, Name, "G.A.M.M.A", "modpack_addons"),
            gammaModsPath,
            onProgress: pct
        );
        DirUtils.CopyDirectory(
            Path.Join(cache, Name, "G.A.M.M.A", "modpack_patches"),
            anomaly,
            onProgress: pct
        );
        File.Copy(
            Path.Join(cache, Name, "G.A.M.M.A_definition_version.txt"),
            Path.Join(gammaModsPath, "..", "version.txt"),
            true
        );
        return Task.CompletedTask;
    }
}
