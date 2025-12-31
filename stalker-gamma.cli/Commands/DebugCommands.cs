using ConsoleAppFramework;
using Serilog;
using stalker_gamma.cli.Utilities;

namespace stalker_gamma.cli.Commands;

[RegisterCommands("debug")]
public class Debug(ILogger logger)
{
    /// <summary>
    /// For debugging broken installations only. Hashes installation folders and creates a compressed archive containing the computed hashes.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <param name="anomaly">The path to the anomaly directory to hash.</param>
    /// <param name="gamma">The path to the gamma directory to hash.</param>
    /// <param name="cache">The path to the cache directory to hash.</param>
    /// <param name="hashType">The type of hash algorithm to use. [Blake3|Sha256]</param>
    /// <returns></returns>
    public async Task HashInstall(
        CancellationToken cancellationToken,
        string anomaly,
        string gamma,
        string cache = "cache",
        HashType hashType = HashType.Blake3
    )
    {
        const string destinationArchive = "stalker-gamma-cli-hashes.zip";
        _logger.Information("Hashing install folders, this will take a while...");
        _logger.Information("Hash Type: {HashType}", hashType);
        await HashUtility.Hash(
            destinationArchive,
            anomaly,
            gamma,
            cache,
            hashType,
            ProgressThrottleUtility.Throttle<double>(pct =>
                _logger.Information("Hash Progress: {Percent:P2}", pct)
            ),
            cancellationToken
        );
        _logger.Information("Finished hashing install folders");
        _logger.Information("Archive created at {DestinationArchive}", destinationArchive);
    }

    private readonly ILogger _logger = logger;
}
