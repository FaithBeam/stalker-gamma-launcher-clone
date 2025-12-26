using stalker_gamma.core.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstallerServices;

public class AnomalyInstaller(GlobalSettings globalSettings, ModDb modDb)
{
    public async Task DownloadAndExtractAsync(
        string archivePath,
        string extractDirectory,
        Action<double> md5Progress,
        Action<double> dlProgress,
        Action<double> extractProgress,
        CancellationToken? cancellationToken = null
    )
    {
        // download anomaly if archive does not exist or doesn't match md5
        if (
            !File.Exists(archivePath)
            || (
                File.Exists(archivePath)
                && await Md5Utility.CalculateFileMd5Async(archivePath, md5Progress)
                    != globalSettings.StalkerAnomalyArchiveMd5
            )
        )
        {
            await modDb.GetModDbLinkCurl(
                globalSettings.StalkerAnomalyModDbUrl,
                archivePath,
                dlProgress,
                cancellationToken
            );
        }

        // extract anomaly
        await ArchiveUtility.ExtractAsync(
            archivePath,
            extractDirectory,
            extractProgress,
            cancellationToken: cancellationToken
        );
    }
}
