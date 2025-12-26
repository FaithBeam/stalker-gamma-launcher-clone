using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public static class DownloadSpecialGitRepo
{
    public static async Task DownloadAsync(
        string repoPath,
        string repoUrl,
        Action<double> onDlProgress
    )
    {
        if (Directory.Exists(repoPath))
        {
            await GitUtility.PullGitRepo(repoPath, onProgress: onDlProgress);
        }
        else
        {
            await GitUtility.CloneGitRepo(repoPath, repoUrl, onProgress: onDlProgress);
        }
    }
}
