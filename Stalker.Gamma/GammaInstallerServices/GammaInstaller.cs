using Stalker.Gamma.Factories;
using Stalker.Gamma.Models;
using Stalker.Gamma.ModOrganizer;
using Stalker.Gamma.ModOrganizer.DownloadModOrganizer;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

public class GammaInstaller(
    StalkerGammaSettings settings,
    GammaProgress gammaProgress,
    IDownloadModOrganizerService downloadModOrganizerService,
    IGetStalkerModsFromApi getStalkerModsFromApi,
    IDownloadableRecordFactory downloadableRecordFactory,
    IModListRecordFactory modListRecordFactory,
    ISeparatorsFactory separatorsFactory
)
{
    public IGammaProgress Progress { get; } = gammaProgress;

    public virtual async Task FullInstallAsync(
        string anomaly,
        string gamma,
        string cache,
        string? mo2Version = null,
        CancellationToken cancellationToken = default
    )
    {
        mo2Version ??= OperatingSystem.IsWindows() ? "v2.5.2" : "v2.4.4";
        cache = Path.IsPathRooted(cache) ? cache : Path.GetFullPath(cache);
        gamma = Path.IsPathRooted(gamma) ? gamma : Path.GetFullPath(gamma);
        anomaly = Path.IsPathRooted(anomaly) ? anomaly : Path.GetFullPath(anomaly);

        var anomalyBinPath = Path.Join(anomaly, "bin");
        var gammaModsPath = Path.Join(gamma, "mods");
        var gammaDownloadsPath = Path.Join(gamma, "downloads");

        Directory.CreateDirectory(anomaly);
        Directory.CreateDirectory(gamma);
        Directory.CreateDirectory(cache);
        Directory.CreateDirectory(gammaModsPath);
        CreateSymbolicLinkUtility.Create(gammaDownloadsPath, cache);

        var modListTxt = await getStalkerModsFromApi.GetModsAsync();
        var modListRecords = modListRecordFactory.Create(modListTxt);
        var separators = separatorsFactory.Create(modListRecords);
        var anomalyRecord = downloadableRecordFactory.CreateAnomalyRecord(gamma, anomaly);
        var addonRecords = modListRecords
            .Select(
                (rec, idx) =>
                    downloadableRecordFactory.TryCreate(idx, gamma, rec, out var dlRec)
                        ? dlRec
                        : null
            )
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
        var groupedAddonRecords = downloadableRecordFactory.CreateGroupedDownloadableRecords(
            addonRecords
        );
        var gammaLargeFilesRecord = downloadableRecordFactory.CreateGammaLargeFilesRecord(gamma);
        var teivazAnomalyGunslingerRecord =
            downloadableRecordFactory.CreateTeivazAnomalyGunslingerRecord(gamma);
        var gammaSetupRecord = downloadableRecordFactory.CreateGammaSetupRecord(gamma, anomaly);
        var stalkerGammaRecord = downloadableRecordFactory.CreateStalkerGammaRecord(gamma, anomaly);

        var internalProgress = Progress as GammaProgress;
        internalProgress!.TotalMods = new List<IDownloadableRecord>(groupedAddonRecords)
        {
            anomalyRecord,
            gammaLargeFilesRecord,
            teivazAnomalyGunslingerRecord,
            gammaSetupRecord,
            stalkerGammaRecord,
        }.Count;

        foreach (var separator in separators)
        {
            await separator.WriteAsync(gamma);
        }

        // Batch #1
        var mainBatch = Task.Run(
            async () =>
                await ProcessAddonsAsync(
                    [anomalyRecord, .. groupedAddonRecords],
                    cancellationToken
                ),
            cancellationToken
        );
        var teivazDlTask = Task.Run(
            async () => await teivazAnomalyGunslingerRecord.DownloadAsync(cancellationToken),
            cancellationToken
        );
        var gammaLargeFilesDlTask = Task.Run(
            async () => await gammaLargeFilesRecord.DownloadAsync(cancellationToken),
            cancellationToken
        );
        var gammaSetupDownloadTask = Task.Run(
            async () => await gammaSetupRecord.DownloadAsync(cancellationToken),
            cancellationToken
        );
        var stalkerGammaDownloadTask = Task.Run(
            async () => await stalkerGammaRecord.DownloadAsync(cancellationToken),
            cancellationToken
        );

        await Task.WhenAll(
            mainBatch,
            teivazDlTask,
            gammaLargeFilesDlTask,
            gammaSetupDownloadTask,
            stalkerGammaDownloadTask
        );

        await gammaSetupRecord.ExtractAsync(cancellationToken);
        await stalkerGammaRecord.ExtractAsync(cancellationToken);
        await gammaLargeFilesRecord.ExtractAsync(cancellationToken);
        await teivazAnomalyGunslingerRecord.ExtractAsync(cancellationToken);

        DeleteReshadeDlls.Delete(anomalyBinPath);
        DeleteShaderCache.Delete(anomaly);

        await downloadModOrganizerService.DownloadAsync(
            cachePath: cache,
            extractPath: gamma,
            version: mo2Version,
            cancellationToken: cancellationToken
        );

        await InstallModOrganizerGammaProfile.InstallAsync(
            Path.Join(gammaDownloadsPath, stalkerGammaRecord.Name),
            gamma
        );

        await WriteModOrganizerIni.WriteAsync(
            gamma,
            anomaly,
            mo2Version,
            separators.Select(x => x.FolderName).ToList()
        );

        await DisableNexusModHandlerLink.DisableAsync(gamma);

        internalProgress.Reset();
    }

    private async Task ProcessAddonsAsync(
        IList<IDownloadableRecord> addons,
        CancellationToken cancellationToken = default
    ) =>
        await Parallel.ForEachAsync(
            addons,
            new ParallelOptions { MaxDegreeOfParallelism = settings.DownloadThreads },
            async (grs, _) =>
            {
                await grs.DownloadAsync(cancellationToken);
                await grs.ExtractAsync(cancellationToken);
            }
        );
}
