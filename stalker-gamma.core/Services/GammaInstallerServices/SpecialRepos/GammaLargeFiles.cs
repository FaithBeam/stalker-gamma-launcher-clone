using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices.SpecialRepos;

public static class GammaLargeFiles
{
    public const string Name = "gamma_large_files_v2";

    public static async Task DownloadAsync(string cache, string repoUrl, Action<double> pct) =>
        await DownloadSpecialGitRepo.DownloadAsync(Path.Join(cache, Name), repoUrl, pct);

    public static Task Extract(
        string repoPath,
        string destinationDir,
        Action<double> onExtractProgress
    )
    {
        DirUtils.CopyDirectory(repoPath, destinationDir, onProgress: onExtractProgress);
        return Task.CompletedTask;
    }
}
