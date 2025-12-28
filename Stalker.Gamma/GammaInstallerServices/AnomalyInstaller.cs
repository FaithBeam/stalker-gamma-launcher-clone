using Stalker.Gamma.Models;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

public interface IAnomalyInstaller : IDownloadableRecord;

public class AnomalyInstaller(GammaProgress gammaProgress, string gammaDir, string anomalyDir)
    : IAnomalyInstaller
{
    public string Name { get; } = "Stalker Anomaly";
    public string ArchiveName { get; } = "Stalker_Anomaly.7z";
    protected string StalkerAnomalyUrl = "https://www.moddb.com/downloads/start/277404";
    protected string StalkerAnomalyMd5 = "d6bce51a4e6d98f9610ef0aa967ba964";
    private string DownloadPath => Path.Join(gammaDir, "downloads", "Stalker_Anomaly.7z");
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
                            new GammaProgress.GammaInstallProgressEventArgs(Name, "Check MD5", pct)
                        )
                ) != StalkerAnomalyMd5
            )
        )
        {
            await ModDbUtility.GetModDbLinkCurl(
                StalkerAnomalyUrl,
                DownloadPath,
                pct =>
                    gammaProgress.OnProgressChanged(
                        new GammaProgress.GammaInstallProgressEventArgs(Name, "Download", pct)
                    ),
                cancellationToken
            );
        }
    }

    public virtual async Task ExtractAsync(CancellationToken cancellationToken = default)
    {
        await SevenZipUtility.ExtractAsync(
            DownloadPath,
            ExtractPath,
            pct =>
                gammaProgress.OnProgressChanged(
                    new GammaProgress.GammaInstallProgressEventArgs(Name, "Extract", pct)
                ),
            cancellationToken: cancellationToken
        );
        gammaProgress.IncrementCompletedMods();
    }
}

public class AnomalyInstallerException(string message, Exception innerException)
    : Exception(message, innerException);
