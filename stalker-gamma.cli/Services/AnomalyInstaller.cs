using Serilog;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Services;

public class AnomalyInstaller(ModDb modDb, GlobalSettings globalSettings, ILogger logger)
{
    public async Task DownloadAndExtractAsync(
        string archivePath,
        string extractDirectory,
        Action<double> onDlProgress,
        Action<double> onExtractProgress
    )
    {
        // download anomaly if archive does not exist or doesn't match md5
        if (
            !File.Exists(archivePath)
            || (
                File.Exists(archivePath)
                && await Md5Utility.CalculateFileMd5Async(
                    archivePath,
                    (cur, total) =>
                        logger.Information(
                            "{Name} | Check MD5 | {Percent:P2}%",
                            "Anomaly".PadRight(50),
                            (double)cur / total
                        )
                ) != globalSettings.StalkerAnomalyArchiveMd5
            )
        )
        {
            await _modDb.GetModDbLinkCurl(
                globalSettings.StalkerAnomalyModDbUrl,
                archivePath,
                onDlProgress
            );
        }

        // extract anomaly
        await ArchiveUtility.ExtractAsync(archivePath, extractDirectory, onExtractProgress);
    }

    private readonly ModDb _modDb = modDb;
}
