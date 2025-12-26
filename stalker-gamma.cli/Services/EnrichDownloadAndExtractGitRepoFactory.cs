using Serilog;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.GammaInstallerServices;

namespace stalker_gamma.cli.Services;

public class EnrichDownloadAndExtractGitRepoFactory(
    ILogger logger,
    ProgressThrottleService progressThrottle,
    ProgressService progressService
)
{
    public DownloadAndExtractRecordService Create(
        string repoName,
        string repoPath,
        string destinationDir,
        string repoUrl,
        Func<Action<double>, Action<double>, bool, Task> dlFunc,
        Func<Action<double>, Task> extractFunc,
        Task? extractPrereqTask = null,
        CancellationToken? cancellationToken = null
    ) =>
        new()
        {
            RepoPath = repoPath,
            DestinationDir = destinationDir,
            RepoUrl = repoUrl,
            OnDlProgress = progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    StructuredLog,
                    repoName.PadRight(40),
                    "Download".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    progressService.TotalProgress
                )
            ),
            OnExtractProgress = progressThrottle.Throttle<double>(pct =>
                logger.Information(
                    StructuredLog,
                    repoName.PadRight(40),
                    "Extract".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    progressService.TotalProgress
                )
            ),
            OnComplete = progressService.IncrementCompleted,
            DownloadFunc = invalidateMirror =>
                dlFunc(
                    progressThrottle.Throttle<double>(pct =>
                        logger.Information(
                            StructuredLog,
                            repoName.PadRight(40),
                            "Check MD5".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            progressService.TotalProgress
                        )
                    ),
                    progressThrottle.Throttle<double>(pct =>
                        logger.Information(
                            StructuredLog,
                            repoName.PadRight(40),
                            "Download".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            progressService.TotalProgress
                        )
                    ),
                    invalidateMirror
                ),
            ExtractFunc = () =>
                extractFunc(
                    progressThrottle.Throttle<double>(pct =>
                        logger.Information(
                            StructuredLog,
                            repoName.PadRight(40),
                            "Extract".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            progressService.TotalProgress
                        )
                    )
                ),
            ExtractPrereqTask = extractPrereqTask,
            CancellationToken = cancellationToken,
        };

    private const string StructuredLog =
        "{AddonName} | {Operation} | {Percent} | {TotalProgress:P2}";
}
