using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices.SpecialRepos;

public interface ITeivazAnomalyGunslingerRepo : IDownloadableRecord;

public class TeivazAnomalyGunslingerRepo(GammaProgress gammaProgress, string gammaDir, string url)
    : ITeivazAnomalyGunslingerRepo
{
    public string Name { get; } = "teivaz_anomaly_gunslinger";
    public string ArchiveName { get; } = "";
    protected string Url = url;
    private string RepoPath => Path.Join(gammaDir, "downloads", Name);
    private string GammaModsDir => Path.Join(gammaDir, "mods");

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
        foreach (
            var gameDataDir in new DirectoryInfo(RepoPath).EnumerateDirectories(
                "gamedata",
                SearchOption.AllDirectories
            )
        )
        {
            DirUtils.CopyDirectory(
                gameDataDir.FullName,
                Path.Join(
                    GammaModsDir,
                    "312- Gunslinger Guns for Anomaly - Teivazcz & Gunslinger Team",
                    "gamedata"
                ),
                overwrite: true,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                    )
            );
        }

        gammaProgress.IncrementCompletedMods();
        return Task.CompletedTask;
    }
}
