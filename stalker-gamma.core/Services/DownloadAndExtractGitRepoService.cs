using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services;

public class DownloadAndExtractGitRepoService(GitUtility gu)
{
    public async Task DownloadAndExtractAsync(
        string repoPath,
        string destinationDir,
        string repoUrl,
        Action<double> onDlProgress,
        Action<double> onExtractProgress,
        Action onComplete
    )
    {
        if (Directory.Exists(repoPath))
        {
            await _gu.PullGitRepo(repoPath, onProgress: onDlProgress);
        }
        else
        {
            await _gu.CloneGitRepo(repoPath, repoUrl, onProgress: onDlProgress);
        }
        DirUtils.CopyDirectory(repoPath, destinationDir, onProgress: onExtractProgress);

        onComplete();
    }

    private readonly GitUtility _gu = gu;
}
