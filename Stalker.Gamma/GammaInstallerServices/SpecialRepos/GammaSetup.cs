using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices.SpecialRepos;

public interface IGammaSetupRepo : IDownloadableRecord;

public class GammaSetupRepo(
    GammaProgress gammaProgress,
    string gammaDir,
    string anomalyDir,
    string url
) : IGammaSetupRepo
{
    public string Name { get; } = "gamma_setup";
    public string ArchiveName { get; } = "";
    protected string Url = url;
    private string RepoPath => Path.Join(gammaDir, "downloads", Name);
    private string GammaModsDir => Path.Join(gammaDir, "mods");
    private string AnomalyDir => anomalyDir;

    public virtual async Task DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (Directory.Exists(RepoPath))
        {
            await GitUtility.PullGitRepo(
                RepoPath,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                ct: cancellationToken
            );
        }
        else
        {
            await GitUtility.CloneGitRepo(
                RepoPath,
                Url,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                ct: cancellationToken
            );
        }
    }

    public virtual Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        DirUtils.CopyDirectory(
            Path.Join(RepoPath, "modpack_addons"),
            GammaModsDir,
            onProgress: pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                )
        );
        DirUtils.CopyDirectory(
            Path.Join(RepoPath, "modpack_patches"),
            AnomalyDir,
            onProgress: pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                )
        );
        gammaProgress.IncrementCompletedMods();
        return Task.CompletedTask;
    }
}
