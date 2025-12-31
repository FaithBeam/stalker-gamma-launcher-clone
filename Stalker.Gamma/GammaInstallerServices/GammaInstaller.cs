using System.Collections.Concurrent;
using Stalker.Gamma.Factories;
using Stalker.Gamma.Models;
using Stalker.Gamma.ModOrganizer;
using Stalker.Gamma.ModOrganizer.DownloadModOrganizer;
using Stalker.Gamma.Utilities;

namespace Stalker.Gamma.GammaInstallerServices;

public class GammaInstallerArgs
{
    public required string Anomaly { get; set; }
    public required string Gamma { get; set; }
    public required string Cache { get; set; }
    public string? Mo2Version { get; set; }
    public bool DownloadGithubArchives { get; set; } = true;
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
}

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

    public virtual async Task FullInstallAsync(GammaInstallerArgs args)
    {
        args.Mo2Version ??= OperatingSystem.IsWindows() ? "v2.5.2" : "v2.4.4";
        args.Cache = Path.IsPathRooted(args.Cache) ? args.Cache : Path.GetFullPath(args.Cache);
        args.Gamma = Path.IsPathRooted(args.Gamma) ? args.Gamma : Path.GetFullPath(args.Gamma);
        args.Anomaly = Path.IsPathRooted(args.Anomaly)
            ? args.Anomaly
            : Path.GetFullPath(args.Anomaly);

        var anomalyBinPath = Path.Join(args.Anomaly, "bin");
        var gammaModsPath = Path.Join(args.Gamma, "mods");
        var gammaDownloadsPath = Path.Join(args.Gamma, "downloads");

        Directory.CreateDirectory(args.Anomaly);
        Directory.CreateDirectory(args.Gamma);
        Directory.CreateDirectory(args.Cache);
        Directory.CreateDirectory(gammaModsPath);
        CreateSymbolicLinkUtility.Create(gammaDownloadsPath, args.Cache);

        var modListTxt = await getStalkerModsFromApi.GetModsAsync();
        var modListRecords = modListRecordFactory.Create(modListTxt);
        var separators = separatorsFactory.Create(modListRecords);
        var anomalyRecord = downloadableRecordFactory.CreateAnomalyRecord(
            Path.Join(args.Gamma, "downloads"),
            args.Anomaly
        );
        var addonRecords = modListRecords
            .Select(
                (rec, idx) =>
                {
                    if (!downloadableRecordFactory.TryCreate(idx, args.Gamma, rec, out var dlRec))
                    {
                        return null;
                    }

                    if (dlRec is GithubRecord ghr)
                    {
                        ghr.Download = args.DownloadGithubArchives;
                        return ghr;
                    }
                    return dlRec;
                }
            )
            .Where(x => x is not null)
            .Select(x => x!)
            .ToList();
        var groupedAddonRecords = downloadableRecordFactory.CreateGroupedDownloadableRecords(
            addonRecords
        );
        var gammaLargeFilesRecord = downloadableRecordFactory.CreateGammaLargeFilesRecord(
            args.Gamma
        );
        var teivazAnomalyGunslingerRecord =
            downloadableRecordFactory.CreateTeivazAnomalyGunslingerRecord(args.Gamma);
        var gammaSetupRecord = downloadableRecordFactory.CreateGammaSetupRecord(
            args.Gamma,
            args.Anomaly
        );
        var stalkerGammaRecord = downloadableRecordFactory.CreateStalkerGammaRecord(
            args.Gamma,
            args.Anomaly
        );

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
            await separator.WriteAsync(args.Gamma);
        }

        var brokenAddons = new ConcurrentBag<IDownloadableRecord>();

        // Batch #1
        var mainBatch = Task.Run(
            async () =>
                await ProcessAddonsAsync(
                    [anomalyRecord, .. groupedAddonRecords],
                    brokenAddons,
                    args.CancellationToken
                ),
            args.CancellationToken
        );
        var teivazDlTask = Task.Run(
            async () => await teivazAnomalyGunslingerRecord.DownloadAsync(args.CancellationToken),
            args.CancellationToken
        );
        var gammaLargeFilesDlTask = Task.Run(
            async () => await gammaLargeFilesRecord.DownloadAsync(args.CancellationToken),
            args.CancellationToken
        );
        var gammaSetupDownloadTask = Task.Run(
            async () => await gammaSetupRecord.DownloadAsync(args.CancellationToken),
            args.CancellationToken
        );
        var stalkerGammaDownloadTask = Task.Run(
            async () => await stalkerGammaRecord.DownloadAsync(args.CancellationToken),
            args.CancellationToken
        );

        await Task.WhenAll(
            mainBatch,
            teivazDlTask,
            gammaLargeFilesDlTask,
            gammaSetupDownloadTask,
            stalkerGammaDownloadTask
        );

        foreach (var brokenAddon in brokenAddons)
        {
            await brokenAddon.DownloadAsync(args.CancellationToken);
            await brokenAddon.ExtractAsync(args.CancellationToken);
        }

        await gammaSetupRecord.ExtractAsync(args.CancellationToken);
        await stalkerGammaRecord.ExtractAsync(args.CancellationToken);
        await gammaLargeFilesRecord.ExtractAsync(args.CancellationToken);
        await teivazAnomalyGunslingerRecord.ExtractAsync(args.CancellationToken);

        DeleteReshadeDlls.Delete(anomalyBinPath);
        DeleteShaderCache.Delete(args.Anomaly);
        await UserLtxForceBorderless.ForceBorderless(args.Anomaly);

        await downloadModOrganizerService.DownloadAsync(
            cachePath: args.Cache,
            extractPath: args.Gamma,
            version: args.Mo2Version,
            cancellationToken: args.CancellationToken
        );

        await InstallModOrganizerGammaProfile.InstallAsync(
            Path.Join(gammaDownloadsPath, stalkerGammaRecord.Name),
            args.Gamma
        );

        await WriteModOrganizerIni.WriteAsync(
            args.Gamma,
            args.Anomaly,
            args.Mo2Version,
            separators.Select(x => x.FolderName).ToList()
        );

        await DisableNexusModHandlerLink.DisableAsync(args.Gamma);

        internalProgress.Reset();
    }

    private async Task ProcessAddonsAsync(
        IList<IDownloadableRecord> addons,
        ConcurrentBag<IDownloadableRecord> brokenAddons,
        CancellationToken cancellationToken = default
    ) =>
        await Parallel.ForEachAsync(
            addons,
            new ParallelOptions { MaxDegreeOfParallelism = settings.DownloadThreads },
            async (grs, _) =>
            {
                try
                {
                    await grs.DownloadAsync(cancellationToken);
                    await grs.ExtractAsync(cancellationToken);
                }
                catch (Exception)
                {
                    brokenAddons.Add(grs);
                }
            }
        );
}
