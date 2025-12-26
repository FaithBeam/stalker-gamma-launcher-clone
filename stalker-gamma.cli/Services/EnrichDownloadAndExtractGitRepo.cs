using Serilog;
using stalker_gamma.core.Services;

namespace stalker_gamma.cli.Services;

public class EnrichDownloadAndExtractGitRepo(
    DownloadAndExtractGitRepoService downloadAndExtractGitRepoService,
    ILogger logger,
    ProgressThrottleService progressThrottle,
    ProgressService progressService
)
{
    private readonly ILogger _logger = logger;
    private readonly ProgressThrottleService _progressThrottle = progressThrottle;
    private readonly ProgressService _progressService = progressService;

    public async Task DownloadAndExtractAsync(
        string repoPath,
        string destinationDir,
        string repoUrl,
        string repoName
    )
    {
        await downloadAndExtractGitRepoService.DownloadAndExtractAsync(
            repoPath,
            destinationDir,
            repoUrl,
            onDlProgress: _progressThrottle.Throttle<double>(pct =>
                _logger.Information(
                    StructuredLog,
                    repoName.PadRight(40),
                    "Pull".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    _progressService.TotalProgress
                )
            ),
            onExtractProgress: _progressThrottle.Throttle<double>(pct =>
                _logger.Information(
                    StructuredLog,
                    repoName.PadRight(40),
                    "Extract".PadRight(10),
                    $"{pct:P2}".PadRight(8),
                    _progressService.TotalProgress
                )
            ),
            onComplete: _progressService.IncrementCompleted
        );
    }

    private const string StructuredLog =
        "{AddonName} | {Operation} | {Percent} | {TotalProgress:P2}";
}
