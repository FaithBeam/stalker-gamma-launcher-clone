using Serilog;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Services;

public class AnomalyInstaller(
    ModDb modDb,
    GlobalSettings globalSettings,
    ILogger logger,
    ProgressThrottleService progressThrottle
)
{
    public async Task DownloadAndExtractAsync(string archivePath, string extractDirectory)
    {
        // download anomaly if archive does not exist or doesn't match md5
        if (
            !File.Exists(archivePath)
            || (
                File.Exists(archivePath)
                && await Md5Utility.CalculateFileMd5Async(
                    archivePath,
                    progressThrottle.Throttle<double>(pct =>
                        logger.Information(
                            "{Name} | {Operation} | {Percent:P2}",
                            "Anomaly".PadRight(40),
                            "Check MD5".PadRight(10),
                            pct
                        )
                    )
                ) != globalSettings.StalkerAnomalyArchiveMd5
            )
        )
        {
            await _modDb.GetModDbLinkCurl(
                globalSettings.StalkerAnomalyModDbUrl,
                archivePath,
                progressThrottle.Throttle<double>(pct =>
                    logger.Information(
                        "{Name} | {Operation} | {Percent:P2}",
                        "Anomaly".PadRight(40),
                        "Download".PadRight(10),
                        pct
                    )
                )
            );
        }

        // extract anomaly
        await ArchiveUtility.ExtractAsync(
            archivePath,
            extractDirectory,
            progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    "{Name} | {Operation} | {Percent:P2}",
                    "Anomaly".PadRight(40),
                    "Extract".PadRight(10),
                    pct
                )
            )
        );
    }

    private readonly ModDb _modDb = modDb;
}
