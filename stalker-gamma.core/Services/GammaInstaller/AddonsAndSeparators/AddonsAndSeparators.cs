using System.Collections.Concurrent;
using System.Threading.Channels;
using stalker_gamma.core.Models;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;
using stalker_gamma.core.Utilities;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;

public class AddonsAndSeparators(
    ProgressService progressService,
    ModListRecordFactory modListRecordFactory,
    GlobalSettings globalSettings
)
{
    private readonly GlobalSettings _globalSettings = globalSettings;
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;
    private static int _visitedExtracts = 0;

    public async Task Install(
        string downloadsPath,
        string modsPaths,
        string modListFile,
        bool forceGitDownload,
        bool checkMd5,
        bool updateLargeFiles,
        bool forceZipExtraction,
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

        var files = (await File.ReadAllLinesAsync(modListFile))
            .Select(x => _modListRecordFactory.Create(x))
            .ToList();

        var total = files.Count;

        var counter = 0;

        var summedFiles = files.Select(f => new { Count = ++counter, File = f }).ToList();

        var separators = summedFiles
            .Where(f => f.File is Separator)
            .Select(f => new { f.Count, File = f.File as Separator })
            .Select(f => new
            {
                f.Count,
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
                        f.File?.WriteMetaIni(modsPaths, f.Count);
                        progressService.UpdateProgress(" ");
                    }
                ),
            });

        var downloadableRecords = summedFiles
            .Where(f => f.File is DownloadableRecord)
            .Select(f => new { f.Count, File = f.File as DownloadableRecord })
            .Select(f => new DownloadableRecordPipeline(
                Count: f.Count,
                File: f.File!,
                Dl: async (invalidateMirrorCache) =>
                {
                    var shouldDlResult = await f.File!.ShouldDownloadAsync(
                        downloadsPath,
                        checkMd5,
                        forceGitDownload
                    );

                    switch (shouldDlResult)
                    {
                        case DownloadableRecord.Action.DoNothing:
                            return false;
                        case DownloadableRecord.Action.DownloadMissing:
                            progressService.UpdateProgress(
                                $"_______________ {f.File.AddonName} _______________"
                            );
                            await f.File.DownloadAsync(
                                downloadsPath,
                                useCurlImpersonate,
                                invalidateMirrorCache
                            );
                            return true;
                        case DownloadableRecord.Action.DownloadMd5Mismatch:
                            progressService.UpdateProgress(
                                $"_______________ {f.File.AddonName} _______________"
                            );
                            progressService.UpdateProgress(
                                $"Md5 mismatch in downloaded file: {f.File.DlPath}. Downloading again."
                            );
                            await f.File.DownloadAsync(
                                downloadsPath,
                                useCurlImpersonate,
                                invalidateMirrorCache
                            );
                            return true;
                        case DownloadableRecord.Action.DownloadForced:
                            progressService.UpdateProgress(
                                $"_______________ {f.File.AddonName} _______________"
                            );
                            progressService.UpdateProgress("Forced downloading");
                            await f.File.DownloadAsync(
                                downloadsPath,
                                useCurlImpersonate,
                                invalidateMirrorCache
                            );
                            return true;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(shouldDlResult),
                                $"{shouldDlResult}"
                            );
                    }
                },
                Extract: async () =>
                    await ExtractAsync(f.File!, downloadsPath, modsPaths, total, f.Count)
            ))
            .GroupBy(x => (x.File.ModDbUrl, x.File.Name));

        foreach (var separator in separators)
        {
            separator.Action();
        }

        var brokenInstalls = await DownloadAndExtractAsync(downloadableRecords, forceZipExtraction);

        foreach (var brokenInstall in brokenInstalls)
        {
            try
            {
                await brokenInstall.Dl(true);
                await brokenInstall.Extract.Invoke();
            }
            catch (CurlDownloadException e)
            {
                progressService.UpdateProgress(
                    $"""

                    ERROR DOWNLOADING {brokenInstall.File.Name}
                    {e}
                    """
                );
            }
            catch (SevenZipExtractException e)
            {
                var extractPath = Path.Join(
                    $"{brokenInstall.Count}-{brokenInstall.File.AddonName}{brokenInstall.File.Patch}"
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
        > downloadableRecords,
        bool forceZipExtraction
    )
    {
        // download
        var dlChannel = Channel.CreateUnbounded<(
            DownloadableRecordPipeline dlRec,
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
                            await dlChannel.Writer.WriteAsync((dlRec, extract));
                        }
                    }
                    catch (CurlDownloadException)
                    {
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
            await foreach (var item in dlChannel.Reader.ReadAllAsync())
            {
                try
                {
                    if (forceZipExtraction || item.justDownloaded)
                    {
                        await item.dlRec.Extract.Invoke();
                    }
                }
                catch (Exception)
                {
                    var extractPath = Path.Join(
                        $"{item.dlRec.Count}-{item.dlRec.File.AddonName}{item.dlRec.File.Patch}"
                    );
                    progressService.UpdateProgress(
                        $"""

                        ERROR EXTRACTING {extractPath}, SKIPPING. WILL RETRY AT THE END.
                        """
                    );
                    brokenInstalls.Enqueue(item.dlRec);
                }
            }
        });

        await Task.WhenAll(t1, t2);

        return brokenInstalls;
    }

    private async Task ExtractAsync(
        DownloadableRecord downloadableRecord,
        string downloadsPath,
        string modsPaths,
        int total,
        int counter
    )
    {
        var extractPath = Path.Join(
            modsPaths,
            $"{counter}-{downloadableRecord.AddonName}{downloadableRecord.Patch}"
        );

        if (!Directory.Exists(extractPath))
        {
            Directory.CreateDirectory(extractPath);
        }

        downloadableRecord.CleanExtractPath(extractPath);

        await downloadableRecord.WriteMetaIniAsync(extractPath);

        progressService.UpdateProgress($"\tExtracting to {extractPath}");
        await downloadableRecord.ExtractAsync(downloadsPath, extractPath);

        progressService.UpdateProgress(_visitedExtracts++ / (double)total * 100);
    }
}

internal record DownloadableRecordPipeline(
    int Count,
    DownloadableRecord File,
    Func<bool, Task<bool>> Dl,
    Func<Task> Extract
);
