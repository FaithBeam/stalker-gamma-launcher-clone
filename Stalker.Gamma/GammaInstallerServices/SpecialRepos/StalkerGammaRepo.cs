using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices.SpecialRepos;

public interface IStalkerGammaRepo : IDownloadableRecord;

public class StalkerGammaRepo(
    GammaProgress gammaProgress,
    string gammaDir,
    string anomalyDir,
    string url
) : IStalkerGammaRepo
{
    public string Name { get; } = "Stalker_GAMMA";
    protected string Url = url;
    public string ArchiveName { get; } = "";
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
            Path.Join(RepoPath, "G.A.M.M.A", "modpack_addons"),
            GammaModsDir,
            onProgress: pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                )
        );
        DirUtils.CopyDirectory(
            Path.Join(RepoPath, "G.A.M.M.A", "modpack_patches"),
            AnomalyDir,
            onProgress: pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                )
        );
        File.Copy(
            Path.Join(RepoPath, "G.A.M.M.A_definition_version.txt"),
            Path.Join(GammaModsDir, "..", "version.txt"),
            true
        );
        gammaProgress.IncrementCompletedMods();
        return Task.CompletedTask;
    }
}
