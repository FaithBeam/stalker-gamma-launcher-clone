using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices.SpecialRepos;

public interface IGammaLargeFilesRepo : IDownloadableRecord;

public class GammaLargeFilesRepo(
    GammaProgress gammaProgress,
    string gammaDir,
    string url,
    GitUtility gitUtility
) : IGammaLargeFilesRepo
{
    public string Name { get; } = "gamma_large_files_v2";
    public string ArchiveName { get; } = "";
    protected string Url = url;
    private string RepoPath => Path.Join(gammaDir, "downloads", Name);
    private string DestinationDir => Path.Join(gammaDir, "mods");

    public virtual async Task DownloadAsync(CancellationToken ct = default)
    {
        if (Directory.Exists(RepoPath))
        {
            await gitUtility.PullGitRepo(
                RepoPath,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                ct
            );
        }
        else
        {
            await gitUtility.CloneGitRepo(
                RepoPath,
                Url,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                ct: ct,
                extraArgs: new List<string> { "--depth", "1" }
            );
        }
    }

    public virtual Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        DirUtils.CopyDirectory(
            RepoPath,
            DestinationDir,
            onProgress: pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                )
        );
        gammaProgress.IncrementCompletedMods();
        return Task.CompletedTask;
    }
}
