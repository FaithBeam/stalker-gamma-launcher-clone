using Serilog;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;

namespace stalker_gamma.cli.Services;

public class EnrichAnomalyInstaller(
    AnomalyInstaller anomalyInstaller,
    ILogger logger,
    ProgressThrottleService progressThrottle,
    ProgressService progressService
)
{
    public async Task DownloadAndExtractAsync(
        string archivePath,
        string extractDirectory,
        CancellationToken? cancellationToken = null
    )
    {
        // download anomaly if archive does not exist or doesn't match md5
        await anomalyInstaller.DownloadAndExtractAsync(
            archivePath,
            extractDirectory,
            progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    StructuredLog,
                    "Anomaly".PadRight(40),
                    "Check MD5".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    _progressService.TotalProgress
                )
            ),
            progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    StructuredLog,
                    "Anomaly".PadRight(40),
                    "Download".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    _progressService.TotalProgress
                )
            ),
            progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    StructuredLog,
                    "Anomaly".PadRight(40),
                    "Extract".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    _progressService.TotalProgress
                )
            ),
            cancellationToken
        );

        _progressService.IncrementCompleted();
    }

    private const string StructuredLog =
        "{AddonName} | {Operation} | {Percent} | {TotalProgress:P2}";
    private readonly ProgressService _progressService = progressService;
}
