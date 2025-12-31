using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

public interface IAnomalyInstaller : IDownloadableRecord;

public class AnomalyInstaller(
    GammaProgress gammaProgress,
    string gammaDir,
    string anomalyDir,
    ModDbUtility modDbUtility,
    ArchiveUtility archiveUtility
) : IAnomalyInstaller
{
    public string Name { get; } = "Stalker Anomaly";
    public string ArchiveName { get; } = "Anomaly-1.5.3-Full.2.7z";
    protected string StalkerAnomalyUrl = "https://www.moddb.com/downloads/start/277404";
    private readonly string _niceUrl =
        "https://www.moddb.com/mods/stalker-anomaly/downloads/stalker-anomaly-153";
    protected string StalkerAnomalyMd5 = "d6bce51a4e6d98f9610ef0aa967ba964";
    public string DownloadPath => Path.Join(gammaDir, "downloads", ArchiveName);
    private string ExtractPath => anomalyDir;

    public virtual async Task DownloadAsync(CancellationToken cancellationToken = default)
    {
        if (
            !File.Exists(DownloadPath)
            || (
                File.Exists(DownloadPath)
                && await Md5Utility.CalculateFileMd5Async(
                    DownloadPath,
                    pct =>
                        gammaProgress.OnProgressChanged(
                            new GammaProgress.GammaInstallProgressEventArgs(
                                Name,
                                "Check MD5",
                                pct,
                                _niceUrl
                            )
                        )
                ) != StalkerAnomalyMd5
            )
        )
        {
            await modDbUtility.GetModDbLinkCurl(
                StalkerAnomalyUrl,
                DownloadPath,
                pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(
                            Name,
                            "Download",
                            pct,
                            _niceUrl
                        )
                    ),
                cancellationToken
            );
        }
    }

    public virtual async Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        await archiveUtility.ExtractAsync(
            DownloadPath,
            ExtractPath,
            pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct, _niceUrl)
                ),
            ct: cancellationToken
        );
        gammaProgress.IncrementCompletedMods();
    }
}

public class AnomalyInstallerException(string message, Exception innerException)
    : Exception(message, innerException);
