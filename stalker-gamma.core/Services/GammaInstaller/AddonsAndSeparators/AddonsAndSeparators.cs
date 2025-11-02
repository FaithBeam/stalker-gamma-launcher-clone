using System.Collections.Concurrent;
using System.Threading.Channels;
using CliWrap.Exceptions;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Factories;
using stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators.Models;

namespace stalker_gamma.core.Services.GammaInstaller.AddonsAndSeparators;

public class AddonsAndSeparators(
    ProgressService progressService,
    ModListRecordFactory modListRecordFactory
)
{
    private readonly ModListRecordFactory _modListRecordFactory = modListRecordFactory;

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
                Dl: (Func<Task<bool>>)(
                    async () =>
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
                                await f.File.DownloadAsync(downloadsPath, useCurlImpersonate);
                                return true;
                            case DownloadableRecord.Action.DownloadMd5Mismatch:
                                progressService.UpdateProgress(
                                    $"_______________ {f.File.AddonName} _______________"
                                );
                                progressService.UpdateProgress(
                                    $"Md5 mismatch in downloaded file: {f.File.DlPath}. Downloading again."
                                );
                                await f.File.DownloadAsync(downloadsPath, useCurlImpersonate);
                                return true;
                            case DownloadableRecord.Action.DownloadForced:
                                progressService.UpdateProgress(
                                    $"_______________ {f.File.AddonName} _______________"
                                );
                                progressService.UpdateProgress("Forced downloading");
                                await f.File.DownloadAsync(downloadsPath, useCurlImpersonate);
                                return true;
                            default:
                                throw new ArgumentOutOfRangeException(
                                    nameof(shouldDlResult),
                                    $"{shouldDlResult}"
                                );
                        }
                    }
                ),
                Extract: (Func<Task>)(
                    async () => await ExtractAsync(f.File!, modsPaths, total, f.Count)
                )
            ));

        foreach (var separator in separators)
        {
            separator.Action();
        }

        var brokenInstalls = await DownloadAndExtractAsync(downloadableRecords, forceZipExtraction);

        if (!brokenInstalls.IsEmpty)
        {
            await DownloadAndExtractAsync(brokenInstalls, forceZipExtraction);
        }
    }

    private async Task<ConcurrentQueue<DownloadableRecordPipeline>> DownloadAndExtractAsync(
        IEnumerable<DownloadableRecordPipeline> downloadableRecords,
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
            foreach (var dlRec in downloadableRecords)
            {
                try
                {
                    var extract = await dlRec.Dl();
                    await dlChannel.Writer.WriteAsync((dlRec, extract));
                }
                catch (CurlDownloadException e)
                {
                    progressService.UpdateProgress(
                        $"""

                        ERROR DOWNLOADING {dlRec.File.Name}, SKIPPING. WILL RETRY AT THE END.
                        {e}
                        """
                    );
                    brokenInstalls.Enqueue(dlRec);
                }
            }

            dlChannel.Writer.TryComplete();
        });

        // extract
        var t2 = Task.Run(async () =>
        {
            await foreach (var item in dlChannel.Reader.ReadAllAsync())
            {
                if (forceZipExtraction || item.justDownloaded)
                {
                    try
                    {
                        await item.dlRec.Extract.Invoke();
                    }
                    catch (CommandExecutionException e)
                    {
                        var extractPath = Path.Join(
                            $"{item.dlRec.Count}-{item.dlRec.File.AddonName}{item.dlRec.File.Patch}"
                        );
                        progressService.UpdateProgress(
                            $"""

                            ERROR EXTRACTING {extractPath}, SKIPPING. WILL RETRY AT THE END.
                            {e}
                            """
                        );
                        brokenInstalls.Enqueue(item.dlRec);
                    }
                }
            }
        });

        await Task.WhenAll(t1, t2);

        return brokenInstalls;
    }

    private async Task ExtractAsync(
        DownloadableRecord downloadableRecord,
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
        await downloadableRecord.ExtractAsync(extractPath);
        progressService.UpdateProgress(counter / (double)total * 100);
    }
}

internal record DownloadableRecordPipeline(
    int Count,
    DownloadableRecord File,
    Func<Task<bool>> Dl,
    Func<Task> Extract
);
