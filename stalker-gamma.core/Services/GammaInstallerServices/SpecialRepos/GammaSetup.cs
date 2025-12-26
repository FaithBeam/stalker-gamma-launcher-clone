using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices.SpecialRepos;

public static class GammaSetupRepo
{
    public const string Name = "gamma_setup";

    public static async Task DownloadAsync(string cache, string repoUrl, Action<double> pct) =>
        await DownloadSpecialGitRepo.DownloadAsync(Path.Join(cache, Name), repoUrl, pct);

    public static Task ExtractAsync(
        string cache,
        string gammaModsPath,
        string anomalyBinPath,
        Action<double> pct
    )
    {
        DirUtils.CopyDirectory(
            Path.Join(cache, Name, "modpack_addons"),
            gammaModsPath,
            onProgress: pct
        );
        DirUtils.CopyDirectory(
            Path.Join(cache, Name, "modpack_patches"),
            anomalyBinPath,
            onProgress: pct
        );
        return Task.CompletedTask;
    }
}
