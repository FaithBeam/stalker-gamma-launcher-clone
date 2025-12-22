using Serilog;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.GammaInstaller.Utilities;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.cli.Services;

public class AnomalyInstaller(ModDb modDb, GlobalSettings globalSettings, ILogger logger)
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
                    ActionUtils.Debounce<double>(pct =>
                        logger.Information(
                            "{Name} | {Operation} | {Percent:P2}",
                            "Anomaly".PadRight(40),
                            "Check MD5",
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
                ActionUtils.Debounce<double>(pct =>
                    logger.Information(
                        "{Name} | {Operation} | {Percent:P2}",
                        "Anomaly".PadRight(40),
                        "Download",
                        pct
                    )
                )
            );
        }

        // extract anomaly
        await ArchiveUtility.ExtractAsync(
            archivePath,
            extractDirectory,
            ActionUtils.Debounce<double>(pct =>
                logger.Information(
                    "{Name} | {Operation} | {Percent:P2}",
                    "Anomaly".PadRight(40),
                    "Extract",
                    pct
                )
            )
        );
    }

    private readonly ModDb _modDb = modDb;
}
