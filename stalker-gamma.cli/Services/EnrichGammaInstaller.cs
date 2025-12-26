using System.Collections.Frozen;
using Serilog;
using stalker_gamma.core.Mappers;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services;
using stalker_gamma.core.Services.Enums;
using stalker_gamma.core.Services.GammaInstallerServices;

namespace stalker_gamma.cli.Services;

public class EnrichGammaInstaller(
    GammaInstaller gammaInstaller,
    ProgressService progressService,
    ProgressThrottleService progressThrottle,
    ILogger logger
)
{
    public async Task InstallAsync(
        FrozenDictionary<int, ModListRecord> modListRecords,
        string anomalyPath,
        Task anomalyTask,
        string gammaPath,
        string cachePath,
        CancellationToken? cancellationToken = null
    )
    {
        var gammaModsPath = Path.Join(gammaPath, "mods");
        var addons = modListRecords
            // moddb
            .Where(kvp => kvp.Value is ModDbRecord)
            .Select(kvp =>
                MapModlistRecordToAddonRecord.Map(
                    kvp.Key,
                    kvp.Value,
                    cachePath,
                    gammaModsPath,
                    AddonType.ModDb,
                    _ => { },
                    _progressThrottle.Throttle<double>(pct =>
                        _logger.Information(
                            StructuredLog,
                            kvp.Value.AddonName![..Math.Min(kvp.Value.AddonName!.Length, 35)]
                                .PadRight(40),
                            "Check MD5".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            _progressService.TotalProgress
                        )
                    ),
                    _progressThrottle.Throttle<double>(pct =>
                        _logger.Information(
                            StructuredLog,
                            kvp.Value.AddonName![..Math.Min(kvp.Value.AddonName!.Length, 35)]
                                .PadRight(40),
                            "Download".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            _progressService.TotalProgress
                        )
                    ),
                    _progressThrottle.Throttle<double>(pct =>
                        _logger.Information(
                            StructuredLog,
                            kvp.Value.AddonName![..Math.Min(kvp.Value.AddonName!.Length, 35)]
                                .PadRight(40),
                            "Extract".PadRight(10),
                            $"{pct:P2}".PadRight(8),
                            _progressService.TotalProgress
                        )
                    )
                )
            )
            // github
            .Concat(
                modListRecords
                    .Where(kvp => kvp.Value is GithubRecord)
                    .Select(kvp =>
                        MapModlistRecordToAddonRecord.Map(
                            kvp.Key,
                            kvp.Value,
                            cachePath,
                            gammaModsPath,
                            AddonType.GitHub,
                            _ => { },
                            _progressThrottle.Throttle<double>(pct =>
                                _logger.Information(
                                    StructuredLog,
                                    kvp.Value.AddonName![
                                            ..Math.Min(kvp.Value.AddonName!.Length, 35)
                                        ]
                                        .PadRight(40),
                                    "Check MD5".PadRight(10),
                                    $"{pct:P2}".PadRight(8),
                                    _progressService.TotalProgress
                                )
                            ),
                            _progressThrottle.Throttle<double>(pct =>
                            {
                                _logger.Information(
                                    StructuredLog,
                                    kvp.Value.AddonName![
                                            ..Math.Min(kvp.Value.AddonName!.Length, 35)
                                        ]
                                        .PadRight(40),
                                    "Download".PadRight(10),
                                    $"{pct:P2}".PadRight(8),
                                    _progressService.TotalProgress
                                );
                            }),
                            _progressThrottle.Throttle<double>(pct =>
                                _logger.Information(
                                    StructuredLog,
                                    kvp.Value.AddonName![
                                            ..Math.Min(kvp.Value.AddonName!.Length, 35)
                                        ]
                                        .PadRight(40),
                                    "Extract".PadRight(10),
                                    $"{pct:P2}".PadRight(8),
                                    _progressService.TotalProgress
                                )
                            )
                        )
                    )
            )
            .GroupBy(x => x.ArchiveDlPath)
            .OrderBy(x => x.First().Index)
            .ToList();

        await gammaInstaller.InstallAsync(
            addons,
            anomalyPath,
            anomalyTask,
            gammaPath,
            cachePath,
            cancellationToken
        );
    }

    private readonly ProgressService _progressService = progressService;
    private readonly ProgressThrottleService _progressThrottle = progressThrottle;
    private readonly ILogger _logger = logger;
    private const string StructuredLog =
        "{AddonName} | {Operation} | {Percent} | {TotalProgress:P2}";
}
