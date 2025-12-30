using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices.SpecialRepos;

public interface ITeivazAnomalyGunslingerRepo : IDownloadableRecord;

public class TeivazAnomalyGunslingerRepo(
    GammaProgress gammaProgress,
    string gammaDir,
    string url,
    GitUtility gitUtility
) : ITeivazAnomalyGunslingerRepo
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
            await gitUtility.PullGitRepo(
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
            await gitUtility.CloneGitRepo(
                RepoPath,
                Url,
                onProgress: pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                ct: cancellationToken,
                extraArgs: new List<string> { "--depth", "1" }
            );
        }
    }

    public virtual Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        gammaProgress.OnDebugProgressChanged(
            new GammaProgress.GammaInstallDebugProgressEventArgs { Text = "START COPY TEIVAZ" }
        );
        var dirs = new DirectoryInfo(RepoPath)
            .EnumerateDirectories("gamedata", SearchOption.AllDirectories)
            .ToList();
        var ordered = dirs.OrderBy(d => d.Name).ToList();
        gammaProgress.OnDebugProgressChanged(
            new GammaProgress.GammaInstallDebugProgressEventArgs
            {
                Text = $"""
                UNORDERED:
                {string.Join(Environment.NewLine, dirs)}
                """,
            }
        );
        gammaProgress.OnDebugProgressChanged(
            new GammaProgress.GammaInstallDebugProgressEventArgs
            {
                Text = $"""
                ORDERED:
                {string.Join(Environment.NewLine, ordered)}
                """,
            }
        );

        foreach (var gameDataDir in ordered)
        {
            gammaProgress.OnDebugProgressChanged(
                new GammaProgress.GammaInstallDebugProgressEventArgs { Text = gameDataDir.FullName }
            );
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
                    ),
                txtProgress: txt =>
                    gammaProgress.OnDebugProgressChanged(
                        new GammaProgress.GammaInstallDebugProgressEventArgs { Text = txt }
                    )
            );
        }
        gammaProgress.OnDebugProgressChanged(
            new GammaProgress.GammaInstallDebugProgressEventArgs { Text = "END COPY TEIVAZ" }
        );

        gammaProgress.IncrementCompletedMods();
        return Task.CompletedTask;
    }
}
