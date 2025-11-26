using System.Collections.Concurrent;
using System.Threading.Channels;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Utilities;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;

public class AddonsAndSeparators(
    ProgressService progressService,
    ModListRecordFactory modListRecordFactory,
    GlobalSettings globalSettings
)
{
    private readonly GlobalSettings _globalSettings = globalSettings;
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
    private static int _visitedExtracts;

    public async Task Install(
        string downloadsPath,
        string modsPaths,
        IList<ModDownloadExtractProgressVm> modDownloadProgresses,
        bool useCurlImpersonate
    )
    {
        _visitedExtracts = 0;
        progressService.UpdateProgress(
            """

            ==================================================================================
                                    Add-ons and Separators Installation                       
            ==================================================================================

            """
        );

        if (!Directory.Exists(downloadsPath))
        {
            Directory.CreateDirectory(downloadsPath);
        }

        if (!Directory.Exists(modsPaths))
        {
            Directory.CreateDirectory(modsPaths);
        }

        var total = modDownloadProgresses.Count;

        var summedFiles = modDownloadProgresses
            .Select(f => new
            {
                File = f.ModListRecord,
                DlProgress = f.ProgressInterface,
                ExtractProgress = f.ProgressInterface,
                MyObj = f,
            })
            .ToList();

        var separators = summedFiles
            .Where(f => f.File is Separator)
            .Select(f => new
            {
                File = f.File as Separator,
                Progress = f.DlProgress,
                f.MyObj,
            })
            .Select(f => new
            {
                f.File,
                Action = (Action)(
                    () =>
                    {
                        progressService.UpdateProgress(
                            $"""
                            _______________ {f.File?.Name} separator _______________
                            Creating MO2 separator in {Path.Join(modsPaths, f.File?.FolderName)}
                            """
                        );
                        f.File?.WriteMetaIni(modsPaths);
                        progressService.UpdateProgress(" ");
                        f.Progress.Report(100);
                        f.MyObj.Status = Status.Done;
                    }
                ),
            });

        var downloadableRecords = summedFiles
            .Where(f => f.File is DownloadableRecord)
            .Select(f => new
            {
                File = f.File as DownloadableRecord,
                f.DlProgress,
                f.ExtractProgress,
                f.MyObj,
            })
            .Select(f => new DownloadableRecordPipeline(
                File: f.File!,
                Dl: async invalidateMirrorCache =>
                {
                    var shouldDownload = await f.File!.ShouldDownloadAsync(downloadsPath, f.MyObj);
                    if (shouldDownload != DownloadableRecord.Action.DoNothing)
                    {
                        f.MyObj.Status = Status.Downloading;
                        await f.File!.DownloadAsync(
                            downloadsPath,
                            useCurlImpersonate,
                            f.DlProgress,
                            f.MyObj,
                            invalidateMirrorCache
                        );
                    }
                    f.DlProgress.Report(100);
                    f.MyObj.Status = Status.Downloaded;
                    return true;
                },
                Extract: async () =>
                    await ExtractAsync(f.File!, downloadsPath, modsPaths, total, f.ExtractProgress),
                MyObj: f.MyObj
            ))
            .GroupBy(x => (x.File.ModDbUrl, x.File.Name));

        foreach (var separator in separators)
        {
            separator.Action();
        }

        var brokenInstalls = await DownloadAndExtractAsync(downloadableRecords);

        foreach (var brokenInstall in brokenInstalls)
        {
            try
            {
                brokenInstall.MyObj.Status = Status.Downloading;
                await brokenInstall.Dl(true);
                brokenInstall.MyObj.Status = Status.Downloaded;
                brokenInstall.MyObj.Status = Status.Extracting;
                await brokenInstall.Extract.Invoke();
                brokenInstall.MyObj.Status = Status.Done;
            }
            catch (CurlDownloadException e)
            {
                brokenInstall.MyObj.Status = Status.Error;
                progressService.UpdateProgress(
                    $"""

                    ERROR DOWNLOADING {brokenInstall.File.Name}
                    {e}
                    """
                );
            }
            catch (SevenZipExtractException e)
            {
                brokenInstall.MyObj.Status = Status.Error;
                var extractPath = Path.Join(
                    $"{brokenInstall.File.Counter}-{brokenInstall.File.AddonName}{brokenInstall.File.Patch}"
                );
                progressService.UpdateProgress(
                    $"""

                    ERROR EXTRACTING {extractPath}, SKIPPING.
                    {e}
                    """
                );
            }
        }
    }

    private async Task<ConcurrentQueue<DownloadableRecordPipeline>> DownloadAndExtractAsync(
        IEnumerable<
            IGrouping<(string? DlLink, string Name), DownloadableRecordPipeline>
        > downloadableRecords
    )
    {
        // download
        var dlChannel = Channel.CreateUnbounded<(
            IList<DownloadableRecordPipeline> dlRecs,
            bool justDownloaded
        )>();

        var brokenInstalls = new ConcurrentQueue<DownloadableRecordPipeline>();

        var t1 = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                downloadableRecords,
                new ParallelOptions { MaxDegreeOfParallelism = _globalSettings.DownloadThreads },
                async (dlRecGroup, _) =>
                {
                    try
                    {
                        var extract = await dlRecGroup.First().Dl(false);
                        foreach (var dlRec in dlRecGroup)
                        {
                            dlRec.MyObj.Status = Status.Downloaded;
                        }
                        await dlChannel.Writer.WriteAsync((dlRecGroup.ToList(), extract));
                    }
                    catch (CurlDownloadException)
                    {
                        foreach (var dlRec in dlRecGroup)
                        {
                            dlRec.MyObj.Status = Status.Retry;
                        }
                        progressService.UpdateProgress(
                            $"""

                            ERROR DOWNLOADING GROUP {dlRecGroup.Key}, SKIPPING. WILL RETRY AT THE END.
                            """
                        );
                        foreach (var dlRec in dlRecGroup)
                        {
                            brokenInstalls.Enqueue(dlRec);
                        }
                    }
                }
            );

            dlChannel.Writer.TryComplete();
        });

        // extract
        var t2 = Task.Run(async () =>
        {
            await Parallel.ForEachAsync(
                dlChannel.Reader.ReadAllAsync(),
                new ParallelOptions { MaxDegreeOfParallelism = _globalSettings.ExtractThreads },
                async (group, _) =>
                {
                    foreach (var item in group.dlRecs)
                    {
                        try
                        {
                            if (group.justDownloaded)
                            {
                                item.MyObj.Status = Status.Extracting;
                                await item.Extract.Invoke();
                                item.MyObj.Status = Status.Done;
                            }
                        }
                        catch (Exception)
                        {
                            item.MyObj.Status = Status.Retry;
                            var extractPath = Path.Join(
                                $"{item.File.Counter}-{item.File.AddonName}{item.File.Patch}"
                            );
                            progressService.UpdateProgress(
                                $"""

                                ERROR EXTRACTING {extractPath}, SKIPPING. WILL RETRY AT THE END.
                                """
                            );
                            brokenInstalls.Enqueue(item);
                        }
                    }
                }
            );
        });

        await Task.WhenAll(t1, t2);

        return brokenInstalls;
    }

    private async Task ExtractAsync(
        DownloadableRecord downloadableRecord,
        string downloadsPath,
        string modsPaths,
        int total,
        IProgress<double> extractProgress
    )
    {
        var extractPath = Path.Join(
            modsPaths,
            $"{downloadableRecord.Counter}-{downloadableRecord.AddonName}{downloadableRecord.Patch}"
        );

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        downloadableRecord.CleanExtractPath(extractPath);

        await downloadableRecord.WriteMetaIniAsync(extractPath);

        progressService.UpdateProgress($"\tExtracting to {extractPath}");
        await downloadableRecord.ExtractAsync(downloadsPath, extractPath, extractProgress);
        progressService.UpdateProgress(
            Interlocked.Increment(ref _visitedExtracts) / (double)total * 100
        );
    }
}

internal record DownloadableRecordPipeline(
    DownloadableRecord File,
    Func<bool, Task<bool>> Dl,
    Func<Task> Extract,
    ModDownloadExtractProgressVm MyObj
);
